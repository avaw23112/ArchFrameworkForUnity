using MemoryPack;
using Arch.Net.ProtocolInternal;

namespace Arch.Net
{
    /// <summary>
    /// RPC for routing control (e.g., force connect to endpoint).
    /// </summary>
    public static class RoutingControlService
    {
        public static void Register(Session session)
        {
            if (session == null) return;
            session.RegisterRpc((byte)RpcIds.RoutingForceConnect, (buf, off, len) =>
            {
                int p = off; int end = off + len;
                if (p >= end || buf[p++] != (byte)RpcIds.RoutingForceConnect) return;
                var span = new System.ReadOnlySpan<byte>(buf, p, end - p);
                var msg = MemoryPack.MemoryPackSerializer.Deserialize<RoutingForceConnectMsg>(span);
                if (!string.IsNullOrEmpty(msg.Endpoint))
                {
                    NetworkRouter.ForceConnect(msg.Endpoint);
                }
            });
        }

        public static void SendForceConnect(Session session, string endpoint)
        {
            if (session == null || string.IsNullOrEmpty(endpoint)) return;
            var msg = new RoutingForceConnectMsg(endpoint);
            var body = MemoryPackSerializer.Serialize(msg);
            var payload = new byte[1 + (body?.Length ?? 0)];
            payload[0] = (byte)RpcIds.RoutingForceConnect;
            if (body != null && body.Length > 0)
                System.Buffer.BlockCopy(body, 0, payload, 1, body.Length);
            var packet = PacketBuilder.BuildRpc(payload);
            session.Send(packet, packet.Length);
        }
    }
}
