using Arch.Net;

namespace Arch.Net.Application.Bootstrap.Steps
{
    public sealed class WorldInitializationStep : INetworkInitializationStep
    {
        public void Initialize(Session session, ref NetworkRuntime runtime)
        {
            Arch.NamedWorld.MapWorldId(0, Arch.NamedWorld.DefaultWord);
            ComponentApplierRegistry.EnsureBuilt();
            ComponentPackerRegistry.EnsureBuilt();
            ChunkAccess.SetAccessor(new WorldChunkAccessor());
        }
    }
}
