using System.Collections.Generic;

namespace Arch.Net
{
    public struct RouteMetrics
    {
        public Endpoint Endpoint;
        public int RttMs;
        public float Loss;
        public float Jitter;
    }

    public interface IRouteSelector
    {
        Endpoint SelectBest(IReadOnlyList<RouteMetrics> candidates, Endpoint current);
    }

    public sealed class SimpleLatencySelector : IRouteSelector
    {
        public Endpoint SelectBest(IReadOnlyList<RouteMetrics> candidates, Endpoint current)
        {
            Endpoint best = current; int bestRtt = int.MaxValue;
            foreach (var c in candidates)
            {
                int score = c.RttMs + (int)(c.Jitter * 10) + (int)(c.Loss * 1000);
                if (score < bestRtt)
                {
                    bestRtt = score; best = c.Endpoint;
                }
            }
            return best;
        }
    }

    /// <summary>
    /// FFIM-like selector: prefers low RTT, penalizes jitter/loss, and includes endpoint weight.
    /// Uses a priority ordering analogous to a small PQ over scored candidates.
    /// This is a placeholder for a more complete flooding/minimization algorithm over a multi-hop graph.
    /// </summary>
    public sealed class FfimSelector : IRouteSelector
    {
        public Endpoint SelectBest(IReadOnlyList<RouteMetrics> candidates, Endpoint current)
        {
            Endpoint best = current;
            int bestScore = int.MaxValue;
            for (int i = 0; i < candidates.Count; i++)
            {
                var c = candidates[i];
                // Composite score: RTT + jitter penalty + loss penalty - static weight bonus
                int score = c.RttMs
                          + (int)(c.Jitter * 20)
                          + (int)(c.Loss * 2000)
                          - (c.Endpoint.Weight * 5);
                if (score < bestScore)
                {
                    bestScore = score; best = c.Endpoint;
                }
            }
            return best;
        }
    }
}
