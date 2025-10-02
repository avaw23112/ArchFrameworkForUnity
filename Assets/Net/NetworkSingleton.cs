using System;
using Arch.Core;
using Arch.Tools;

namespace Arch.Net
{
    /// <summary>
    /// Lightweight static holder; AOT-safe and avoids reflection.
    /// </summary>
    public static class NetworkSingleton
    {
        public static Session Session;

        public static void EnsureInitialized(ref NetworkRuntime runtime)
        {
            if (Session != null) return;

            var session = new Session("default");
            var endpoint = runtime.Endpoint;
            if (string.IsNullOrEmpty(endpoint)) endpoint = "loopback://local";

            if (endpoint.StartsWith("lite://"))
            {
                session.AttachTransport(new LiteNetLibTransport());
            }
            else
            {
                session.AttachTransport(new MockLoopbackTransport());
            }

            session.OnConnect += () => ArchLog.LogInfo($"Connected {endpoint}");
            session.OnDisconnect += r => ArchLog.LogWarning($"Disconnected: {r}");
            session.OnReconnect += () => ArchLog.LogInfo("Reconnected");
            session.OnNetworkUnstable += h => ArchLog.LogWarning($"Network unstable: {h}");

            Session = session;
            // Initialize router with the session; endpoints are lobby-only from config.
            NetworkRouter.Initialize(session);
            // Apply route selection strategy from config
            var sel = Arch.Net.NetworkSettings.Config.RouteSelector;
            switch (sel)
            {
                case NetworkConfig.RouteSelectorType.FFIM:
                    NetworkRouter.UseSelector(new FfimSelector());
                    break;
                default:
                    NetworkRouter.UseSelector(new SimpleLatencySelector());
                    break;
            }
            // Register packet handler once to avoid per-packet allocations
            NetworkCommandQueue.RegisterPacketHandler(session.HandlePacket);
            session.Connect(endpoint);

            // Register built-in RPC handlers
            session.RegisterRpc((byte)RpcIds.TypeManifest, (buf, off, len) =>
            {
                // Parse and compare local registry
                var list = ManifestSerializer.ParseTypeManifest(buf, off, len);
                int nMismatch = 0, nMissing = 0;
                foreach (var e in list)
                {
                    var t = Type.GetType(e.Name);
                    if (t == null) { nMissing++; continue; }
                    if (ComponentRegistry.TryGet(t, out var ct))
                    {
                        if (ct.Id != e.Id) nMismatch++;
                    }
                    else nMissing++;
                }
                Arch.Tools.ArchLog.LogInfo($"[TypeManifest] entries={list.Count} mismatch={nMismatch} missing={nMissing}");
            });

            // Send local type manifest (echo server will bounce it back for verification)
            var tmPayload = ManifestSerializer.BuildTypeManifest();
            var tmPacket = PacketBuilder.BuildRpc(tmPayload);
            session.Send(tmPacket, tmPacket.Length);

            // Register ArchetypeManifest handler (v1: single-type entries)
            session.RegisterRpc((byte)RpcIds.ArchetypeManifest, (buf, off, len) =>
            {
                var list = ManifestSerializer.ParseArchetypeManifest(buf, off, len);
                Arch.Tools.ArchLog.LogInfo($"[ArchetypeManifest] entries={list.Count}");
                foreach (var e in list)
                {
                    if (e.TypeIds != null && e.TypeIds.Length == 1)
                    {
                        // Stable: single-component signature uses typeId as archId
                        uint archId = (uint)e.TypeIds[0];
                        ArchetypeRegistry.Register(archId, e.TypeIds);
                    }
                }
            });

            // Register ArchetypeManifest V2 handler (archId + ordered typeIds)
            session.RegisterRpc((byte)RpcIds.ArchetypeManifestV2, (buf, off, len) =>
            {
                var list = ManifestSerializer.ParseArchetypeManifestV2(buf, off, len);
                Arch.Tools.ArchLog.LogInfo($"[ArchetypeManifestV2] entries={list.Count}");
                foreach (var e in list)
                {
                    ArchetypeRegistry.Register(e.ArchId, e.TypeIds);
                }
            });

            // Register structural command group handler (reliable structural sync before state sync)
            session.RegisterRpc((byte)RpcIds.StructCommandGroup, (buf, off, len) =>
            {
                // Payload slice includes RpcId as first byte; enqueue as-is for apply before Sync
                var payload = new byte[len];
                System.Buffer.BlockCopy(buf, off, payload, 0, len);
                StructCommandQueue.Enqueue(payload);
            });

            // Topology RPCs
            TopologyService.Register(session);
            // Routing control RPCs
            RoutingControlService.Register(session);
            // Peer forward RPCs
            PeerForwardService.Register(session);
            // Sync relay RPCs
            SyncRelayService.Register(session);

            // Send local archetype manifest V2
            var world = Arch.NamedWorld.DefaultWord;
            var am2Payload = ManifestSerializer.BuildArchetypeManifestV2(world);
            var am2Packet = PacketBuilder.BuildRpc(am2Payload);
            // Register locally to ensure mapping is available before echo
            foreach (var e in ManifestSerializer.ParseArchetypeManifestV2(am2Payload, 0, am2Payload.Length))
            {
                ArchetypeRegistry.Register(e.ArchId, e.TypeIds);
            }
            session.Send(am2Packet, am2Packet.Length);

            // Map default world id to Default world (id=0)
            Arch.NamedWorld.MapWorldId(0, Arch.NamedWorld.DefaultWord);

            // Build applier/packer registries once (close generics via reflection at init, avoid per-packet reflection)
            ComponentApplierRegistry.EnsureBuilt();
            ComponentPackerRegistry.EnsureBuilt();
            // Enable chunk-level memcpy accessor using new Arch APIs (caller may override)
            ChunkAccess.SetAccessor(new WorldChunkAccessor());
        }
    }
}
