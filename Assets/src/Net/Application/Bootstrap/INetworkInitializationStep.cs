using Arch.Net;

namespace Arch.Net.Application.Bootstrap
{
    public interface INetworkInitializationStep
    {
        void Initialize(Session session, ref NetworkRuntime runtime);
    }
}
