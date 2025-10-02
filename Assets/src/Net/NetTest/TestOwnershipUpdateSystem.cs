using Arch;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.Net;
using Arch.Tools;

namespace Assets.Scripts.Test.Net
{
    /// <summary>
    /// Demonstrates NetSyncUpdateSystem branch behavior by ownership.
    /// </summary>
    [System]
    public sealed class TestOwnershipUpdateSystem : NetSyncUpdateSystem<TestPosition>
    {
        protected override void RunByOwner(Entity entity, ref TestPosition component)
        {
            // Simulate write by owner
            component.x += 0.1f;
            entity.Set(in component);
            ArchLog.LogInfo($"[Owner] Entity {entity} wrote TestPosition -> ({component.x:F2},{component.y:F2},{component.z:F2})");
        }

        protected override void RunByObserver(Entity entity, in TestPosition component)
        {
            // Observer read-only path
            ArchLog.LogInfo($"[Observer] Entity {entity} read TestPosition -> ({component.x:F2},{component.y:F2},{component.z:F2})");
        }
    }
}

