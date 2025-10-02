using Arch;
using Arch.Core;
using Attributes;

namespace Arch.Net
{
    /// <summary>
    /// Flushes structural command groups once per frame before Sync scanners run.
    /// </summary>
    [System]
    [First]
    public sealed class StructCommandFlushSystem : GlobalUpdateSystem<NetworkRuntime>
    {
        private int m_lastFlushFrame;
        protected override void Run(Entity entity, ref NetworkRuntime runtime)
        {
            // Ensure aggregator subscribed and send group for default world (id=0)
            StructCommandAggregator.EnsureSubscribed();
            int frame = (int)UnityEngine.Time.frameCount;
            var cfg = Arch.Net.NetworkSettings.Config;
            int interval = cfg?.StructCommandFlushIntervalFrames > 0 ? cfg.StructCommandFlushIntervalFrames : 1;
            if (frame - m_lastFlushFrame >= interval)
            {
                StructCommandAggregator.Flush(0);
                m_lastFlushFrame = frame;
            }
        }
    }
}
