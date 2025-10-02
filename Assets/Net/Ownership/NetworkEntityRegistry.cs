using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using UnityEngine;

namespace Arch.Net
{
    /// <summary>
    /// Maps network entity ids to ECS Entities.
    /// </summary>
    public static class NetworkEntityRegistry
    {
        private struct Entry { public Entity Entity; public int LastSeenFrame; }
        private static readonly Dictionary<ulong, Entry> s_entities = new Dictionary<ulong, Entry>();

        public static bool TryGet(ulong id, out Entity entity)
        {
            if (s_entities.TryGetValue(id, out var e)) { entity = e.Entity; return true; }
            entity = default; return false;
        }

        public static void Register(ulong id, in Entity entity)
        {
            s_entities[id] = new Entry { Entity = entity, LastSeenFrame = Time.frameCount };
        }

        public static void Unregister(ulong id)
        {
            s_entities.Remove(id);
        }

        public static void MarkSeen(ulong id)
        {
            if (s_entities.TryGetValue(id, out var e))
            {
                e.LastSeenFrame = Time.frameCount;
                s_entities[id] = e;
            }
        }

        /// <summary>
        /// Destroy entities whose ids have not been seen for maxAgeFrames.
        /// Only culls entities that still have NetworkEntityId component.
        /// </summary>
        public static void CullStale(World world, int maxAgeFrames)
        {
            if (world == null) return;
            int now = Time.frameCount;
            var toRemove = new List<ulong>();
            foreach (var kv in s_entities)
            {
                int age = now - kv.Value.LastSeenFrame;
                if (age < 0) age = 0;
                if (age > maxAgeFrames)
                {
                    var e = kv.Value.Entity;
                    try
                    {
                        if (world.IsAlive(e) && e.isVaild() && e.Has<NetworkEntityId>())
                        {
                            world.Destroy(e);
                            toRemove.Add(kv.Key);
                        }
                    }
                    catch { /* ignore */ }
                }
            }
            foreach (var id in toRemove) s_entities.Remove(id);
        }
    }
}
