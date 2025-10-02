using System;
using System.Collections.Generic;
using System.Text;
using MemoryPack;
using Arch.Net.ProtocolInternal;

namespace Arch.Net
{
    /// <summary>
    /// Handles topology-related RPCs: peer advertisements and metric updates.
    /// Integrates with TopologyGraph and NetworkRouter at runtime.
    /// </summary>
    public static class TopologyService
    {
        public static void Register(Session session)
        {
            if (session == null) return;
            session.RegisterRpc((byte)RpcIds.TopologyAdvert, HandleAdvert);
            session.RegisterRpc((byte)RpcIds.TopologyMetrics, HandleMetrics);
        }

        private static void HandleAdvert(byte[] buf, int off, int len)
        {
            int p = off; int end = off + len;
            if (p >= end || buf[p++] != (byte)RpcIds.TopologyAdvert) return;
            try
            {
                var span = new ReadOnlySpan<byte>(buf, p, end - p);
                var msg = MemoryPackSerializer.Deserialize<TopologyAdvertMsg>(span);
                if (msg.Entries != null)
                {
                    foreach (var e in msg.Entries)
                    {
                        if (string.IsNullOrEmpty(e.PeerId)) continue;
                        TopologyGraph.RegisterPeer(e.PeerId, e.Endpoint, e.Weight);
                        if (!string.IsNullOrEmpty(e.Endpoint)) NetworkRouter.AddPeer(e.Endpoint, e.Weight);
                    }
                }
            }
            catch
            {
                // fallback legacy parser
                uint count = ReadVarUInt(buf, ref p, end);
                for (uint i = 0; i < count && p < end; i++)
                {
                    string peerId = ReadString(buf, ref p, end);
                    string endpoint = ReadString(buf, ref p, end);
                    int weight = (int)ReadVarUInt(buf, ref p, end);
                    if (!string.IsNullOrEmpty(peerId))
                    {
                        TopologyGraph.RegisterPeer(peerId, endpoint, weight);
                        if (!string.IsNullOrEmpty(endpoint)) NetworkRouter.AddPeer(endpoint, weight);
                    }
                }
            }
        }

        private static void HandleMetrics(byte[] buf, int off, int len)
        {
            int p = off; int end = off + len;
            if (p >= end || buf[p++] != (byte)RpcIds.TopologyMetrics) return;
            try
            {
                var span = new ReadOnlySpan<byte>(buf, p, end - p);
                var msg = MemoryPackSerializer.Deserialize<TopologyMetricsMsg>(span);
                if (msg.Entries != null)
                {
                    foreach (var e in msg.Entries)
                    {
                        TopologyGraph.UpdateEdge(e.A, e.B, e.Rtt, e.Loss, e.Jitter);
                    }
                }
            }
            catch
            {
                uint edgeCount = ReadVarUInt(buf, ref p, end);
                for (uint i = 0; i < edgeCount && p < end; i++)
                {
                    string a = ReadString(buf, ref p, end);
                    string b = ReadString(buf, ref p, end);
                    int rtt = (int)ReadVarUInt(buf, ref p, end);
                    float loss = ReadFloat(buf, ref p, end);
                    float jitter = ReadFloat(buf, ref p, end);
                    TopologyGraph.UpdateEdge(a, b, rtt, loss, jitter);
                }
            }
        }

        private static uint ReadVarUInt(byte[] buf, ref int p, int end)
        {
            uint val = 0; int shift = 0;
            while (p < end)
            {
                byte b = buf[p++];
                val |= (uint)(b & 0x7F) << shift;
                if ((b & 0x80) == 0) break;
                shift += 7;
            }
            return val;
        }

        private static string ReadString(byte[] buf, ref int p, int end)
        {
            uint n = ReadVarUInt(buf, ref p, end);
            if (n == 0 || p + n > end) return string.Empty;
            var s = Encoding.UTF8.GetString(buf, p, (int)n);
            p += (int)n;
            return s;
        }

        private static float ReadFloat(byte[] buf, ref int p, int end)
        {
            if (p + 4 > end) return 0f;
            unsafe
            {
                uint u = (uint)(buf[p] | (buf[p + 1] << 8) | (buf[p + 2] << 16) | (buf[p + 3] << 24));
                p += 4;
                return *(float*)(&u);
            }
        }
    }
}
