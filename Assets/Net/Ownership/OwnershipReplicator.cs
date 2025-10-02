using System;
using System.Reflection;
using Arch.Core;
using Arch.Core.Extensions;
using Attributes;

namespace Arch.Net
{
    /// <summary>
    /// Auto-attaches NetworkOwner when any [NetworkSync] component is added to an entity.
    /// No per-entity RPC is used; Sync systems handle state replication.
    /// </summary>
    public static class OwnershipReplicator
    {
        private static bool s_initialized;
        private static bool s_applyingRemote;

        public static void Initialize()
        {
            if (s_initialized) return;

            // Subscribe to entity destroyed for id cleanup for all worlds
            foreach (var w in Arch.NamedWorld.Instance.NamedWorlds)
            {
                SubscribeEntityDestroyed(w);
            }

            // Build subscriptions for all [NetworkSync] components
            var list = new System.Collections.Generic.List<Type>();
            Collector.CollectTypes<IComponent>(list);
            foreach (var t in list)
            {
                if (t == null || !t.IsValueType) continue;
                if (t.GetCustomAttributes(typeof(NetworkSyncAttribute), false).Length == 0) continue;
                foreach (var w in Arch.NamedWorld.Instance.NamedWorlds)
                {
                    TrySubscribeAdded(w, t);
                }
            }

            s_initialized = true;
        }

        // No RPC handlers: structure changes propagate via Sync snapshots only.

        private static void SubscribeEntityDestroyed(World world)
        {
            world.SubscribeEntityDestroyed((in Entity e) =>
            {
                if (e.TryGet<NetworkEntityId>(out var nid))
                {
                    NetworkEntityRegistry.Unregister(nid.Value);
                }
            });
        }

        private static void TrySubscribeAdded(World world, Type t)
        {
            var mi = typeof(OwnershipReplicator).GetMethod(nameof(SubscribeAddedGeneric), BindingFlags.Static | BindingFlags.NonPublic);
            try { mi.MakeGenericMethod(t).Invoke(null, new object[] { world }); }
            catch { /* ignore */ }
        }

        private static void SubscribeAddedGeneric<T>(World world) where T : struct, IComponent
        {
            world.SubscribeComponentAdded<T>((in Entity entity, ref T component) =>
            {
                if (s_applyingRemote) return;
                // Ensure owner exists (auto-add). Do not send RPC.
                if (!entity.TryGet<NetworkOwner>(out var owner))
                {
                    entity.Add<NetworkOwner>();
                }
                // Ensure NetworkEntityId exists; if not, allocate now
                if (!entity.TryGet<NetworkEntityId>(out var nid))
                {
                    entity.Add<NetworkEntityId>();
                    nid = entity.Get<NetworkEntityId>();
                    nid.Value = OwnershipService.GenerateEntityId();
                    entity.Set(in nid);
                    NetworkEntityRegistry.Register(nid.Value, in entity);
                }
            });
        }
        // No RPC/binary helpers required for Sync-only approach.
    }
}
