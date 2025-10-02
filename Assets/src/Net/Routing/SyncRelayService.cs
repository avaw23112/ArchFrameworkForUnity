using System;
using MemoryPack;
using Arch.Net.ProtocolInternal;

namespace Arch.Net
{
    /// <summary>
    /// Relays Sync packets along the MST: sender -> parent, parent -> children.
    /// Applies locally before forwarding. Uses PeerForward for multi-hop.
    /// </summary>
    public static class SyncRelayService
    {
        // 去重缓存：hash -> lastSeenFrame
        private static readonly System.Collections.Generic.Dictionary<ulong, int> s_seen = new System.Collections.Generic.Dictionary<ulong, int>(4096);
        private static int s_logCounterRecvFwd;
        private static int s_logCounterDup;

        public static void Register(Session session)
        {
            if (session == null) return;
            session.RegisterRpc((byte)RpcIds.SyncRelay, (buf, off, len) =>
            {
                int p = off; int end = off + len;
                if (p >= end || buf[p++] != (byte)RpcIds.SyncRelay) return;
                var span = new ReadOnlySpan<byte>(buf, p, end - p);
                SyncRelayMsg msg;
                try { msg = MemoryPackSerializer.Deserialize<SyncRelayMsg>(span); }
                catch { return; }
                if (msg?.Packet == null || msg.Packet.Length == 0) return;

                // 0) 去重过滤（基于包hash与窗口帧）
                if (IsDuplicate(msg)) return;

                // 1) Apply locally
                session.HandlePacket(msg.Packet);

                // 2) TTL exhausted? stop
                if (msg.Ttl == 0) return;

                // 2.5) 配置：如禁用 SyncRelay 转发，则只本地应用
                var cfg = Arch.Net.NetworkSettings.Config;
                if (cfg != null && !cfg.EnableSyncRelay) return;

                // 3) Forward to children
                var myPeerId = OwnershipService.MyClientId.ToString();
                var children = TopologyGraphGetChildren(myPeerId);
                if (children == null || children.Count == 0) return;

                var next = new SyncRelayMsg(myPeerId, msg.Packet, (byte)(msg.Ttl - 1));
                var body = MemoryPackSerializer.Serialize(next);
                var payload = new byte[1 + (body?.Length ?? 0)];
                payload[0] = (byte)RpcIds.SyncRelay;
                if (body != null && body.Length > 0)
                    System.Buffer.BlockCopy(body, 0, payload, 1, body.Length);

                foreach (var childPeerId in children)
                {
                    // 单向沿树扩散，无需回溯过滤；若需要可比较 childPeerId != msg.SourcePeerId
                    PeerForwardService.Send(session, childPeerId, payload, allowForceConnect: false);
                }

                // Logging (sampled)
                var logCfg = Arch.Net.NetworkSettings.Config;
                if (logCfg != null && logCfg.EnableSyncRelayLog)
                {
                    int sample = logCfg.SyncRelayLogSampleRate > 0 ? logCfg.SyncRelayLogSampleRate : 100;
                    if ((System.Threading.Interlocked.Increment(ref s_logCounterRecvFwd) % sample) == 0)
                    {
                        Arch.Tools.ArchLog.LogInfo($"[SyncRelay] recv+fwd from={msg.SourcePeerId} children={children.Count} ttl={msg.Ttl}");
                    }
                }
            });
        }

        public static void SendUp(byte[] syncPacket)
        {
            var session = NetworkSingleton.Session;
            if (session == null || syncPacket == null || syncPacket.Length == 0) return;
            var myPeerId = OwnershipService.MyClientId.ToString();
            int ttl = Arch.Net.NetworkSettings.Config?.SyncRelayTtl ?? 16;
            if (ttl < 0) ttl = 0;
            var msg = new SyncRelayMsg(myPeerId, syncPacket, (byte)ttl);
            var body = MemoryPackSerializer.Serialize(msg);
            var payload = new byte[1 + (body?.Length ?? 0)];
            payload[0] = (byte)RpcIds.SyncRelay;
            if (body != null && body.Length > 0)
                System.Buffer.BlockCopy(body, 0, payload, 1, body.Length);
            var packet = PacketBuilder.BuildRpc(payload);
            var cfg = Arch.Net.NetworkSettings.Config;
            if (cfg != null && cfg.EnableSyncRelay)
                session.Send(packet, packet.Length);
            else
                session.Send(syncPacket, syncPacket.Length); // 回退：直接发送原始包
        }

        private static System.Collections.Generic.List<string> TopologyGraphGetChildren(string myPeerId)
        {
            var res = new System.Collections.Generic.List<string>();
            var peers = TopologyGraph.GetPeers();
            for (int i = 0; i < peers.Count; i++)
            {
                var peer = peers[i];
                if (peer == myPeerId) continue;
                if (TopologyGraph.TryGetBestNeighbor(peer, out var best, out _))
                {
                    if (best == myPeerId) res.Add(peer);
                }
            }
            return res;
        }

        private static bool IsDuplicate(SyncRelayMsg msg)
        {
            try
            {
                var cfg = Arch.Net.NetworkSettings.Config;
                int window = cfg?.SyncRelayDedupWindowFrames ?? 120;
                int now = (int)UnityEngine.Time.frameCount;
                ulong h = Hash64(msg.SourcePeerId, msg.Packet);
                // 清理过期项（简单线性清理）
                var toRemove = new System.Collections.Generic.List<ulong>();
                foreach (var kv in s_seen)
                {
                    if (now - kv.Value > window) toRemove.Add(kv.Key);
                }
                for (int i = 0; i < toRemove.Count; i++) s_seen.Remove(toRemove[i]);

                if (s_seen.TryGetValue(h, out var last) && now - last <= window)
                {
                    s_seen[h] = now; // touch
                    if (cfg != null && cfg.EnableSyncRelayLog)
                    {
                        int sample = cfg.SyncRelayLogSampleRate > 0 ? cfg.SyncRelayLogSampleRate : 100;
                        if ((System.Threading.Interlocked.Increment(ref s_logCounterDup) % sample) == 0)
                        {
                            Arch.Tools.ArchLog.LogInfo($"[SyncRelay] drop-dup age={now-last} seen={s_seen.Count}");
                        }
                    }
                    return true;
                }
                s_seen[h] = now;

                // Capacity control
                int cap = cfg?.SyncRelayDedupCapacity ?? 8192;
                if (cap > 0 && s_seen.Count > cap)
                {
                    // Remove oldest half of entries (simple heuristic)
                    int target = cap / 2;
                    var list = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<ulong,int>>(s_seen);
                    list.Sort((a,b)=> a.Value.CompareTo(b.Value)); // ascending by lastSeen
                    for (int i = 0; i < list.Count - target; i++)
                    {
                        s_seen.Remove(list[i].Key);
                    }
                }
                return false;
            }
            catch { return false; }
        }

        private static ulong Hash64(string source, byte[] data)
        {
            const ulong FNV_OFFSET = 1469598103934665603UL;
            const ulong FNV_PRIME = 1099511628211UL;
            ulong h = FNV_OFFSET;
            if (!string.IsNullOrEmpty(source))
            {
                for (int i = 0; i < source.Length; i++)
                {
                    h ^= source[i];
                    h *= FNV_PRIME;
                }
            }
            if (data != null)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    h ^= data[i];
                    h *= FNV_PRIME;
                }
            }
            return h;
        }
    }
}
