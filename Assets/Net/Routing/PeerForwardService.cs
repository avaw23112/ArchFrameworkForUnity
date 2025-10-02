using System;
using MemoryPack;
using Arch.Net.ProtocolInternal;

namespace Arch.Net
{
    /// <summary>
    /// Peer-to-peer RPC forwarder: wraps an inner RPC payload and forwards to a target peer.
    /// If target equals self, delivers payload locally by invoking Session.HandlePacket on a built RPC packet.
    /// Note: true multi-hop requires multi-link support; current implementation best-effort sends via current session.
    /// </summary>
    public static class PeerForwardService
    {
        public static void Register(Session session)
        {
            if (session == null) return;
            session.RegisterRpc((byte)RpcIds.PeerForward, (buf, off, len) =>
            {
                int p = off; int end = off + len;
                if (p >= end || buf[p++] != (byte)RpcIds.PeerForward) return;
                var span = new ReadOnlySpan<byte>(buf, p, end - p);
                var msg = MemoryPackSerializer.Deserialize<PeerForwardMsg>(span);
                var myPeerId = OwnershipService.MyClientId.ToString();
                if (msg.TargetPeerId == myPeerId)
                {
                    // Deliver locally: rebuild RPC packet and handle
                    var pkt = PacketBuilder.BuildRpc(msg.Payload);
                    session.HandlePacket(pkt);
                    return;
                }
                if (msg.Ttl == 0) return;
                // Best-effort: forward again via current session (requires multi-link to truly route)
                var fwd = new PeerForwardMsg(msg.TargetPeerId, msg.SourcePeerId, msg.Payload, (byte)(msg.Ttl - 1));
                SendEnveloped(session, fwd);
            });
        }

        public static void Send(Session session, string targetPeerId, byte[] innerRpcPayload, bool allowForceConnect = false)
        {
            if (session == null || string.IsNullOrEmpty(targetPeerId) || innerRpcPayload == null) return;
            var myPeerId = OwnershipService.MyClientId.ToString();
            // If we are already connected to the target endpoint, send direct
            if (TopologyGraph.TryGetEndpoint(targetPeerId, out var endpoint) && endpoint == NetworkRouter.CurrentEndpoint)
            {
                var pkt = PacketBuilder.BuildRpc(innerRpcPayload);
                session.Send(pkt, pkt.Length);
                return;
            }
            // Optionally force-connect then send
            if (allowForceConnect && TopologyGraph.TryGetEndpoint(targetPeerId, out endpoint) && !string.IsNullOrEmpty(endpoint))
            {
                NetworkRouter.ForceConnect(endpoint);
                var pkt = PacketBuilder.BuildRpc(innerRpcPayload);
                session.Send(pkt, pkt.Length);
                return;
            }
            // Else wrap and forward via current session
            var msg = new PeerForwardMsg(targetPeerId, myPeerId, innerRpcPayload, 16);
            SendEnveloped(session, msg);
        }

        private static void SendEnveloped(Session session, PeerForwardMsg msg)
        {
            var body = MemoryPackSerializer.Serialize(msg);
            var payload = new byte[1 + (body?.Length ?? 0)];
            payload[0] = (byte)RpcIds.PeerForward;
            if (body != null && body.Length > 0)
                System.Buffer.BlockCopy(body, 0, payload, 1, body.Length);
            var packet = PacketBuilder.BuildRpc(payload);
            session.Send(packet, packet.Length);
        }
    }
}
