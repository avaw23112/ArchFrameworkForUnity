using System.Collections.Generic;

namespace Arch.Net
{
    /// <summary>
    /// Static holder for session router.
    /// </summary>
    public static class NetworkRouter
    {
        public static SessionRouter Instance { get; private set; }
        public static void Initialize(ISession session) { if (Instance == null && session != null) Instance = new SessionRouter(session); }
        public static void ConfigureEndpoints(IEnumerable<string> urls) { Instance?.SetEndpoints(urls); }
        public static void UseSelector(IRouteSelector selector) { Instance?.SetSelector(selector); }
        public static void AddPeer(string url, int weight = 0) { Instance?.AddPeer(url, weight); }
        public static void RemovePeer(string url) { Instance?.RemovePeer(url); }
        public static void SetPeerWeight(string url, int weight) { Instance?.SetPeerWeight(url, weight); }
        public static string CurrentEndpoint => Instance?.CurrentEndpoint;
        public static void ForceConnect(string url) { Instance?.ForceConnect(url); }
        public static void Tick() { Instance?.Tick(); }
    }
}
