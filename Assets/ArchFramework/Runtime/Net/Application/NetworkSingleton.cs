using Arch.Net.Application.Bootstrap;

namespace Arch.Net
{
    /// <summary>
    /// Lightweight static holder; AOT-safe and avoids reflection.
    /// </summary>
    public static class NetworkSingleton
    {
        private static NetworkBootstrapper s_pBootstrapper = NetworkBootstrapper.CreateDefault();

        public static Session Session { get; private set; }

        public static void EnsureInitialized(ref NetworkRuntime runtime)
        {
            if (Session != null) return;
            Session = s_pBootstrapper.Initialize(ref runtime);
        }

        public static void ConfigureBootstrapper(NetworkBootstrapper bootstrapper)
        {
            if (Session != null)
            {
                throw new System.InvalidOperationException("Cannot replace bootstrapper after initialization");
            }

            s_pBootstrapper = bootstrapper ?? throw new System.ArgumentNullException(nameof(bootstrapper));
        }
    }
}
