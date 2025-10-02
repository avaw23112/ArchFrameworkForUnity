using System;
using System.Collections.Generic;

namespace Arch.Net
{
    public struct InterpKey : IEquatable<InterpKey>
    {
        public uint ArchId;
        public uint ChunkUid;
        public int TypeId;
        public int Base;
        public int Count;
        public bool Equals(InterpKey other) => ArchId == other.ArchId && ChunkUid == other.ChunkUid && TypeId == other.TypeId && Base == other.Base && Count == other.Count;
        public override int GetHashCode() => HashCode.Combine(ArchId, ChunkUid, TypeId, Base, Count);
    }

    /// <summary>
    /// Stores target byte buffers for interpolation per (archetype,chunk,type,range), with last timestamp.
    /// </summary>
    public static class InterpolationCache
    {
        private sealed class Entry
        {
            public byte[] Target;
            public int ElemSize;
            public long TimestampMs;
            public int WindowMs;
        }
        private static readonly Dictionary<InterpKey, Entry> s_map = new Dictionary<InterpKey, Entry>();

        public static void SetTarget(uint archId, uint chunkUid, int typeId, int baseIndex, int count, byte[] bytes, int elemSize, int windowMs, long tsMs)
        {
            var key = new InterpKey { ArchId = archId, ChunkUid = chunkUid, TypeId = typeId, Base = baseIndex, Count = count };
            if (!s_map.TryGetValue(key, out var e))
            {
                e = new Entry(); s_map[key] = e;
            }
            e.Target = bytes; e.ElemSize = elemSize; e.TimestampMs = tsMs; e.WindowMs = windowMs;
        }

        public static IEnumerable<(InterpKey key, byte[] target, int elemSize, int windowMs, long tsMs)> Entries()
        {
            foreach (var kv in s_map)
            {
                var e = kv.Value; yield return (kv.Key, e.Target, e.ElemSize, e.WindowMs, e.TimestampMs);
            }
        }
    }
}

