using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Arch.Core;
using Attributes;
using Arch.Core.Extensions;

namespace Arch.Net
{
    /// <summary>
    /// 组件应用注册表（接收端）
    /// - 目的：缓存值类型组件的强类型“写入”委托，避免每包反射与装箱。
    /// - 构建：启动时通过反射闭包泛型一次；IL2CPP/HCLR 环境需提供 AOT 泛型桩。
    /// - 路径：优先尝试直接对 Chunk/ComponentBuffer 进行 Blit；回退到按实体遍历并 Set。
    /// </summary>
    public static class ComponentApplierRegistry
    {
        private static readonly Dictionary<int, Action<World, byte[], int, int, int>> s_map = new Dictionary<int, Action<World, byte[], int, int, int>>();
        private static bool s_built;

        public static void EnsureBuilt()
        {
            if (s_built) return;
            BuildFromComponentRegistry();
            s_built = true;
        }

        private static void BuildFromComponentRegistry()
        {
            // Build a generic applier per registered value-type component
            var pRegister = typeof(ComponentApplierRegistry).GetMethod("RegisterGeneric", BindingFlags.Static | BindingFlags.NonPublic);
            var list = new System.Collections.Generic.List<System.Type>();
            Collector.CollectTypes<IComponent>(list);
            foreach (var t in list)
            {
                if (t == null || !t.IsValueType) continue;
                if (!ComponentRegistry.TryGet(t, out var ct)) continue;
                try
                {
                    var gm = pRegister.MakeGenericMethod(t);
                    gm.Invoke(null, new object[] { ct.Id });
                }
                catch { /* ignore types that cannot be closed */ }
            }
        }

        private static void RegisterGeneric<T>(int typeId) where T : struct, IComponent
        {
            // capture closed generic into delegate
            s_map[typeId] = (world, buffer, offset, entityCount, compSize) =>
            {
                ApplyBlitGeneric<T>(world, buffer, offset, entityCount, compSize);
            };
        }

        public static bool TryApply(int typeId, World world, byte[] buffer, int offset, int entityCount, int compSize)
        {
            if (s_map.TryGetValue(typeId, out var applier))
            {
                applier(world, buffer, offset, entityCount, compSize);
                return true;
            }
            return false;
        }

        private static void ApplyBlitGeneric<T>(World targetWorld, byte[] buffer, int offset, int entityCount, int compSize)
            where T : struct, IComponent
        {
            int idx = 0;
            var q = new QueryDescription().WithAll<T>();
            targetWorld.Query(in q, (Entity e, ref T c) =>
            {
                if (idx >= entityCount) return;
                if (offset + (idx + 1) * compSize > buffer.Length) return;
                UnsafeCopyInto(ref c, buffer, offset + idx * compSize);
                e.Set(in c);
                idx++;
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void UnsafeCopyInto<T>(ref T target, byte[] src, int srcOffset) where T : struct
        {
            fixed (byte* pSrc = &src[srcOffset])
            {
                target = Unsafe.ReadUnaligned<T>(pSrc);
            }
        }
    }
}
