using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;

namespace Arch.Net
{
    /// <summary>
    /// Allocates and tracks receive-side entities per (archId, chunkId) to support Sync-only replication.
    /// Creates entities on demand and applies component bytes when chunk blit is unavailable.
    /// </summary>
    public static class ReceiverEntityAllocator
    {
        private struct Key : IEquatable<Key>
        {
            public uint ArchId;
            public uint ChunkId;
            public bool Equals(Key other) => ArchId == other.ArchId && ChunkId == other.ChunkId;
            public override bool Equals(object obj) => obj is Key k && Equals(k);
            public override int GetHashCode() => ((int)ArchId * 397) ^ (int)ChunkId;
        }

        private static readonly Dictionary<Key, List<Entity>> s_map = new Dictionary<Key, List<Entity>>();
        private static readonly Dictionary<Key, int> s_expectedCount = new Dictionary<Key, int>();
        private static readonly Dictionary<Key, World> s_worldOfKey = new Dictionary<Key, World>();

        public static void BumpExpectedCount(World world, uint archId, uint chunkId, int need)
        {
            var key = new Key { ArchId = archId, ChunkId = chunkId };
            s_worldOfKey[key] = world;
            if (s_expectedCount.TryGetValue(key, out var curr))
            {
                if (need > curr) s_expectedCount[key] = need;
            }
            else s_expectedCount[key] = need;
        }

        public static void EnsureRange(World world, uint archId, uint chunkId, int entityBase, int count, int[] typeIds)
        {
            if (world == null || count <= 0) return;
            var key = new Key { ArchId = archId, ChunkId = chunkId };
            BumpExpectedCount(world, archId, chunkId, entityBase + count);
            if (!s_map.TryGetValue(key, out var list))
            {
                list = new List<Entity>(entityBase + count);
                s_map[key] = list;
            }
            int need = entityBase + count;
            while (list.Count < need)
            {
                var e = world.Create();
                // Add declared types for archetype if provided
                if (typeIds != null)
                {
                    for (int i = 0; i < typeIds.Length; i++)
                    {
                        var t = Arch.ComponentRegistryExtensions.GetType(typeIds[i]);
                        if (t != null) AddComponentViaReflection(e, t);
                    }
                }
                // Ensure owner exists for NetSync-controlled entities
                if (!e.Has<NetworkOwner>()) e.Add<NetworkOwner>();
                list.Add(e);
            }
        }

        public static void ReconcileEndOfFrame()
        {
            // For each tracked key, ensure list count == expected count by destroying extras
            var keys = new List<Key>(s_expectedCount.Keys);
            foreach (var key in keys)
            {
                if (!s_map.TryGetValue(key, out var list)) continue;
                int want = s_expectedCount[key];
                if (want < 0) want = 0;
                if (list.Count > want)
                {
                    if (!s_worldOfKey.TryGetValue(key, out var world) || world == null) continue;
                    for (int i = list.Count - 1; i >= want; i--)
                    {
                        var e = list[i];
                        try { if (world.IsAlive(e) && e.isVaild()) world.Destroy(e); }
                        catch { /* ignore */ }
                        list.RemoveAt(i);
                    }
                }
            }
            // Reset expectations for next frame
            s_expectedCount.Clear();
        }

        public static bool TryApplyRaw(World world, uint archId, uint chunkId, int typeId, int entityBase, int count, byte[] buffer, int offset, int elemSize)
        {
            if (world == null || count <= 0 || buffer == null || elemSize <= 0) return false;
            var key = new Key { ArchId = archId, ChunkId = chunkId };
            if (!s_map.TryGetValue(key, out var list))
            {
                // Create minimal entities with just this component
                EnsureRange(world, archId, chunkId, entityBase, count, new[] { typeId });
                list = s_map[key];
            }
            // Ensure the component exists on all targets
            var compType = Arch.ComponentRegistryExtensions.GetType(typeId);
            if (compType == null) return false;
            int end = entityBase + count;
            for (int i = 0; i < count; i++)
            {
                int idx = entityBase + i;
                if (idx < 0 || idx >= list.Count) return false;
                var e = list[idx];
                if (!e.isVaild()) return false;
                AddComponentViaReflection(e, compType);
                SetComponentFromBytesViaReflection(e, compType, buffer, offset + i * elemSize);
            }
            return true;
        }

        public static bool TryApplyRawByIds(World world, int typeId, ulong[] ids, byte[] buffer, int offset, int elemSize)
        {
            if (world == null || ids == null || ids.Length == 0 || buffer == null || elemSize <= 0) return false;
            var compType = Arch.ComponentRegistryExtensions.GetType(typeId);
            if (compType == null) return false;
            for (int i = 0; i < ids.Length; i++)
            {
                ulong id = ids[i];
                if (!NetworkEntityRegistry.TryGet(id, out var e) || !e.isVaild())
                {
                    e = world.Create<NetworkEntityId, NetworkOwner>();
                    e.Setter((ref NetworkEntityId nid) => { nid.Value = id; });
                    NetworkEntityRegistry.Register(id, in e);
                }
                NetworkEntityRegistry.MarkSeen(id);
                AddComponentViaReflection(e, compType);
                SetComponentFromBytesViaReflection(e, compType, buffer, offset + i * elemSize);
            }
            return true;
        }

        private static void AddComponentViaReflection(Entity entity, Type compType)
        {
            var mi = typeof(ReceiverEntityAllocator).GetMethod(nameof(AddGeneric), BindingFlags.Static | BindingFlags.NonPublic);
            try { mi.MakeGenericMethod(compType).Invoke(null, new object[] { entity }); }
            catch { /* ignore */ }
        }

        private static void SetComponentFromBytesViaReflection(Entity entity, Type compType, byte[] src, int srcOffset)
        {
            var mi = typeof(ReceiverEntityAllocator).GetMethod(nameof(SetFromBytesGeneric), BindingFlags.Static | BindingFlags.NonPublic);
            try { mi.MakeGenericMethod(compType).Invoke(null, new object[] { entity, src, srcOffset }); }
            catch { /* ignore */ }
        }

        private static void AddGeneric<T>(Entity entity) where T : struct, IComponent
        {
            if (!entity.Has<T>()) entity.Add<T>();
        }

        private static unsafe void SetFromBytesGeneric<T>(Entity entity, byte[] src, int srcOffset) where T : struct, IComponent
        {
            if (!entity.Has<T>()) entity.Add<T>();
            T val;
            fixed (byte* pSrc = &src[srcOffset])
            {
                val = Unsafe.ReadUnaligned<T>(pSrc);
            }
            entity.Set(in val);
        }
    }
}
