using Arch.Core;
using Arch.Core.Extensions;
using Assets.Scripts.Test.Net; // TestPosition example component

namespace Arch.Net
{
    /// <summary>
    /// Helper to create common Units via code (no ScriptableObject).
    /// </summary>
    public static class GameUnits
    {
        /// <summary>
        /// Create a Player (network Unit): auto-complete network metadata and Unit, set initial position.
        /// </summary>
        public static Entity CreatePlayer(
            World world,
            float x = 0, float y = 0, float z = 0)
        {
            return UnitFactory.CreateNetworkUnit(
                world,
                configure: ent =>
                {
                    if (!ent.Has<TestPosition>()) ent.Add<TestPosition>();
                    ent.Setter((ref TestPosition p) => { p.x = x; p.y = y; p.z = z; });
                });
        }

        /// <summary>
        /// Create an NPC (optionally networked) and set initial position.
        /// </summary>
        public static Entity CreateNpc(
            World world,
            bool networked = false,
            float x = 0, float y = 0, float z = 0)
        {
            Entity e = networked
                ? UnitFactory.CreateNetworkUnit(world)
                : UnitFactory.CreateUnit(world);
            if (!e.Has<TestPosition>()) e.Add<TestPosition>();
            e.Setter((ref TestPosition p) => { p.x = x; p.y = y; p.z = z; });
            return e;
        }

        /// <summary>
        /// Promote an existing entity to Player (network Unit) and ensure common components.
        /// </summary>
        public static void PromoteToPlayer(
            ref Entity entity)
        {
            UnitFactory.EnsureAsUnit(ref entity, networked: true);
            if (!entity.Has<TestPosition>()) entity.Add<TestPosition>();
            entity.Setter((ref TestPosition p) => { p.x = 0; p.y = 0; p.z = 0; });
        }

        /// <summary>
        /// Create a Spectator (network Unit); position is optional.
        /// </summary>
        public static Entity CreateSpectator(World world)
        {
            return UnitFactory.CreateNetworkUnit(world);
        }
    }
}
