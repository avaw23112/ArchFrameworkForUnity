using System;
using System.Buffers;
using System.Collections.Generic;

namespace Arch.Net
{
    internal struct DeltaKey : IEquatable<DeltaKey>
    {
        public uint ArchId;
        public uint ChunkUid;
        public int TypeId;
        public bool Equals(DeltaKey other) => ArchId == other.ArchId && ChunkUid == other.ChunkUid && TypeId == other.TypeId;
        public override int GetHashCode() => HashCode.Combine(ArchId, ChunkUid, TypeId);
    }

    /// <summary>
    /// 发送端 Delta 基线缓存（每 (archId, chunkUid, typeId) 一份）
    /// - 目的：比较当前批次与 Baseline，生成变化掩码和差异负载；若无需发送则返回 null。
    /// - 更新：发送后更新 Baseline，确保下一帧对比正确。
    /// </summary>
    public static class SenderDeltaCache
    {
        private sealed class Entry
        {
            public byte[] Baseline; // entire chunk bytes (contiguous by entity index)
            public int ElemSize;
            public int Count; // last known entity count
        }
        private static readonly Dictionary<DeltaKey, Entry> s_map = new Dictionary<DeltaKey, Entry>();

        private static Entry Ensure(uint archId, uint chunkUid, int typeId, int totalCount, int elemSize)
        {
            var key = new DeltaKey { ArchId = archId, ChunkUid = chunkUid, TypeId = typeId };
            if (!s_map.TryGetValue(key, out var e)) { e = new Entry(); s_map[key] = e; }
            int needBytes = totalCount * elemSize;
            if (e.Baseline == null || e.Baseline.Length < needBytes || e.ElemSize != elemSize)
            {
                e.Baseline = new byte[needBytes]; e.ElemSize = elemSize; e.Count = totalCount;
            }
            return e;
        }

        /// <summary>
        /// Build delta payload for a batch if beneficial: [maskLen varint][mask bytes][delta bytes]. Returns true; deltaPayload is null if no change.
        /// </summary>
        public static bool TryBuildDelta(uint archId, uint chunkUid, int typeId, int baseIndex, int count, byte[] rawBatch, int elemSize, out byte[] deltaPayload)
        {
            deltaPayload = null;
            int totalCount = baseIndex + count; // minimal baseline size
            var e = Ensure(archId, chunkUid, typeId, totalCount, elemSize);
            int maskLen = (count + 7) >> 3;
            var mask = ArrayPool<byte>.Shared.Rent(maskLen);
            var deltasBuf = ArrayPool<byte>.Shared.Rent(count * elemSize);
            int deltasWritten = 0;
            int changed = 0;
            for (int i = 0; i < count; i++)
            {
                int srcOff = i * elemSize;
                int baseOff = (baseIndex + i) * elemSize;
                bool diff = false;
                for (int b = 0; b < elemSize; b++)
                {
                    if (e.Baseline[baseOff + b] != rawBatch[srcOff + b]) { diff = true; break; }
                }
                if (diff)
                {
                    mask[i >> 3] |= (byte)(1 << (i & 7));
                    var src = new ReadOnlySpan<byte>(rawBatch, srcOff, elemSize);
                    src.CopyTo(new Span<byte>(deltasBuf, deltasWritten, elemSize));
                    deltasWritten += elemSize;
                    changed++;
                }
            }
            if (changed == 0)
            {
                try { ArrayPool<byte>.Shared.Return(mask); } catch { }
                try { ArrayPool<byte>.Shared.Return(deltasBuf); } catch { }
                return true; // nothing to send
            }
            // Compose payload: [maskLen varint][mask][delta]
            int payloadLen = VarIntSize((uint)maskLen) + maskLen + deltasWritten;
            deltaPayload = new byte[payloadLen];
            int p = 0;
            WriteVarUInt(deltaPayload, ref p, (uint)maskLen);
            System.Buffer.BlockCopy(mask, 0, deltaPayload, p, maskLen); p += maskLen;
            System.Buffer.BlockCopy(deltasBuf, 0, deltaPayload, p, deltasWritten);
            try { ArrayPool<byte>.Shared.Return(mask); } catch { }
            try { ArrayPool<byte>.Shared.Return(deltasBuf); } catch { }
            return true;
        }

        public static void UpdateBaseline(uint archId, uint chunkUid, int typeId, int baseIndex, int count, byte[] data, int elemSize, bool isRaw)
        {
            int totalCount = baseIndex + count;
            var e = Ensure(archId, chunkUid, typeId, totalCount, elemSize);
            if (isRaw)
            {
                System.Buffer.BlockCopy(data, 0, e.Baseline, baseIndex * elemSize, count * elemSize);
            }
            else
            {
                // data is delta payload: [maskLen varint][mask][delta]
                int p = 0;
                uint maskLen = ReadVarUInt(data, ref p, data.Length);
                var mask = new ReadOnlySpan<byte>(data, p, (int)maskLen); p += (int)maskLen;
                var del = new ReadOnlySpan<byte>(data, p, data.Length - p);
                int dOff = 0;
                for (int i = 0; i < count; i++)
                {
                    if ((mask[i >> 3] & (1 << (i & 7))) != 0)
                    {
                        del.Slice(dOff, elemSize).CopyTo(new Span<byte>(e.Baseline, (baseIndex + i) * elemSize, elemSize));
                        dOff += elemSize;
                    }
                }
            }
        }

        // 计算 Delta 编码后的负载长度（不写入，仅统计变化数量）。传入组件缓冲起始指针与 stride。
        public static unsafe int GetDeltaEncodedLengthRaw(uint archId, uint chunkUid, int typeId, int baseIndex, int count, IntPtr pSrcBase, int stride, int elemSize)
        {
            int totalCount = baseIndex + count;
            var e = Ensure(archId, chunkUid, typeId, totalCount, elemSize);
            int changed = 0;
            byte* pBase = (byte*)pSrcBase;
            for (int i = 0; i < count; i++)
            {
                int baseOff = (baseIndex + i) * elemSize;
                byte* pElem = pBase + i * stride;
                bool diff = false;
                for (int b = 0; b < elemSize; b++)
                {
                    if (e.Baseline[baseOff + b] != *(pElem + b)) { diff = true; break; }
                }
                if (diff) changed++;
            }
            int maskLen = (count + 7) >> 3;
            return VarIntSize((uint)maskLen) + maskLen + changed * elemSize;
        }

        // 将 Delta 直接写入目标 span：[maskLen varint][mask][delta] 并更新 Baseline。
        public static unsafe void WriteDeltaToSpanRaw(uint archId, uint chunkUid, int typeId, int baseIndex, int count, IntPtr pSrcBase, int stride, int elemSize, Span<byte> dst)
        {
            int totalCount = baseIndex + count;
            var e = Ensure(archId, chunkUid, typeId, totalCount, elemSize);
            int p = 0;
            int maskLen = (count + 7) >> 3;
            // 写 maskLen（varint）
            uint v = (uint)maskLen;
            while (v >= 0x80) { dst[p++] = (byte)(v | 0x80); v >>= 7; }
            dst[p++] = (byte)v;
            // mask 区域
            var maskSpan = dst.Slice(p, maskLen);
            maskSpan.Clear();
            p += maskLen;
            var deltaSpan = dst.Slice(p);
            int dOff = 0;
            byte* pBase = (byte*)pSrcBase;
            for (int i = 0; i < count; i++)
            {
                int baseOff = (baseIndex + i) * elemSize;
                byte* pElem = pBase + i * stride;
                bool diff = false;
                for (int b = 0; b < elemSize; b++)
                {
                    if (e.Baseline[baseOff + b] != *(pElem + b)) { diff = true; break; }
                }
                if (diff)
                {
                    maskSpan[i >> 3] |= (byte)(1 << (i & 7));
                    new ReadOnlySpan<byte>(pElem, elemSize).CopyTo(deltaSpan.Slice(dOff, elemSize));
                    // 更新 Baseline
                    new ReadOnlySpan<byte>(pElem, elemSize).CopyTo(new Span<byte>(e.Baseline, baseOff, elemSize));
                    dOff += elemSize;
                }
            }
        }

        private static int VarIntSize(uint v)
        {
            int n = 0;
            do { n++; v >>= 7; }
            while (v != 0);
            return n;
        }
        private static void WriteVarUInt(byte[] buf, ref int p, uint v)
        {
            while (v >= 0x80)
            {
                buf[p++] = (byte)(v | 0x80);
                v >>= 7;
            }
            buf[p++] = (byte)v;
        }
        private static uint ReadVarUInt(byte[] buf, ref int p, int end)
        {
            uint val = 0;
            int sh = 0; while (p < end)
            {
                byte b = buf[p++];
                val |= (uint)(b & 0x7F) << sh;
                if ((b & 0x80) == 0) break; sh += 7;
            }
            return val;
        }
    }
}
