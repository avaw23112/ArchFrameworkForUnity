using Arch;
using Arch.Core;
using Arch.Tools;

namespace Arch.Net
{
    /// <summary>
    /// Pump transport and drain command queue each frame; limited to entities with NetworkRuntime.
    /// Marked [Last] to run after other update systems.
    /// </summary>
    [System]
    [Last]
    public sealed class NetworkUpdateSystem : GlobalUpdateSystem<NetworkRuntime>
    {
        private const int CommandsPerFrame = 256;

        /// <summary>
        /// Pump transport and drain command/packet queues according to NetworkSettings.
        /// </summary>
        protected override void Run(Entity entity, ref NetworkRuntime runtime)
        {
            // Ensure session is ready even if component was added before subscriptions
            NetworkSingleton.EnsureInitialized(ref runtime);
            var s = NetworkSingleton.Session;
            s?.Update();
            NetworkCommandQueue.Drain(Arch.Net.NetworkSettings.Config.CommandsPerFrame, Arch.Net.NetworkSettings.Config.PacketsPerFrame);
            // Router tick
            NetworkRouter.Tick();
        }

    }
}

