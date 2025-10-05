using Arch;
using Arch.Core;
using Attributes;
using System;

namespace Arch.Net
{
    /// <summary>
    /// 同步扫描系统（块级 Chunk 版）基于回调构包：fill 直接从组件缓冲 memcpy/写入 delta。
    /// </summary>
    [System]
    public sealed class SyncChunkScanSystem : GlobalUpdateSystem<NetworkRuntime>
    {
        protected override void Run(Entity entity, ref NetworkRuntime runtime)
        {
            if (!Arch.Net.NetworkSettings.Config.UseChunkScan) return;
            var session = NetworkSingleton.Session;
            if (session == null) return;

            int maxBatch = Arch.Net.NetworkSettings.Config.EntitiesPerPacket;

            var syncTypes = SyncTypeCache.GetAll();
            var processedArch = new System.Collections.Generic.HashSet<uint>();
            for (int tIdx = 0; tIdx < syncTypes.Count; tIdx++)
            {
                var tMeta = syncTypes[tIdx];
                uint archId = tMeta.ArchId;
                if (!processedArch.Add(archId)) continue;
                if (!world.TryGetArchetype(archId, out var archetype)) continue;

                ChunkRangeIterator chunks;
                if (!world.TryGetChunks(archId, out chunks)) continue;

                foreach (var chunk in chunks)
                {
                    // 收集当前 Archetype 存在的同步组件
                    var segTypes = new System.Collections.Generic.List<(int typeId, int typeIndex)>(syncTypes.Count);
                    for (int s = 0; s < syncTypes.Count; s++)
                    {
                        var sm = syncTypes[s];
                        if (archetype.TryGetTypeIndex(sm.TypeId, out var idx2)) segTypes.Add((sm.TypeId, idx2));
                    }
                    if (segTypes.Count == 0) continue;

                    if (!world.TryGetComponentBuffer(in chunk, segTypes[0].typeIndex, out var _, out int _, out int totalCount, out int _))
                        continue;
                    world.ReleaseComponentBuffer(IntPtr.Zero);

                    // 可选 Owned mask
                    bool[] ownedMask = null;
                    int ownerTypeId;
                    try { ownerTypeId = Arch.ComponentRegistryExtensions.GetTypeId(typeof(NetworkOwner)); } catch { ownerTypeId = -1; }
                    if (ownerTypeId > 0 && archetype.TryGetTypeIndex(ownerTypeId, out var ownerTypeIndex))
                    {
                        if (world.TryGetComponentBuffer(in chunk, ownerTypeIndex, out var pOwner, out int _, out int ownerCount, out int ownerStride))
                        {
                            ownedMask = new bool[ownerCount];
                            unsafe
                            {
                                byte* pBase = (byte*)pOwner;
                                for (int i = 0; i < ownerCount; i++)
                                {
                                    int clientId = System.Runtime.CompilerServices.Unsafe.ReadUnaligned<int>(pBase + i * ownerStride);
                                    ownedMask[i] = (clientId == OwnershipService.MyClientId);
                                }
                            }
                            world.ReleaseComponentBuffer(IntPtr.Zero);
                        }
                    }

                    int remaining = totalCount;
                    int baseIndex = 0;
                    while (remaining > 0)
                    {
                        int batch = remaining > maxBatch ? maxBatch : remaining;

                        if (ownedMask != null)
                        {
                            int windowStart = baseIndex;
                            int windowEnd = baseIndex + batch;
                            int i = windowStart;
                            while (i < windowEnd)
                            {
                                while (i < windowEnd && !ownedMask[i]) i++;
                                if (i >= windowEnd) break;
                                int runStart = i;
                                while (i < windowEnd && ownedMask[i]) i++;
                                int runEnd = i;
                                int runLen = runEnd - runStart;
                                if (runLen <= 0) continue;

                                var meta = new (int typeId, int elemSize, byte flags, int length)[segTypes.Count];
                                var useDelta = new bool[segTypes.Count];
                                for (int si = 0; si < segTypes.Count; si++)
                                {
                                    var st = segTypes[si];
                                    if (!world.TryGetComponentBuffer(in chunk, st.typeIndex, out var pPtrX, out int eSize, out int _, out int strideX))
                                        continue;
                                    int rawLen = runLen * eSize;
                                    int len = rawLen;
                                    byte flags = 0;
                                    if (IsTypeDelta(st.typeId))
                                    {
                                        int dlen = SenderDeltaCache.GetDeltaEncodedLengthRaw(archId, chunk.GetUid(), st.typeId, runStart, runLen, pPtrX, strideX, eSize);
                                        if (dlen > 0 && dlen < rawLen) { len = dlen; flags = 0x1; useDelta[si] = true; }
                                    }
                                    world.ReleaseComponentBuffer(IntPtr.Zero);
                                    meta[si] = (st.typeId, eSize, flags, len);
                                }

                                uint chunkUid = chunk.GetUid();
                                var packet = PacketBuilder.BuildSyncSegments(0, archId, chunkUid, (ushort)runStart, (ushort)runLen, meta, (segIndex, dst) =>
                                {
                                    var st = segTypes[segIndex];
                                    if (world.TryGetComponentBuffer(in chunk, st.typeIndex, out var pPtr2, out int eSize2, out int _, out int stride2))
                                    {
                                        if (useDelta[segIndex])
                                        {
                                            SenderDeltaCache.WriteDeltaToSpanRaw(archId, chunk.GetUid(), st.typeId, runStart, runLen, pPtr2, stride2, eSize2, dst);
                                        }
                                        else
                                        {
                                            int bytes = runLen * eSize2;
                                            unsafe { byte* pSrcBase = (byte*)pPtr2 + runStart * stride2; new ReadOnlySpan<byte>(pSrcBase, bytes).CopyTo(dst); }
                                        }
                                        world.ReleaseComponentBuffer(IntPtr.Zero);
                                    }
                                });
                                SyncRelayService.SendUp(packet);
                            }
                        }
                        else
                        {
                            var meta = new (int typeId, int elemSize, byte flags, int length)[segTypes.Count];
                            var useDelta = new bool[segTypes.Count];
                            for (int si = 0; si < segTypes.Count; si++)
                            {
                                var st = segTypes[si];
                                if (!world.TryGetComponentBuffer(in chunk, st.typeIndex, out var pPtrX, out int eSize, out int _, out int strideX))
                                    continue;
                                int rawLen = batch * eSize;
                                int len = rawLen;
                                byte flags = 0;
                                if (IsTypeDelta(st.typeId))
                                {
                                    int dlen = SenderDeltaCache.GetDeltaEncodedLengthRaw(archId, chunk.GetUid(), st.typeId, baseIndex, batch, pPtrX, strideX, eSize);
                                    if (dlen > 0 && dlen < rawLen) { len = dlen; flags = 0x1; useDelta[si] = true; }
                                }
                                world.ReleaseComponentBuffer(IntPtr.Zero);
                                meta[si] = (st.typeId, eSize, flags, len);
                            }
                            uint chunkUid = chunk.GetUid();
                            var packet = PacketBuilder.BuildSyncSegments(0, archId, chunkUid, (ushort)baseIndex, (ushort)batch, meta, (segIndex, dst) =>
                            {
                                var st = segTypes[segIndex];
                                if (world.TryGetComponentBuffer(in chunk, st.typeIndex, out var pPtr2, out int eSize2, out int _, out int stride2))
                                {
                                    if (useDelta[segIndex])
                                    {
                                        SenderDeltaCache.WriteDeltaToSpanRaw(archId, chunk.GetUid(), st.typeId, baseIndex, batch, pPtr2, stride2, eSize2, dst);
                                    }
                                    else
                                    {
                                        int bytes = batch * eSize2;
                                        unsafe { byte* pSrcBase = (byte*)pPtr2 + baseIndex * stride2; new ReadOnlySpan<byte>(pSrcBase, bytes).CopyTo(dst); }
                                    }
                                    world.ReleaseComponentBuffer(IntPtr.Zero);
                                }
                            });
                            SyncRelayService.SendUp(packet);
                        }

                        baseIndex += batch;
                        remaining -= batch;
                    }
                }
            }
        }

        private static bool IsTypeDelta(int typeId)
        {
            var all = SyncTypeCache.GetAll();
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].TypeId == typeId) return all[i].HasSyncDelta;
            }
            return false;
        }
    }
}
