using System;
using System.Collections.Generic;
using System.Reflection;
using Arch.Core;
using Attributes;
using static Arch.Net.SyncScanSystem;

namespace Arch.Net
{
    /// <summary>
    /// 同步类型缓存
    /// - 目的：缓存带有 [NetworkSync] 的值类型组件及其元数据，避免每帧反射与类型遍历。
    /// - 内容：Type、typeId、archId、是否带 [SyncDelta]、强类型采集委托（用于按拥有权收集 OwnedBatch）。
    /// - 生命周期：首次访问时构建，必要时可显式 Rebuild（通常不需要）。
    /// </summary>
    internal static class SyncTypeCache
    {
        internal sealed class Entry
        {
            public Type Type;
            public int TypeId;
            public uint ArchId;
            public bool HasSyncDelta;
            public Func<World, int, OwnedBatch> CollectOwned; // 指向 SyncScanSystem.CollectOwned<T>
            public Action<World, uint, int, int> BuildAndSend; // 指向 SyncScanSystem.BuildAndSendGeneric<T>
        }

        private static volatile bool s_built;
        private static readonly List<Entry> s_entries = new List<Entry>(32);

        /// <summary>
        /// 返回缓存的全部同步类型条目。
        /// </summary>
        public static IReadOnlyList<Entry> GetAll()
        {
            EnsureBuilt();
            return s_entries;
        }

        /// <summary>
        /// 强制重建缓存（一般无需调用）。
        /// </summary>
        public static void Rebuild()
        {
            s_entries.Clear();
            s_built = false;
            EnsureBuilt();
        }

        private static void EnsureBuilt()
        {
            if (s_built) return;
            Build();
            s_built = true;
        }

        private static void Build()
        {
            var types = new List<Type>();
            Collector.CollectTypes<IComponent>(types);

            foreach (var t in types)
            {
                if (t == null || !t.IsValueType) continue;
                // 仅收集带 [NetworkSync] 的值类型组件
                if (t.GetCustomAttributes(typeof(NetworkSyncAttribute), false).Length == 0) continue;
                if (!ComponentRegistry.TryGet(t, out var ct)) continue;

                uint archId = ArchetypeRegistry.TryGetArchIdForSingleType(ct.Id, out var tmp) ? tmp : (uint)ct.Id;
                bool hasDelta = t.GetCustomAttributes(typeof(SyncDeltaAttribute), false).Length > 0;

                // 绑定 CollectOwned<T> 为强类型委托，避免每帧反射调用。
                Func<World, int, OwnedBatch> collector = null;
                Action<World, uint, int, int> buildAndSend = null;
                try
                {
                    var mi = typeof(SyncScanSystem)
                        .GetMethod("CollectOwned", BindingFlags.Static | BindingFlags.NonPublic)
                        ?.MakeGenericMethod(t);
                    if (mi != null)
                    {
                        collector = (Func<World, int, OwnedBatch>)Delegate.CreateDelegate(typeof(Func<World, int, OwnedBatch>), mi);
                    }
                    var mi2 = typeof(SyncScanSystem)
                        .GetMethod("BuildAndSendGeneric", BindingFlags.Static | BindingFlags.NonPublic)
                        ?.MakeGenericMethod(t);
                    if (mi2 != null)
                    {
                        buildAndSend = (Action<World, uint, int, int>)Delegate.CreateDelegate(typeof(Action<World, uint, int, int>), mi2);
                    }
                }
                catch { /* AOT 环境需确保泛型桩存在 */ }

                s_entries.Add(new Entry
                {
                    Type = t,
                    TypeId = ct.Id,
                    ArchId = archId,
                    HasSyncDelta = hasDelta,
                    CollectOwned = collector,
                    BuildAndSend = buildAndSend,
                });
            }
        }
    }
}
