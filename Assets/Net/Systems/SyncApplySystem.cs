using Arch;
using Arch.Core;
using Arch.Tools;
using System;
using System.Runtime.CompilerServices;

namespace Arch.Net
{
    /// <summary>
    /// 同步应用系统（接收端）
    /// - 负责：从入队列读取 Sync 包，解析 Header/Segments，将数据写回目标世界。
    /// - 路径：
    ///   1) 段编码（Segments）：按段解析（含 NetworkEntityId 段/组件段/Delta 段），必要时创建/对齐接收端实体，再写入组件。
    ///   2) 单组件编码：尝试 Chunk 级 blit（ChunkAccess），失败则走 ComponentApplierRegistry 回退路径。
    /// - 性能：尽量使用 Unsafe 的无对齐读写；按帧限流（PacketsPerFrame）。
    /// </summary>
    [System]
    [Last]
    public sealed class SyncApplySystem : GlobalUpdateSystem<NetworkRuntime>
    {
        private static int s_applyLogCounter;
        // 单元素 int[] 缓存，避免频繁的临时分配
        private static readonly System.Collections.Generic.Dictionary<int, int[]> s_singleTypeArrayCache = new System.Collections.Generic.Dictionary<int, int[]>(128);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int[] GetSingleTypeArray(int typeId)
        {
            if (!s_singleTypeArrayCache.TryGetValue(typeId, out var arr))
            {
                arr = new int[1] { typeId };
                s_singleTypeArrayCache[typeId] = arr;
            }
            return arr;
        }
        protected override void Run(Entity entity, ref NetworkRuntime runtime)
        {
            // Apply any pending structural command groups before processing Sync snapshots
            StructCommandQueue.DrainApply();
            int nProcessed = 0;
            var cfgLog = Arch.Net.NetworkSettings.Config;
            bool enableLog = cfgLog != null && cfgLog.EnableSyncApplyLog;
            int logSample = (cfgLog != null && cfgLog.SyncApplyLogSampleRate > 0) ? cfgLog.SyncApplyLogSampleRate : 100;
            SyncIncomingQueue.SyncPacket pkt;
            while (SyncIncomingQueue.TryDequeue(out pkt))
            {
                try
                {
                // Resolve world by header
                if (!NamedWorld.TryGetById(pkt.Header.WorldId, out var targetWorld))
                {
                    ArchLog.LogWarning($"[SyncApply] Unknown WorldId={pkt.Header.WorldId}");
                    continue;
                }

                // Try resolve archetype typeIds (may be null for segmented codec if not needed)
                ArchetypeRegistry.TryResolveTypeIds(pkt.Header.ArchetypeId, out var typeIds);
                if (enableLog && (System.Threading.Interlocked.Increment(ref s_applyLogCounter) % logSample) == 0)
                {
                    ArchLog.LogInfo($"[SyncApply] pkt codec={pkt.Header.Codec} world={pkt.Header.WorldId} arch={pkt.Header.ArchetypeId} chunk={pkt.Header.ChunkId} base={pkt.Header.EntityBase} count={pkt.Header.EntityCount}");
                }

                if (pkt.Header.Codec == CodecType.Segments)
                {
                    // Multi-segment payload: [segCount][(typeId)(elemSize)(len)(bytes)]*
                    int p = pkt.Offset;
                    int end = pkt.Offset + pkt.Length;
                    if (p >= end) { nProcessed++; continue; }
                    byte segCount = pkt.Buffer[p++];
                    // Pre-scan to find NetworkEntityId segment (if present)
                    int saveP = p;
                    int netIdTypeId = Arch.ComponentRegistryExtensions.GetTypeId(typeof(NetworkEntityId));
                    ulong[] ids = null;
                    int q = saveP;
                    for (int si = 0; si < segCount && q < end; si++)
                    {
                        uint tId = ReadVarUInt(pkt.Buffer, ref q, end);
                        uint eSize = ReadVarUInt(pkt.Buffer, ref q, end);
                        byte sFlags = pkt.Buffer[q++];
                        uint blen = ReadVarUInt(pkt.Buffer, ref q, end);
                        if (tId == (uint)netIdTypeId && (int)eSize == sizeof(ulong) && blen >= (uint)(pkt.Header.EntityCount * sizeof(ulong)))
                        {
                            ids = new ulong[pkt.Header.EntityCount];
                            for (int ii = 0; ii < pkt.Header.EntityCount; ii++)
                            {
                                ids[ii] = ReadUInt64(pkt.Buffer, q + ii * (int)eSize);
                                NetworkEntityRegistry.MarkSeen(ids[ii]);
                            }
                        }
                        q += (int)blen;
                    }
                    // Ensure receiver-side entities exist if we know archetype and chunk mapping
                    if (ids == null && typeIds != null && typeIds.Length > 0)
                    {
                        ReceiverEntityAllocator.EnsureRange(targetWorld, pkt.Header.ArchetypeId, pkt.Header.ChunkId, pkt.Header.EntityBase, (int)pkt.Header.EntityCount, typeIds);
                        ReceiverEntityAllocator.BumpExpectedCount(targetWorld, pkt.Header.ArchetypeId, pkt.Header.ChunkId, pkt.Header.EntityBase + (int)pkt.Header.EntityCount);
                    }
                    // Process segments
                    for (int i = 0; i < segCount && p < end; i++)
                    {
                        uint typeId = ReadVarUInt(pkt.Buffer, ref p, end);
                        uint elemSize = ReadVarUInt(pkt.Buffer, ref p, end);
                        byte segFlags = pkt.Buffer[p++];
                        uint byteLen = ReadVarUInt(pkt.Buffer, ref p, end);
                        if (p + byteLen > end) break;
                        if (InterpolationRegistry.TryGet((int)typeId, out var desc))
                        {
                            byte[] segBytes;
                            if ((segFlags & 0x1) != 0)
                            {
                                segBytes = ReceiverDeltaCache.ApplyDelta(pkt.Header.ArchetypeId, pkt.Header.ChunkId, (int)typeId, pkt.Header.EntityBase, (int)pkt.Header.EntityCount, pkt.Buffer, p, (int)byteLen, (int)elemSize);
                            }
                            else
                            {
                                segBytes = new byte[byteLen];
                                System.Buffer.BlockCopy(pkt.Buffer, p, segBytes, 0, (int)byteLen);
                                ReceiverDeltaCache.UpdateBaselineRaw(pkt.Header.ArchetypeId, pkt.Header.ChunkId, (int)typeId, pkt.Header.EntityBase, (int)pkt.Header.EntityCount, segBytes, (int)elemSize);
                            }
                            long nowMs = (long)UnityEngine.Time.realtimeSinceStartupAsDouble * 1000L;
                            InterpolationCache.SetTarget(pkt.Header.ArchetypeId, pkt.Header.ChunkId, (int)typeId, pkt.Header.EntityBase, (int)pkt.Header.EntityCount, segBytes, (int)elemSize, desc.WindowMs, nowMs);
                        }
                        else
                        {
                            if ((segFlags & 0x1) != 0)
                            {
                                var raw = ReceiverDeltaCache.ApplyDelta(pkt.Header.ArchetypeId, pkt.Header.ChunkId, (int)typeId, pkt.Header.EntityBase, (int)pkt.Header.EntityCount, pkt.Buffer, p, (int)byteLen, (int)elemSize);
                                if (!ChunkAccess.TryBlitComponentRange(targetWorld, pkt.Header.ArchetypeId, pkt.Header.ChunkId, (int)typeId, pkt.Header.EntityBase, (int)pkt.Header.EntityCount, raw, 0, (int)elemSize))
                                {
                                    if (!ComponentApplierRegistry.TryApply((int)typeId, targetWorld, raw, 0, (int)pkt.Header.EntityCount, (int)elemSize))
                                    {
                                        // Last resort: allocate and apply directly
                                        if (ids != null)
                                        {
                                            ReceiverEntityAllocator.TryApplyRawByIds(targetWorld, (int)typeId, ids, raw, 0, (int)elemSize);
                                        }
                                        else
                                        {
                                        ReceiverEntityAllocator.EnsureRange(targetWorld, pkt.Header.ArchetypeId, pkt.Header.ChunkId, pkt.Header.EntityBase, (int)pkt.Header.EntityCount, typeIds ?? GetSingleTypeArray((int)typeId));
                                            ReceiverEntityAllocator.BumpExpectedCount(targetWorld, pkt.Header.ArchetypeId, pkt.Header.ChunkId, pkt.Header.EntityBase + (int)pkt.Header.EntityCount);
                                            ReceiverEntityAllocator.TryApplyRaw(targetWorld, pkt.Header.ArchetypeId, pkt.Header.ChunkId, (int)typeId, pkt.Header.EntityBase, (int)pkt.Header.EntityCount, raw, 0, (int)elemSize);
                                        }
                                    }
                                }
                            }
                            else if (!ChunkAccess.TryBlitComponentRange(targetWorld, pkt.Header.ArchetypeId, pkt.Header.ChunkId, (int)typeId, pkt.Header.EntityBase, (int)pkt.Header.EntityCount, pkt.Buffer, p, (int)elemSize))
                            {
                                if (!ComponentApplierRegistry.TryApply((int)typeId, targetWorld, pkt.Buffer, p, (int)pkt.Header.EntityCount, (int)elemSize))
                                {
                                    if (ids != null)
                                    {
                                        ReceiverEntityAllocator.TryApplyRawByIds(targetWorld, (int)typeId, ids, pkt.Buffer, p, (int)elemSize);
                                    }
                                    else
                                    {
                                        ReceiverEntityAllocator.EnsureRange(targetWorld, pkt.Header.ArchetypeId, pkt.Header.ChunkId, pkt.Header.EntityBase, (int)pkt.Header.EntityCount, typeIds ?? GetSingleTypeArray((int)typeId));
                                        ReceiverEntityAllocator.BumpExpectedCount(targetWorld, pkt.Header.ArchetypeId, pkt.Header.ChunkId, pkt.Header.EntityBase + (int)pkt.Header.EntityCount);
                                        ReceiverEntityAllocator.TryApplyRaw(targetWorld, pkt.Header.ArchetypeId, pkt.Header.ChunkId, (int)typeId, pkt.Header.EntityBase, (int)pkt.Header.EntityCount, pkt.Buffer, p, (int)elemSize);
                                    }
                                }
                            }
                        }
                        p += (int)byteLen;
                    }
                }
                else
                {
                    // Single-component payload
                    if (typeIds == null || typeIds.Length == 0 || pkt.Header.EntityCount == 0)
                    {
                        nProcessed++;
                        if (nProcessed >= Arch.Net.NetworkSettings.Config.PacketsPerFrame) break;
                        continue;
                    }
                    int compSize = pkt.Length / pkt.Header.EntityCount;
                    if (compSize <= 0 || compSize * pkt.Header.EntityCount > pkt.Length)
                    {
                        ArchLog.LogWarning("[SyncApply] Invalid payload size");
                    }
                    else
                    {
                        if (!ChunkAccess.TryBlitComponentRange(targetWorld, pkt.Header.ArchetypeId, pkt.Header.ChunkId, typeIds[0], pkt.Header.EntityBase, (int)pkt.Header.EntityCount, pkt.Buffer, pkt.Offset, compSize))
                        {
                            if (!ComponentApplierRegistry.TryApply(typeIds[0], targetWorld, pkt.Buffer, pkt.Offset, (int)pkt.Header.EntityCount, compSize))
                            {
                                ReceiverEntityAllocator.EnsureRange(targetWorld, pkt.Header.ArchetypeId, pkt.Header.ChunkId, pkt.Header.EntityBase, (int)pkt.Header.EntityCount, typeIds);
                                ReceiverEntityAllocator.BumpExpectedCount(targetWorld, pkt.Header.ArchetypeId, pkt.Header.ChunkId, pkt.Header.EntityBase + (int)pkt.Header.EntityCount);
                                ReceiverEntityAllocator.TryApplyRaw(targetWorld, pkt.Header.ArchetypeId, pkt.Header.ChunkId, typeIds[0], pkt.Header.EntityBase, (int)pkt.Header.EntityCount, pkt.Buffer, pkt.Offset, compSize);
                            }
                        }
                    }

                    nProcessed++;
                    if (nProcessed >= Arch.Net.NetworkSettings.Config.PacketsPerFrame) break;
                }
                }
                catch (Exception ex)
                {
                    ArchLog.LogWarning($"[SyncApply] Exception processing pkt: {ex.Message}");
                }
            }

            // Reconcile receiver entity counts once per frame
            ReceiverEntityAllocator.ReconcileEndOfFrame();
            // Cull stale ids using config
            var cfg = Arch.Net.NetworkSettings.Config;
            int staleFrames = cfg?.StaleIdCullFrames > 0 ? cfg.StaleIdCullFrames : 5;
            NetworkEntityRegistry.CullStale(Arch.NamedWorld.DefaultWord, staleFrames);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong ReadUInt64(byte[] buf, int offset)
        {
            unchecked
            {
                return (ulong)buf[offset + 0]
                     | ((ulong)buf[offset + 1] << 8)
                     | ((ulong)buf[offset + 2] << 16)
                     | ((ulong)buf[offset + 3] << 24)
                     | ((ulong)buf[offset + 4] << 32)
                     | ((ulong)buf[offset + 5] << 40)
                     | ((ulong)buf[offset + 6] << 48)
                     | ((ulong)buf[offset + 7] << 56);
            }
        }
    }
}
