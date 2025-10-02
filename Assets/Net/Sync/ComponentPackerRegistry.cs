using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Attributes;

namespace Arch.Net
{
    /// <summary>
    /// 组件打包注册表（发送端）
    /// - 目的：将值类型 IComponent 的内存布局直接拷贝为连续字节，以便构造同步（Sync）数据段。
    /// - 做法：启动时通过反射一次性闭包泛型，缓存 typeId -> 强类型打包委托，运行期避免反射与装箱。
    /// - GC 优化：打包过程采用“先计数、后一次性分配”策略，避免 List<T> 与中间临时数组分配。
    ///
    /// 注意：该注册表仅打包本端拥有（NetworkOwner 且 OwnershipService.IsOwner）的实体组件。
    /// </summary>
    public static class ComponentPackerRegistry
    {
        // typeId -> (world,maxEntities) => (payload,count,elemSize)
        private static readonly Dictionary<int, Func<World, int, (byte[] payload, int count, int compSize)>> s_map = new Dictionary<int, Func<World, int, (byte[], int, int)>>();
        private static bool s_built;

        /// <summary>
        /// 确保已构建缓存（仅构建一次）。
        /// </summary>
        public static void EnsureBuilt()
        {
            if (s_built) return;
            BuildFromComponentRegistry();
            s_built = true;
        }

        /// <summary>
        /// 根据组件注册表收集值类型组件，闭包泛型并注册打包委托（仅一次）。
        /// </summary>
        private static void BuildFromComponentRegistry()
        {
            var list = new List<Type>();
            Collector.CollectTypes<IComponent>(list);
            foreach (var t in list)
            {
                if (t == null || !t.IsValueType) continue;
                if (!ComponentRegistry.TryGet(t, out var ct)) continue;
                try
                {
                    var gm = typeof(ComponentPackerRegistry)
                        .GetMethod("RegisterGeneric", BindingFlags.Static | BindingFlags.NonPublic)
                        .MakeGenericMethod(t);
                    gm.Invoke(null, new object[] { ct.Id });
                }
                catch { /* 忽略无法闭包的类型（AOT 需确保有桩）*/ }
            }
        }

        /// <summary>
        /// 注册单个强类型 T 的打包委托。
        /// </summary>
        private static void RegisterGeneric<T>(int typeId) where T : struct, IComponent
        {
            s_map[typeId] = (world, maxEntities) => PackGeneric<T>(world, maxEntities);
        }

        /// <summary>
        /// 按 typeId 查找打包器并执行。
        /// </summary>
        public static bool TryPack(int typeId, World world, int maxEntities, out byte[] payload, out int count, out int compSize)
        {
            payload = null; count = 0; compSize = 0;
            if (s_map.TryGetValue(typeId, out var fn))
            {
                var res = fn(world, maxEntities);
                payload = res.payload; count = res.count; compSize = res.compSize;
                return count > 0 && payload != null;
            }
            return false;
        }

        /// <summary>
        /// 通用强类型打包：
        /// 1) 首次遍历仅计数（最多 maxEntities）；
        /// 2) 一次性分配精确大小的字节数组；
        /// 3) 二次遍历进行无对齐写入，避免 List 与中间缓冲带来的 GC。
        /// </summary>
        private static (byte[] payload, int count, int compSize) PackGeneric<T>(World world, int maxEntities)
            where T : struct, IComponent
        {
            int size = Unsafe.SizeOf<T>();
            if (size <= 0) return (null, 0, 0);

            int n = 0;
            var q = new QueryDescription().WithAll<T>();
            // 首次遍历：计数（仅统计本端拥有的实体）
            world.Query(in q, (Entity e, ref T c) =>
            {
                if (n >= maxEntities) return;
                if (e.TryGet<NetworkOwner>(out var owner) && OwnershipService.IsOwner(owner.OwnerClientId))
                {
                    n++;
                }
            });

            if (n <= 0) return (null, 0, size);

            var buf = new byte[n * size];
            int i = 0;
            // 二次遍历：顺序写入到目标缓冲
            world.Query(in q, (Entity e, ref T c) =>
            {
                if (i >= n) return;
                if (e.TryGet<NetworkOwner>(out var owner) && OwnershipService.IsOwner(owner.OwnerClientId))
                {
                    UnsafeWrite(buf, i * size, c);
                    i++;
                }
            });

            return (buf, n, size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void UnsafeWrite<T>(byte[] dst, int offset, in T value) where T : struct
        {
            fixed (byte* pDst = &dst[offset])
            {
                Unsafe.WriteUnaligned(pDst, value);
            }
        }
    }
}
