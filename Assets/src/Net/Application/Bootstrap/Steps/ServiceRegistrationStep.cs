using Arch.Net;

namespace Arch.Net.Application.Bootstrap.Steps
{
    public sealed class ServiceRegistrationStep : INetworkInitializationStep
    {
        public void Initialize(Session session, ref NetworkRuntime runtime)
        {
            TopologyService.Register(session);
            RoutingControlService.Register(session);
            PeerForwardService.Register(session);
            SyncRelayService.Register(session);
        }
    }
}
