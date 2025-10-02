using Arch.Core;
using Arch.Tools;
using System;

namespace Arch.Net
{
    /// <summary>
    /// Chunk-level accessor backed by World/Archetype/Chunk SoA APIs.
    /// Uses memcpy-like CopyFrom to write raw bytes into component storage.
    /// </summary>
    public sealed class WorldChunkAccessor : IChunkAccessor
    {
        public bool TryBlitComponentRange(
            World world,
            uint archetypeId,
            uint chunkId,
            int typeId,
            int entityBase,
            int entityCount,
            byte[] src,
            int srcOffset,
            int compSize)
        {
            if (world == null || src == null || entityCount <= 0 || compSize <= 0) return false;

            // 1) Locate chunk by (archId, chunkUid)
            if (!world.TryGetChunkByUid(archetypeId, chunkId, out var chunk))
            {
                ArchLog.LogWarning($"[ChunkAccessor] Chunk not found: arch={archetypeId} uid={chunkId}");
                return false;
            }

            // 2) Map global typeId to archetype-local typeIndex
            if (!world.TryGetArchetype(archetypeId, out var archetype))
            {
                ArchLog.LogWarning($"[ChunkAccessor] Archetype not found: {archetypeId}");
                return false;
            }
            if (!archetype.TryGetTypeIndex(typeId, out var typeIndex))
            {
                ArchLog.LogWarning($"[ChunkAccessor] TypeId {typeId} not in archetype {archetypeId}");
                return false;
            }

            // 3) Bounds and element-size sanity (optional, safe)
            if (world.TryGetComponentBuffer(in chunk, typeIndex, out var _, out int elemSize, out int totalCount, out int stride))
            {
                world.ReleaseComponentBuffer(IntPtr.Zero); // release pin if required by impl
                if (elemSize != compSize)
                {
                    ArchLog.LogWarning($"[ChunkAccessor] ElemSize mismatch: header={compSize} actual={elemSize}");
                    // Continue anyway to avoid dropping data; or return false to enforce strictness
                }
                if (entityBase < 0 || entityBase + entityCount > totalCount)
                {
                    ArchLog.LogWarning($"[ChunkAccessor] Range OOB: base={entityBase} count={entityCount} total={totalCount}");
                    return false;
                }
            }

            // 4) Blit from src to component storage
            try
            {
                var span = new ReadOnlySpan<byte>(src, srcOffset, entityCount * compSize);
                world.CopyFrom(in chunk, typeIndex, entityBase, span, compSize);
                return true;
            }
            catch (Exception ex)
            {
                ArchLog.LogWarning($"[ChunkAccessor] CopyFrom failed: {ex.Message}");
                return false;
            }
        }
    }
}

