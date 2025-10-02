using Arch.Core;

namespace Arch.Net
{
    /// <summary>
    /// Abstraction for chunk-level component blit. Implementations can directly write bytes
    /// into chunk component storage using ChunkId/EntityBase/Count addressing.
    /// </summary>
    public interface IChunkAccessor
    {
        /// <summary>
        /// Try to blit a contiguous range of component data into storage.
        /// Returns true if handled by chunk path; false to let caller fall back to entity iteration.
        /// </summary>
        bool TryBlitComponentRange(
            World world,
            uint archetypeId,
            uint chunkId,
            int typeId,
            int entityBase,
            int entityCount,
            byte[] src,
            int srcOffset,
            int compSize);
    }
}

