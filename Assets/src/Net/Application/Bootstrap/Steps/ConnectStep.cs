using Arch.Net;

namespace Arch.Net.Application.Bootstrap.Steps
{
    public sealed class ConnectStep : INetworkInitializationStep
    {
        public void Initialize(Session session, ref NetworkRuntime runtime)
        {
            var endpoint = runtime.Endpoint;
            if (string.IsNullOrEmpty(endpoint))
            {
                endpoint = NetworkSettings.Config.DefaultEndpoint;
                runtime.Endpoint = endpoint;
            }

            session.Connect(endpoint);
        }
    }
}
