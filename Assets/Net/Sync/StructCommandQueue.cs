using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;

namespace Arch.Net
{
    /// <summary>
    /// Thread-safe queue for structural command groups and helpers to apply them.
    /// Ensures groups are applied before Sync snapshots in SyncApplySystem.
    /// </summary>
    public static class StructCommandQueue
    {
        private static readonly ConcurrentQueue<byte[]> s_queue = new ConcurrentQueue<byte[]>();
        private static readonly Dictionary<int, uint> s_lastSeqBySender = new Dictionary<int, uint>();

        public static void Enqueue(byte[] payload)
        {
            if (payload == null || payload.Length == 0) return;
            s_queue.Enqueue(payload);
        }

        public static void DrainApply()
        {
            while (s_queue.TryDequeue(out var p))
            {
                try
                {
                    ApplySingle(p);
                }
                catch { /* ignore */ }
            }
        }

        private static void ApplySingle(byte[] payload)
        {
            int p = 0; int end = payload.Length;
            if (p >= end) return;
            // First byte should be RpcIds.StructCommandGroup, but some transports may pass only payload without id; handle both
            byte first = payload[p];
            if (first == (byte)RpcIds.StructCommandGroup) p++;

            uint worldId = ReadVarUInt(payload, ref p, end);
            int senderId = (int)ReadVarUInt(payload, ref p, end);
            uint seq = ReadVarUInt(payload, ref p, end);

            if (s_lastSeqBySender.TryGetValue(senderId, out var last) && seq <= last)
            {
                return; // drop old/duplicate
            }
            s_lastSeqBySender[senderId] = seq;

            if (!NamedWorld.TryGetById(worldId, out var world)) world = NamedWorld.DefaultWord;
            if (world == null) return;

            // Decode create list
            uint createCount = ReadVarUInt(payload, ref p, end);
            for (uint i = 0; i < createCount && p < end; i++)
            {
                ulong id = ReadUInt64(payload, ref p, end);
                if (!NetworkEntityRegistry.TryGet(id, out var e) || !e.isVaild())
                {
                    e = world.Create<NetworkEntityId, NetworkOwner>();
                    e.Setter((ref NetworkEntityId nid) => nid.Value = id);
                    NetworkEntityRegistry.Register(id, in e);
                }
                NetworkEntityRegistry.MarkSeen(id);
            }

            // Decode destroy list
            uint destroyCount = ReadVarUInt(payload, ref p, end);
            for (uint i = 0; i < destroyCount && p < end; i++)
            {
                ulong id = ReadUInt64(payload, ref p, end);
                if (NetworkEntityRegistry.TryGet(id, out var e) && world.IsAlive(e) && e.isVaild())
                {
                    world.Destroy(e);
                    NetworkEntityRegistry.Unregister(id);
                }
            }

            // Decode add list
            uint addCount = ReadVarUInt(payload, ref p, end);
            for (uint i = 0; i < addCount && p < end; i++)
            {
                ulong id = ReadUInt64(payload, ref p, end);
                uint tcount = ReadVarUInt(payload, ref p, end);
                if (!NetworkEntityRegistry.TryGet(id, out var e) || !e.isVaild())
                {
                    e = world.Create<NetworkEntityId, NetworkOwner>();
                    e.Setter((ref NetworkEntityId nid) => nid.Value = id);
                    NetworkEntityRegistry.Register(id, in e);
                }
                NetworkEntityRegistry.MarkSeen(id);
                for (uint k = 0; k < tcount && p < end; k++)
                {
                    int typeId = (int)ReadVarUInt(payload, ref p, end);
                    var t = Arch.ComponentRegistryExtensions.GetType(typeId);
                    if (t != null) AddComponentViaReflection(e, t);
                }
            }

            // Decode remove list
            uint remCount = ReadVarUInt(payload, ref p, end);
            for (uint i = 0; i < remCount && p < end; i++)
            {
                ulong id = ReadUInt64(payload, ref p, end);
                uint tcount = ReadVarUInt(payload, ref p, end);
                if (!NetworkEntityRegistry.TryGet(id, out var e) || !e.isVaild())
                {
                    // nothing to remove
                    for (uint k = 0; k < tcount; k++) ReadVarUInt(payload, ref p, end);
                    continue;
                }
                for (uint k = 0; k < tcount && p < end; k++)
                {
                    int typeId = (int)ReadVarUInt(payload, ref p, end);
                    var t = Arch.ComponentRegistryExtensions.GetType(typeId);
                    if (t != null) RemoveComponentViaReflection(e, t);
                }
            }
        }

        // helpers
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
        private static ulong ReadUInt64(byte[] buf, ref int p, int end)
        {
            if (p + 8 > end) return 0UL;
            ulong v = (ulong)buf[p + 0]
                     | ((ulong)buf[p + 1] << 8)
                     | ((ulong)buf[p + 2] << 16)
                     | ((ulong)buf[p + 3] << 24)
                     | ((ulong)buf[p + 4] << 32)
                     | ((ulong)buf[p + 5] << 40)
                     | ((ulong)buf[p + 6] << 48)
                     | ((ulong)buf[p + 7] << 56);
            p += 8; return v;
        }

        private static void AddComponentViaReflection(Entity entity, Type compType)
        {
            var mi = typeof(StructCommandQueue).GetMethod(nameof(AddGeneric), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            try { mi.MakeGenericMethod(compType).Invoke(null, new object[] { entity }); }
            catch { /* ignore */ }
        }
        private static void RemoveComponentViaReflection(Entity entity, Type compType)
        {
            var mi = typeof(StructCommandQueue).GetMethod(nameof(RemoveGeneric), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            try { mi.MakeGenericMethod(compType).Invoke(null, new object[] { entity }); }
            catch { /* ignore */ }
        }
        private static void AddGeneric<T>(Entity e) where T : struct, IComponent { if (!e.Has<T>()) e.Add<T>(); }
        private static void RemoveGeneric<T>(Entity e) where T : struct, IComponent { if (e.Has<T>()) e.Remove<T>(); }
    }
}

