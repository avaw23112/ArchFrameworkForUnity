using System.Collections.Generic;
using MemoryPack;
using Arch.Net.ProtocolInternal;

namespace Arch.Net
{
    /// <summary>
    /// Sender helpers for topology RPCs using MemoryPack (code-as-protocol).
    /// </summary>
    public static class TopologyRpc
    {
        public static void SendAdvert(Session session, IEnumerable<PeerEntry> entries)
        {
            if (session == null) return;
            var list = entries is List<PeerEntry> l ? l : new List<PeerEntry>(entries ?? System.Array.Empty<PeerEntry>());
            var msg = new TopologyAdvertMsg(list);
            var payloadBody = MemoryPackSerializer.Serialize(msg);
            var payload = new byte[1 + (payloadBody?.Length ?? 0)];
            payload[0] = (byte)RpcIds.TopologyAdvert;
            if (payloadBody != null && payloadBody.Length > 0)
                System.Buffer.BlockCopy(payloadBody, 0, payload, 1, payloadBody.Length);
            var packet = PacketBuilder.BuildRpc(payload);
            session.Send(packet, packet.Length);
        }

        public static void SendMetrics(Session session, IEnumerable<EdgeEntry> entries)
        {
            if (session == null) return;
            var list = entries is List<EdgeEntry> l ? l : new List<EdgeEntry>(entries ?? System.Array.Empty<EdgeEntry>());
            var msg = new TopologyMetricsMsg(list);
            var payloadBody = MemoryPackSerializer.Serialize(msg);
            var payload = new byte[1 + (payloadBody?.Length ?? 0)];
            payload[0] = (byte)RpcIds.TopologyMetrics;
            if (payloadBody != null && payloadBody.Length > 0)
                System.Buffer.BlockCopy(payloadBody, 0, payload, 1, payloadBody.Length);
            var packet = PacketBuilder.BuildRpc(payload);
            session.Send(packet, packet.Length);
        }
    }
}
