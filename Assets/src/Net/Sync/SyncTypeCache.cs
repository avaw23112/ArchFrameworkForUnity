using System;
using System.Collections.Generic;
using System.Reflection;
using Arch.Core;
using Attributes;

namespace Arch.Net
{
    /// <summary>
    /// Sync type cache
    /// - Caches value-type components marked with [NetworkSync] and related metadata.
    /// - Stores: Type, typeId, archId, HasSyncDelta, and a strong-typed send delegate (BuildAndSend).
    /// - Built on first use; call Rebuild() if the component set changes.
    /// </summary>
    internal static class SyncTypeCache
    {
        internal sealed class Entry
        {
            public Type Type;
            public int TypeId;
            public uint ArchId;
            public bool HasSyncDelta;
            public Action<World, uint, int, int> BuildAndSend; // points to SyncScanSystem.BuildAndSendGeneric<T>
        }

        private static volatile bool s_built;
        private static readonly List<Entry> s_entries = new List<Entry>(32);

        /// <summary>
        /// Returns all cached sync type entries.
        /// </summary>
        public static IReadOnlyList<Entry> GetAll()
        {
            EnsureBuilt();
            return s_entries;
        }

        /// <summary>
        /// Force rebuild of the cache (normally not needed).
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
                if (t.GetCustomAttributes(typeof(NetworkSyncAttribute), false).Length == 0) continue;
                if (!ComponentRegistry.TryGet(t, out var ct)) continue;

                uint archId = ArchetypeRegistry.TryGetArchIdForSingleType(ct.Id, out var tmp) ? tmp : (uint)ct.Id;
                bool hasDelta = t.GetCustomAttributes(typeof(SyncDeltaAttribute), false).Length > 0;

                Action<World, uint, int, int> buildAndSend = null;
                try
                {
                    var mi = typeof(SyncScanSystem)
                        .GetMethod("BuildAndSendGeneric", BindingFlags.Static | BindingFlags.NonPublic)
                        ?.MakeGenericMethod(t);
                    if (mi != null)
                    {
                        buildAndSend = (Action<World, uint, int, int>)Delegate.CreateDelegate(
                            typeof(Action<World, uint, int, int>), mi);
                    }
                }
                catch
                {
                    // ignore binding errors; entry will remain without a sender delegate
                }

                s_entries.Add(new Entry
                {
                    Type = t,
                    TypeId = ct.Id,
                    ArchId = archId,
                    HasSyncDelta = hasDelta,
                    BuildAndSend = buildAndSend,
                });
            }
        }
    }
}

