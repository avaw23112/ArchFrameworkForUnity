using Arch;
using Arch.Core;
using Attributes;

namespace Arch.Net
{
    /// <summary>
    /// Ensures ownership replicator is initialized when networking starts.
    /// </summary>
    [System]
    public sealed class OwnershipInitSystem : GlobalAwakeSystem<NetworkRuntime>
    {
        protected override void Run(Entity entity, ref NetworkRuntime runtime)
        {
            // Ensure session exists, then initialize replicator
            NetworkSingleton.EnsureInitialized(ref runtime);
            OwnershipReplicator.Initialize();
        }
    }
}

