using Arch.Net;

namespace Arch.Net.Application.Bootstrap.Steps
{
    public sealed class ManifestSynchronizationStep : INetworkInitializationStep
    {
        public void Initialize(Session session, ref NetworkRuntime runtime)
        {
            var typeManifestPayload = ManifestSerializer.BuildTypeManifest();
            var typeManifestPacket = PacketBuilder.BuildRpc(typeManifestPayload);
            session.Send(typeManifestPacket, typeManifestPacket.Length);

            var world = Arch.NamedWorld.DefaultWord;
            var archetypePayload = ManifestSerializer.BuildArchetypeManifestV2(world);
            var archetypePacket = PacketBuilder.BuildRpc(archetypePayload);

            foreach (var entry in ManifestSerializer.ParseArchetypeManifestV2(archetypePayload, 0, archetypePayload.Length))
            {
                ArchetypeRegistry.Register(entry.ArchId, entry.TypeIds);
            }

            session.Send(archetypePacket, archetypePacket.Length);
        }
    }
}
