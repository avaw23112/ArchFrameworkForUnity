using Arch;
using Arch.Core;

namespace Arch.Net
{
    /// <summary>
    /// Periodically computes MST from TopologyGraph and nudges router toward the chosen parent.
    /// </summary>
    [System]
    public sealed class TopologyUpdateSystem : GlobalUpdateSystem<NetworkRuntime>
    {
        // Defaults; can be overridden by NetworkConfig
        private const int DefaultUpdateIntervalFrames = 60;            // ~1s @60fps
        private const int DefaultMinMigrationIntervalFrames = 300;      // ~5s
        private const int DefaultImprovementThreshold = 50;             // score units
        private const int DefaultRequiredConsecutiveBetter = 2;

        private int m_lastFrame;
        private int m_lastMigrationFrame;
        private int m_betterCount;
        private string m_lastCandidateEndpoint;

        protected override void Run(Entity entity, ref NetworkRuntime runtime)
        {
            var cfg = Arch.Net.NetworkSettings.Config;
            if (cfg != null && !cfg.TopologyEnabled) return;
            // Load config (with sane defaults)
            int updateInterval = cfg?.TopologyUpdateIntervalFrames > 0 ? cfg.TopologyUpdateIntervalFrames : DefaultUpdateIntervalFrames;
            int minMigrationInterval = cfg?.TopologyMinMigrationIntervalFrames > 0 ? cfg.TopologyMinMigrationIntervalFrames : DefaultMinMigrationIntervalFrames;
            int improvementThreshold = cfg?.TopologyImprovementThreshold > 0 ? cfg.TopologyImprovementThreshold : DefaultImprovementThreshold;
            int requiredConsecutive = cfg?.TopologyRequiredConsecutiveBetter > 0 ? cfg.TopologyRequiredConsecutiveBetter : DefaultRequiredConsecutiveBetter;

            // throttle
            int frame = (int)(UnityEngine.Time.frameCount);
            if (frame - m_lastFrame < updateInterval) return;
            m_lastFrame = frame;

            string myPeerId = OwnershipService.MyClientId.ToString();
            // Ensure local peer exists in graph with current endpoint
            var sess = NetworkSingleton.Session;
            var currEndpoint = sess?.Transport?.Endpoint;
            if (!string.IsNullOrEmpty(currEndpoint))
                TopologyGraph.RegisterPeer(myPeerId, currEndpoint, 0);

            // Choose best neighbor as preferred parent candidate
            if (!TopologyGraph.TryGetBestNeighbor(myPeerId, out var candidatePeer, out var candidateScore)) return;
            if (!TopologyGraph.TryGetEndpoint(candidatePeer, out var candidateEndpoint) || string.IsNullOrEmpty(candidateEndpoint)) return;

            // Resolve current endpoint -> peer & score
            string currentEndpoint = NetworkRouter.CurrentEndpoint ?? currEndpoint;
            TopologyGraph.TryGetPeerIdByEndpoint(currentEndpoint, out var currentPeer);
            int currentScore;
            if (string.IsNullOrEmpty(currentPeer) || !TopologyGraph.TryGetEdgeScore(myPeerId, currentPeer, out currentScore))
            {
                currentScore = int.MaxValue / 2;
            }

            // Hysteresis & rate limiting
            if (currentEndpoint == candidateEndpoint)
            {
                m_betterCount = 0;
                m_lastCandidateEndpoint = candidateEndpoint;
                return;
            }

            if (currentScore - candidateScore >= improvementThreshold)
            {
                if (m_lastCandidateEndpoint == candidateEndpoint) m_betterCount++; else { m_betterCount = 1; m_lastCandidateEndpoint = candidateEndpoint; }
                if (m_betterCount >= requiredConsecutive && (frame - m_lastMigrationFrame) >= minMigrationInterval)
                {
                    NetworkRouter.ForceConnect(candidateEndpoint);
                    m_lastMigrationFrame = frame;
                    m_betterCount = 0;
                }
            }
            else
            {
                m_betterCount = 0;
                m_lastCandidateEndpoint = candidateEndpoint;
            }
        }
    }
}
