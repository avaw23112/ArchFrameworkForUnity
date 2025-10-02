using Arch.Core;

namespace Arch.Net
{
    /// <summary>
    /// Static entry to chunk accessor. Default implementation falls back to per-entity blit via ComponentApplierRegistry.
    /// External modules can set a custom accessor that performs true chunk-level memcpy.
    /// </summary>
    public static class ChunkAccess
    {
        private static IChunkAccessor s_pAccessor = new DefaultFallbackAccessor();

        /// <summary>
        /// Set custom accessor (e.g., Arch.LowLevel integration) to enable true chunk-level writes.
        /// </summary>
        public static void SetAccessor(IChunkAccessor accessor)
        {
            if (accessor != null) s_pAccessor = accessor;
        }

        /// <summary>
        /// Try blit via accessor; if accessor declines, return false.
        /// </summary>
        public static bool TryBlitComponentRange(
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
            return s_pAccessor?.TryBlitComponentRange(world, archetypeId, chunkId, typeId, entityBase, entityCount, src, srcOffset, compSize) == true;
        }

        private sealed class DefaultFallbackAccessor : IChunkAccessor
        {
            public bool TryBlitComponentRange(World world, uint archetypeId, uint chunkId, int typeId, int entityBase, int entityCount, byte[] src, int srcOffset, int compSize)
            {
                // Fallback: use cached per-entity applier (ignores chunk hints)
                if (entityCount <= 0 || compSize <= 0) return true; // nothing to do
                ComponentApplierRegistry.TryApply(typeId, world, src, srcOffset, entityCount, compSize);
                return true;
            }
        }
    }
}

