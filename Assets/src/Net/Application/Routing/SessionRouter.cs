using System;
using System.Collections.Generic;

namespace Arch.Net
{
    /// <summary>
    /// Manages endpoints, tracks metrics and can migrate session to a better route.
    /// </summary>
    public sealed class SessionRouter
    {
        private readonly ISession m_pSession;
        private readonly List<Endpoint> m_pEndpoints = new List<Endpoint>();
        private readonly Dictionary<string, RouteMetrics> m_pMetrics = new Dictionary<string, RouteMetrics>();
        private IRouteSelector m_pSelector;
        private string m_szCurrent;

        public SessionRouter(ISession session, IRouteSelector selector = null)
        {
            m_pSession = session ?? throw new ArgumentNullException(nameof(session));
            m_pSelector = selector ?? new SimpleLatencySelector();

            // subscribe latency updates if transport exposes it
            if (m_pSession.Transport is ITransport t)
            {
                t.LatencyUpdated += OnLatency;
                m_szCurrent = t.Endpoint;
            }
        }

        public void SetSelector(IRouteSelector selector)
        {
            if (selector != null) m_pSelector = selector;
        }

        public void SetEndpoints(IEnumerable<string> urls)
        {
            m_pEndpoints.Clear();
            foreach (var u in urls) m_pEndpoints.Add(new Endpoint { Url = u, Weight = 0 });
        }

        public void AddPeer(string url, int weight = 0)
        {
            if (string.IsNullOrEmpty(url)) return;
            for (int i = 0; i < m_pEndpoints.Count; i++)
            {
                if (m_pEndpoints[i].Url == url)
                {
                    var e = m_pEndpoints[i]; e.Weight = weight; m_pEndpoints[i] = e; return;
                }
            }
            m_pEndpoints.Add(new Endpoint { Url = url, Weight = weight });
        }

        public void RemovePeer(string url)
        {
            if (string.IsNullOrEmpty(url)) return;
            for (int i = 0; i < m_pEndpoints.Count; i++)
            {
                if (m_pEndpoints[i].Url == url)
                {
                    m_pEndpoints.RemoveAt(i);
                    break;
                }
            }
            // If we removed the current endpoint, force reevaluation next tick
            if (m_szCurrent == url) m_szCurrent = null;
        }

        public void SetPeerWeight(string url, int weight)
        {
            for (int i = 0; i < m_pEndpoints.Count; i++)
            {
                if (m_pEndpoints[i].Url == url)
                {
                    var e = m_pEndpoints[i]; e.Weight = weight; m_pEndpoints[i] = e; return;
                }
            }
        }

        public string CurrentEndpoint => m_szCurrent;

        public void ForceConnect(string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint)) return;
            if (m_szCurrent == endpoint) return;
            Migrate(endpoint);
        }

        private void OnLatency(int rtt)
        {
            if (m_pSession.Transport == null) return;
            UpdateMetric(m_pSession.Transport.Endpoint, rtt);
        }

        private void UpdateMetric(string url, int rtt)
        {
            if (!m_pMetrics.TryGetValue(url, out var m))
            {
                m = new RouteMetrics { Endpoint = new Endpoint { Url = url }, RttMs = rtt };
            }
            else
            {
                m.RttMs = rtt;
            }
            m_pMetrics[url] = m;
        }

        public void Tick()
        {
            // build candidates
            var list = new List<RouteMetrics>(m_pEndpoints.Count);
            foreach (var e in m_pEndpoints)
            {
                if (m_pMetrics.TryGetValue(e.Url, out var m)) list.Add(m);
                else list.Add(new RouteMetrics { Endpoint = e, RttMs = int.MaxValue });
            }
            var current = new Endpoint { Url = m_szCurrent };
            var best = m_pSelector.SelectBest(list, current);
            if (best.Url != m_szCurrent && best.Url != null)
            {
                Migrate(best.Url);
            }
        }

        private void Migrate(string endpoint)
        {
            try
            {
                var newTransport = TransportFactory.Create(endpoint);
                newTransport.Configure(endpoint);
                // Attach first so we don't miss early Connected event
                m_pSession.AttachTransport(newTransport);
                // Use session to drive connect to ensure hooks fire consistently
                m_pSession.Connect(endpoint);
                m_szCurrent = endpoint;
            }
            catch (Exception ex)
            {
                Arch.Tools.ArchLog.LogWarning($"[Router] Migrate failed: {ex.Message}");
            }
        }
    }
}
