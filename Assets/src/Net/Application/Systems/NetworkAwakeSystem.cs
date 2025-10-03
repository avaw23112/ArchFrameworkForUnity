using Arch;
using Arch.Core;
using Arch.Tools;

namespace Arch.Net
{
    /// <summary>
    /// Initialize network session when NetworkRuntime component is added.
    /// </summary>
    [System]
    public sealed class NetworkAwakeSystem : GlobalAwakeSystem<NetworkRuntime>
    {
        /// <summary>
        /// Ensure the NetworkSingleton session is initialized and bound.
        /// </summary>
        protected override void Run(Entity entity, ref NetworkRuntime runtime)
        {
            NetworkSingleton.EnsureInitialized(ref runtime);
        }
    }
}
