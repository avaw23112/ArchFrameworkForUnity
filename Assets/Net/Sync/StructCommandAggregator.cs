using System;
using System.Collections.Generic;
using System.Reflection;
using Arch.Core;
using Arch.Core.Extensions;

namespace Arch.Net
{
    /// <summary>
    /// Aggregates local structural changes (create/destroy/add/remove) into a reliable command group.
    /// Flushes once per frame via StructCommandFlushSystem before Sync scanners run.
    /// </summary>
    public static class StructCommandAggregator
    {
        private static bool s_subscribed;
        private static readonly HashSet<ulong> s_created = new HashSet<ulong>();
        private static readonly HashSet<ulong> s_destroyed = new HashSet<ulong>();
        private static readonly Dictionary<ulong, HashSet<int>> s_added = new Dictionary<ulong, HashSet<int>>();
        private static readonly Dictionary<ulong, HashSet<int>> s_removed = new Dictionary<ulong, HashSet<int>>();
        private static uint s_seq;

        public static void EnsureSubscribed()
        {
            if (s_subscribed) return;
            foreach (var w in Arch.NamedWorld.Instance.NamedWorlds)
            {
                SubscribeEntityEvents(w);
            }
            // Subscribe for all [NetworkSync] components
            var list = new List<Type>();
            Attributes.Collector.CollectTypes<IComponent>(list);
            foreach (var t in list)
            {
                if (t == null || !t.IsValueType) continue;
                if (t.GetCustomAttributes(typeof(NetworkSyncAttribute), false).Length == 0) continue;
                foreach (var w in Arch.NamedWorld.Instance.NamedWorlds)
                {
                    TrySubscribeAdded(w, t);
                    TrySubscribeRemoved(w, t);
                }
            }
            s_subscribed = true;
        }

        private static void SubscribeEntityEvents(World w)
        {
            // Track destruction
            w.SubscribeEntityDestroyed((in Entity e) =>
            {
                if (e.TryGet<NetworkEntityId>(out var nid))
                {
                    // 所有权过滤：仅记录本端拥有的实体
                    if (e.TryGet<NetworkOwner>(out var owner) && OwnershipService.IsOwner(owner.OwnerClientId))
                        s_destroyed.Add(nid.Value);
                }
            });
            // Track creation when NetworkEntityId is first added
            w.SubscribeComponentAdded<NetworkEntityId>((in Entity e, ref NetworkEntityId nid) =>
            {
                if (e.TryGet<NetworkOwner>(out var owner) && OwnershipService.IsOwner(owner.OwnerClientId))
                    s_created.Add(nid.Value);
            });
        }

        private static void TrySubscribeAdded(World world, Type t)
        {
            var mi = typeof(StructCommandAggregator).GetMethod(nameof(SubAddedGeneric), BindingFlags.Static | BindingFlags.NonPublic);
            try { mi.MakeGenericMethod(t).Invoke(null, new object[] { world }); }
            catch { /* ignore */ }
        }
        private static void TrySubscribeRemoved(World world, Type t)
        {
            var mi = typeof(StructCommandAggregator).GetMethod(nameof(SubRemovedGeneric), BindingFlags.Static | BindingFlags.NonPublic);
            try { mi.MakeGenericMethod(t).Invoke(null, new object[] { world }); }
            catch { /* ignore */ }
        }

        private static void SubAddedGeneric<T>(World world) where T : struct, IComponent
        {
            world.SubscribeComponentAdded<T>((in Entity e, ref T c) =>
            {
                if (!e.TryGet<NetworkEntityId>(out var nid)) return;
                if (!e.TryGet<NetworkOwner>(out var owner) || !OwnershipService.IsOwner(owner.OwnerClientId)) return;
                if (!ComponentRegistry.TryGet(typeof(T), out var ct)) return;
                if (!s_added.TryGetValue(nid.Value, out var set)) s_added[nid.Value] = set = new HashSet<int>();
                set.Add(ct.Id);
            });
        }
        private static void SubRemovedGeneric<T>(World world) where T : struct, IComponent
        {
            world.SubscribeComponentRemoved((in Entity e, ref T c) =>
            {
                if (!e.TryGet<NetworkEntityId>(out var nid)) return;
                if (!e.TryGet<NetworkOwner>(out var owner) || !OwnershipService.IsOwner(owner.OwnerClientId)) return;
                if (!ComponentRegistry.TryGet(typeof(T), out var ct)) return;
                if (!s_removed.TryGetValue(nid.Value, out var set)) s_removed[nid.Value] = set = new HashSet<int>();
                set.Add(ct.Id);
            });
        }

        public static void Flush(uint worldId = 0)
        {
            if (NetworkSingleton.Session == null) return;
            // nothing to send?
            if (s_created.Count == 0 && s_destroyed.Count == 0 && s_added.Count == 0 && s_removed.Count == 0) return;

            // estimate size and build
            int size = 0;
            size += VarIntSize(worldId);
            size += VarIntSize(OwnershipService.MyClientId >= 0 ? (uint)OwnershipService.MyClientId : 0u);
            size += VarIntSize(++s_seq);

            size += VarIntSize((uint)s_created.Count) + s_created.Count * 8;
            size += VarIntSize((uint)s_destroyed.Count) + s_destroyed.Count * 8;
            size += VarIntSize((uint)s_added.Count);
            foreach (var kv in s_added) { size += 8 + VarIntSize((uint)kv.Value.Count) + SumVarInt(kv.Value); }
            size += VarIntSize((uint)s_removed.Count);
            foreach (var kv in s_removed) { size += 8 + VarIntSize((uint)kv.Value.Count) + SumVarInt(kv.Value); }

            var payload = new byte[1 + size];
            int p = 0; payload[p++] = (byte)RpcIds.StructCommandGroup;
            WriteVarUInt(payload, ref p, worldId);
            WriteVarUInt(payload, ref p, (uint)(OwnershipService.MyClientId >= 0 ? OwnershipService.MyClientId : 0));
            WriteVarUInt(payload, ref p, s_seq);

            WriteVarUInt(payload, ref p, (uint)s_created.Count);
            foreach (var id in s_created) WriteUInt64(payload, ref p, id);

            WriteVarUInt(payload, ref p, (uint)s_destroyed.Count);
            foreach (var id in s_destroyed) WriteUInt64(payload, ref p, id);

            WriteVarUInt(payload, ref p, (uint)s_added.Count);
            foreach (var kv in s_added)
            {
                WriteUInt64(payload, ref p, kv.Key);
                WriteVarUInt(payload, ref p, (uint)kv.Value.Count);
                foreach (var t in kv.Value) WriteVarUInt(payload, ref p, (uint)t);
            }

            WriteVarUInt(payload, ref p, (uint)s_removed.Count);
            foreach (var kv in s_removed)
            {
                WriteUInt64(payload, ref p, kv.Key);
                WriteVarUInt(payload, ref p, (uint)kv.Value.Count);
                foreach (var t in kv.Value) WriteVarUInt(payload, ref p, (uint)t);
            }

            var pkt = PacketBuilder.BuildRpc(payload);
            // 树形扩散：优先使用 SyncRelay；若关闭则直接发送
            var cfg = Arch.Net.NetworkSettings.Config;
            if (cfg != null && cfg.EnableSyncRelay)
                SyncRelayService.SendUp(pkt);
            else
                NetworkSingleton.Session.Send(pkt, pkt.Length);

            // clear
            s_created.Clear(); s_destroyed.Clear(); s_added.Clear(); s_removed.Clear();
        }

        private static int VarIntSize(uint v) { int n = 0; do { n++; v >>= 7; } while (v != 0); return n; }
        private static int SumVarInt(HashSet<int> set) { int s = 0; foreach (var v in set) s += VarIntSize((uint)v); return s; }
        private static void WriteVarUInt(byte[] buf, ref int p, uint v) { while (v >= 0x80) { buf[p++] = (byte)(v | 0x80); v >>= 7; } buf[p++] = (byte)v; }
        private static void WriteUInt64(byte[] buf, ref int p, ulong v)
        {
            unchecked
            {
                buf[p + 0] = (byte)(v);
                buf[p + 1] = (byte)(v >> 8);
                buf[p + 2] = (byte)(v >> 16);
                buf[p + 3] = (byte)(v >> 24);
                buf[p + 4] = (byte)(v >> 32);
                buf[p + 5] = (byte)(v >> 40);
                buf[p + 6] = (byte)(v >> 48);
                buf[p + 7] = (byte)(v >> 56);
                p += 8;
            }
        }
    }
}
