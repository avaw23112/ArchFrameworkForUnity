using Arch;
using Arch.Core;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Arch.Net
{
    /// <summary>
    /// Applies simple linear interpolation for types marked [Interpolate]: only float fields are interpolated.
    /// This is a minimal runtime path; future source generators can produce optimal code.
    /// </summary>
    [System]
    [Last]
    public sealed class InterpolationUpdateSystem : GlobalUpdateSystem<NetworkRuntime>
    {
        protected override void Run(Entity entity, ref NetworkRuntime runtime)
        {
            InterpolationRegistry.EnsureBuilt();

            foreach (var (key, target, elemSize, windowMs, tsMs) in InterpolationCache.Entries())
            {
                if (!NamedWorld.TryGetById(0, out var world)) continue; // only default world mapped for now
                var cfg = Arch.Net.NetworkSettings.Config;
                bool allowFallback = cfg == null || cfg.EnableInterpolationFallback;
                if (!world.TryGetArchetype(key.ArchId, out var archetype))
                {
                    if (allowFallback)
                    {
                        ReceiverEntityAllocator.EnsureRange(world, key.ArchId, key.ChunkUid, key.Base, key.Count, new[] { key.TypeId });
                        ReceiverEntityAllocator.BumpExpectedCount(world, key.ArchId, key.ChunkUid, key.Base + key.Count);
                        ReceiverEntityAllocator.TryApplyRaw(world, key.ArchId, key.ChunkUid, key.TypeId, key.Base, key.Count, target, 0, elemSize);
                    }
                    continue;
                }
                if (!archetype.TryGetTypeIndex(key.TypeId, out var typeIndex))
                {
                    if (allowFallback)
                    {
                        ReceiverEntityAllocator.EnsureRange(world, key.ArchId, key.ChunkUid, key.Base, key.Count, new[] { key.TypeId });
                        ReceiverEntityAllocator.BumpExpectedCount(world, key.ArchId, key.ChunkUid, key.Base + key.Count);
                        ReceiverEntityAllocator.TryApplyRaw(world, key.ArchId, key.ChunkUid, key.TypeId, key.Base, key.Count, target, 0, elemSize);
                    }
                    continue;
                }
                if (!world.TryGetChunkByUid(key.ArchId, key.ChunkUid, out var chunk))
                {
                    if (allowFallback)
                    {
                        ReceiverEntityAllocator.EnsureRange(world, key.ArchId, key.ChunkUid, key.Base, key.Count, new[] { key.TypeId });
                        ReceiverEntityAllocator.BumpExpectedCount(world, key.ArchId, key.ChunkUid, key.Base + key.Count);
                        ReceiverEntityAllocator.TryApplyRaw(world, key.ArchId, key.ChunkUid, key.TypeId, key.Base, key.Count, target, 0, elemSize);
                    }
                    continue;
                }
                if (!world.TryGetComponentBuffer(in chunk, typeIndex, out var ptr, out int elemSize2, out int totalCount, out int stride))
                {
                    if (allowFallback)
                    {
                        ReceiverEntityAllocator.EnsureRange(world, key.ArchId, key.ChunkUid, key.Base, key.Count, new[] { key.TypeId });
                        ReceiverEntityAllocator.BumpExpectedCount(world, key.ArchId, key.ChunkUid, key.Base + key.Count);
                        ReceiverEntityAllocator.TryApplyRaw(world, key.ArchId, key.ChunkUid, key.TypeId, key.Base, key.Count, target, 0, elemSize);
                    }
                    continue;
                }
                world.ReleaseComponentBuffer(IntPtr.Zero);

                if (!InterpolationRegistry.TryGet(key.TypeId, out var d) || d.FloatOffsets == null || d.FloatOffsets.Length == 0) continue;
                float alpha = 0f;
                if (windowMs > 0)
                {
                    float dt = Time.deltaTime * 1000f;
                    alpha = Mathf.Clamp01(dt / windowMs);
                }
                int count = key.Count;
                unsafe
                {
                    byte* basePtr = (byte*)ptr + key.Base * stride;
                    for (int i = 0; i < count; i++)
                    {
                        byte* pDst = basePtr + i * stride;
                        int srcOff = i * elemSize;
                        // For each float field offset
                        for (int fi = 0; fi < d.FloatOffsets.Length; fi++)
                        {
                            int off = d.FloatOffsets[fi];
                            if (off + 4 > elemSize || srcOff + off + 4 > target.Length) continue;
                            float curr = Unsafe.ReadUnaligned<float>(pDst + off);
                            float tgt = MemoryMarshal.Read<float>(new ReadOnlySpan<byte>(target, srcOff + off, 4));
                            float val = Mathf.Lerp(curr, tgt, alpha);
                            Unsafe.WriteUnaligned(pDst + off, val);
                        }
                    }
                }
            }
        }
    }
}
