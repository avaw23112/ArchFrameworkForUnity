using System;
using Arch.Net;
using Arch.Tools;

namespace Arch.Net.Application.Bootstrap.Steps
{
    public sealed class RpcRegistrationStep : INetworkInitializationStep
    {
        public void Initialize(Session session, ref NetworkRuntime runtime)
        {
            session.RegisterRpc((byte)RpcIds.TypeManifest, (buffer, offset, length) =>
            {
                var list = ManifestSerializer.ParseTypeManifest(buffer, offset, length);
                int mismatch = 0;
                int missing = 0;
                foreach (var entry in list)
                {
                    var type = Type.GetType(entry.Name);
                    if (type == null)
                    {
                        missing++;
                        continue;
                    }

                    if (ComponentRegistry.TryGet(type, out var componentType))
                    {
                        if (componentType.Id != entry.Id) mismatch++;
                    }
                    else
                    {
                        missing++;
                    }
                }
                ArchLog.LogInfo($"[TypeManifest] entries={list.Count} mismatch={mismatch} missing={missing}");
            });

            session.RegisterRpc((byte)RpcIds.ArchetypeManifest, (buffer, offset, length) =>
            {
                var list = ManifestSerializer.ParseArchetypeManifest(buffer, offset, length);
                ArchLog.LogInfo($"[ArchetypeManifest] entries={list.Count}");
                foreach (var entry in list)
                {
                    if (entry.TypeIds != null && entry.TypeIds.Length == 1)
                    {
                        uint archId = (uint)entry.TypeIds[0];
                        ArchetypeRegistry.Register(archId, entry.TypeIds);
                    }
                }
            });

            session.RegisterRpc((byte)RpcIds.ArchetypeManifestV2, (buffer, offset, length) =>
            {
                var list = ManifestSerializer.ParseArchetypeManifestV2(buffer, offset, length);
                ArchLog.LogInfo($"[ArchetypeManifestV2] entries={list.Count}");
                foreach (var entry in list)
                {
                    ArchetypeRegistry.Register(entry.ArchId, entry.TypeIds);
                }
            });

            session.RegisterRpc((byte)RpcIds.StructCommandGroup, (buffer, offset, length) =>
            {
                var payload = new byte[length];
                Buffer.BlockCopy(buffer, offset, payload, 0, length);
                StructCommandQueue.Enqueue(payload);
            });
        }
    }
}
