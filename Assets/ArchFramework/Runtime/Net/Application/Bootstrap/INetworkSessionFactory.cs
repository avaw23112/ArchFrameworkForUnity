using Arch.Net;

namespace Arch.Net.Application.Bootstrap
{
    public interface INetworkSessionFactory
    {
        Session Create(ref NetworkRuntime runtime);
    }
}
