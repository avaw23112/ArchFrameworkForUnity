using System;
using System.Collections.Generic;

namespace Arch.Net
{
    /// <summary>
    /// 接收端 Delta 基线缓存（每 (archId, chunkUid, typeId) 一份）
    /// - 目的：对端发送的是变化掩码 + 差异数据时，用本地 Baseline 还原为原始批次字节。
    /// - 更新：ApplyDelta 读掩码并覆盖差异；同时更新 Baseline。
    /// </summary>
    public static class ReceiverDeltaCache
    {
        private sealed class Entry { public byte[] Baseline; public int ElemSize; }
        private static readonly Dictionary<DeltaKey, Entry> s_map = new Dictionary<DeltaKey, Entry>();

        private static Entry Ensure(uint archId, uint chunkUid, int typeId, int totalCount, int elemSize)
        {
            var key = new DeltaKey { ArchId = archId, ChunkUid = chunkUid, TypeId = typeId };
            if (!s_map.TryGetValue(key, out var e)) { e = new Entry(); s_map[key] = e; }
            int need = totalCount * elemSize;
            if (e.Baseline == null || e.Baseline.Length < need || e.ElemSize != elemSize) { e.Baseline = new byte[need]; e.ElemSize = elemSize; }
            return e;
        }

        public static void UpdateBaselineRaw(uint archId, uint chunkUid, int typeId, int baseIndex, int count, byte[] raw, int elemSize)
        {
            var e = Ensure(archId, chunkUid, typeId, baseIndex + count, elemSize);
            System.Buffer.BlockCopy(raw, 0, e.Baseline, baseIndex * elemSize, count * elemSize);
        }

        public static byte[] ApplyDelta(uint archId, uint chunkUid, int typeId, int baseIndex, int count, byte[] deltaPayloadSrc, int offset, int length, int elemSize)
        {
            // Clone baseline slice and overlay changed elements
            var e = Ensure(archId, chunkUid, typeId, baseIndex + count, elemSize);
            var raw = new byte[count * elemSize];
            System.Buffer.BlockCopy(e.Baseline, baseIndex * elemSize, raw, 0, raw.Length);
            int p = offset;
            int end = offset + length;
            uint maskLen = ReadVarUInt(deltaPayloadSrc, ref p, end);
            var mask = new ReadOnlySpan<byte>(deltaPayloadSrc, p, (int)maskLen); p += (int)maskLen;
            int dOff = 0;
            for (int i = 0; i < count; i++)
            {
                if ((mask[i >> 3] & (1 << (i & 7))) != 0)
                {
                    new ReadOnlySpan<byte>(deltaPayloadSrc, p + dOff, elemSize).CopyTo(new Span<byte>(raw, i * elemSize, elemSize));
                    dOff += elemSize;
                }
            }
            // Update baseline with reconstructed raw
            System.Buffer.BlockCopy(raw, 0, e.Baseline, baseIndex * elemSize, raw.Length);
            return raw;
        }

        private static uint ReadVarUInt(byte[] buf, ref int p, int end) { uint v = 0; int sh = 0; while (p < end) { byte b = buf[p++]; v |= (uint)(b & 0x7F) << sh; if ((b & 0x80) == 0) break; sh += 7; } return v; }
    }
}
