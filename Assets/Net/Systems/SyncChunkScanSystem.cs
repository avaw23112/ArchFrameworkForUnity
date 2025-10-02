using Arch;
using Arch.Core;
using Attributes;
using System;
using System.Runtime.CompilerServices;

namespace Arch.Net
{
    /// <summary>
    /// 同步扫描系统（块级 Chunk 版）
    /// - 负责：对包含 [NetworkSync] 值类型组件的 Archetype 的每个 Chunk 进行 memcpy 打包并发送。
    /// - 优化：通过 SyncTypeCache 复用同步类型列表；每个 Archetype 仅处理一次，避免重复发送。
    /// - 需求：依赖底层世界的 TryGetChunks/Archetype/ComponentBuffer 等 API 提供零拷贝访问。
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
                if (!processedArch.Add(archId)) continue; // 每个 Archetype 仅处理一次

                if (!world.TryGetArchetype(archId, out var archetype))
                    continue;

                // 枚举该原型的所有 Chunk
                ChunkRangeIterator chunks;
                if (!world.TryGetChunks(archId, out chunks))
                    continue;

                foreach (var chunk in chunks)
                {
                    // 收集“当前 Archetype 存在的同步组件”的 typeId/typeIndex 列表
                    var segTypes = new System.Collections.Generic.List<(int typeId, int typeIndex)>(syncTypes.Count);
                    for (int s = 0; s < syncTypes.Count; s++)
                    {
                        var sm = syncTypes[s];
                        if (archetype.TryGetTypeIndex(sm.TypeId, out var idx2))
                        {
                            segTypes.Add((sm.TypeId, idx2));
                        }
                    }
                    if (segTypes.Count == 0) continue;

                    // 通过首个段查询总元素个数
                    if (!world.TryGetComponentBuffer(in chunk, segTypes[0].typeIndex, out var _, out int _, out int totalCount, out int _))
                        continue;
                    world.ReleaseComponentBuffer(IntPtr.Zero);

                    // 可选：构建 Owned mask（若原型包含 NetworkOwner）
                    bool[] ownedMask = null;
                    int ownerTypeId;
                    try { ownerTypeId = Arch.ComponentRegistryExtensions.GetTypeId(typeof(NetworkOwner)); }
                    catch { ownerTypeId = -1; }
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
                                // 寻找下一段连续 owned run
                                while (i < windowEnd && !ownedMask[i]) i++;
                                if (i >= windowEnd) break;
                                int runStart = i;
                                while (i < windowEnd && ownedMask[i]) i++;
                                int runEnd = i;
                                int runLen = runEnd - runStart;
                                if (runLen <= 0) continue;

                                // 准备分段元信息（不创建中间段数据），长度为 raw bytes 长度
                                var meta = new (int typeId, int elemSize, byte flags, int length)[segTypes.Count];
                                var elemSizes = new int[segTypes.Count];
                                for (int si = 0; si < segTypes.Count; si++)
                                {
                                    var st = segTypes[si];
                                    if (!world.TryGetComponentBuffer(in chunk, st.typeIndex, out var _, out int eSize, out int _, out int _))
                                        continue;
                                    world.ReleaseComponentBuffer(IntPtr.Zero);
                                    elemSizes[si] = eSize;
                                    meta[si] = (st.typeId, eSize, (byte)0, runLen * eSize);
                                }
                                uint chunkUid = chunk.GetUid();
                                var packet = PacketBuilder.BuildSyncSegments(0, archId, chunkUid, (ushort)runStart, (ushort)runLen, meta, (segIndex, dst) =>
                                {
                                    var st = segTypes[segIndex];
                                    if (world.TryGetComponentBuffer(in chunk, st.typeIndex, out var pPtr2, out int eSize2, out int _, out int stride2))
                                    {
                                        int bytes = runLen * eSize2;
                                        unsafe
                                        {
                                            byte* pSrcBase = (byte*)pPtr2 + runStart * stride2;
                                            fixed (byte* pDst = dst)
                                            {
                                                Unsafe.CopyBlockUnaligned(pDst, pSrcBase, (uint)bytes);
                                            }
                                        }
                                        world.ReleaseComponentBuffer(IntPtr.Zero);
                                    }
                                });
                                SyncRelayService.SendUp(packet);
                            }
                        }
                        else
                        {
                            // 批量路径：同样直接在 fill 回调中从组件缓冲 memcpy 到 payload
                            var meta = new (int typeId, int elemSize, byte flags, int length)[segTypes.Count];
                            for (int si = 0; si < segTypes.Count; si++)
                            {
                                var st = segTypes[si];
                                if (!world.TryGetComponentBuffer(in chunk, st.typeIndex, out var _, out int eSize, out int _, out int _))
                                    continue;
                                world.ReleaseComponentBuffer(IntPtr.Zero);
                                meta[si] = (st.typeId, eSize, (byte)0, batch * eSize);
                            }
                            uint chunkUid = chunk.GetUid();
                            var packet = PacketBuilder.BuildSyncSegments(0, archId, chunkUid, (ushort)baseIndex, (ushort)batch, meta, (segIndex, dst) =>
                            {
                                var st = segTypes[segIndex];
                                if (world.TryGetComponentBuffer(in chunk, st.typeIndex, out var pPtr2, out int eSize2, out int _, out int stride2))
                                {
                                    int bytes = batch * eSize2;
                                    unsafe
                                    {
                                        byte* pSrcBase = (byte*)pPtr2 + baseIndex * stride2;
                                        fixed (byte* pDst = dst)
                                        {
                                            Unsafe.CopyBlockUnaligned(pDst, pSrcBase, (uint)bytes);
                                        }
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

        // 判断某 typeId 是否具备 [SyncDelta] 标记（基于缓存，避免反射）。
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
