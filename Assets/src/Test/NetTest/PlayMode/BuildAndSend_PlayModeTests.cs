using System.Collections;
using Arch.Core.Extensions;
using UnityEngine.Assertions;

namespace Arch.Net.Tests.PlayMode
{
    public class BuildAndSend_PlayModeTests
    {
        public IEnumerator BuildAndSend_SendsSyncPacket_Loopback()
        {
            // Arrange config
            OwnershipService.MyClientId = 1;
            var cfg = NetworkSettings.Config;
            // disable chunk scan and relay
            typeof(NetworkConfig).GetField("m_vUseChunkScan", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(cfg, false);
            typeof(NetworkConfig).GetField("m_vEnableSyncRelay", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?.SetValue(cfg, false);

            // init session with loopback transport
            var rt = new NetworkRuntime { Endpoint = "loopback://local" };
            NetworkSingleton.EnsureInitialized(ref rt);
            var session = NetworkSingleton.Session;

            // capture received packets
            bool gotSync = false;
            byte[] lastPacket = null;
            session.Transport.DataReceived += (pkt) => { lastPacket = pkt; gotSync = true; };

            // create a test entity with components
            var w = NamedWorld.DefaultWord;
            var e = w.Create(new Assets.Scripts.Test.Net.TestPosition { x = 1, y = 2, z = 3 });
            e.Add<NetworkOwner>();
            e.Setter((ref NetworkOwner no) => no.OwnerClientId = 1);
            e.Add<NetworkEntityId>();
            e.Setter((ref NetworkEntityId nid) => nid.Value = OwnershipService.GenerateEntityId());

            // BuildAndSend via cache delegate
            int netIdType = ComponentRegistryExtensions.GetTypeId(typeof(NetworkEntityId));
            var entries = SyncTypeCache.GetAll();
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry.Type == typeof(Assets.Scripts.Test.Net.TestPosition) && entry.BuildAndSend != null)
                {
                    entry.BuildAndSend(w, entry.ArchId, netIdType, 64);
                }
            }

            // allow transport poll
            session.Update();
            yield return null;

            // Assert we received a Sync packet
            Assert.IsTrue(gotSync, "No packet received on loopback transport.");
            Assert.IsNotNull(lastPacket, "Packet buffer null.");
            int headerLen;
            var header = PacketHeader.ReadFrom(lastPacket, out headerLen);
            Assert.AreEqual(PacketType.Sync, header.Type, "Expected Sync packet type.");
        }
    }
}

