using System;
using Arch.Net;

namespace Arch.Net.Application.Bootstrap
{
    public sealed class DefaultNetworkSessionFactory : INetworkSessionFactory
    {
        public Session Create(ref NetworkRuntime runtime)
        {
            var session = new Session("default");
            var endpoint = runtime.Endpoint;
            if (string.IsNullOrEmpty(endpoint))
            {
                endpoint = NetworkSettings.Config.DefaultEndpoint;
                runtime.Endpoint = endpoint;
            }

            var transport = TransportFactory.Create(endpoint);
            session.AttachTransport(transport);
            return session;
        }
    }
}
