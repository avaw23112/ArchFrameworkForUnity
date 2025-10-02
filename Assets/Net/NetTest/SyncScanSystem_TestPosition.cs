using Arch;
using Arch.Core;
using Assets.Scripts.Test.Net;

namespace Arch.Net
{
    /// <summary>
    /// Minimal Sync scanner for demo component Assets.Scripts.Test.Net.TestPosition.
    /// Sends one Sync packet per entity per frame.
    /// </summary>
    [System]
    public sealed class SyncScanSystem_TestPosition : GlobalUpdateSystem<TestPosition>
    {
        protected override void Run(Entity entity, ref Assets.Scripts.Test.Net.TestPosition component_T1)
        {
            var pSession = NetworkSingleton.Session;
            if (pSession == null) return;

            // Build minimal payload: 3 floats (x,y,z)
            var p = new byte[12];
            var fx = System.BitConverter.GetBytes(component_T1.x);
            var fy = System.BitConverter.GetBytes(component_T1.y);
            var fz = System.BitConverter.GetBytes(component_T1.z);
            System.Buffer.BlockCopy(fx, 0, p, 0, 4);
            System.Buffer.BlockCopy(fy, 0, p, 4, 4);
            System.Buffer.BlockCopy(fz, 0, p, 8, 4);

            // Use default locator (0) for now; downstream logs will show receipt
            // ArchetypeId=1 for single-component TestPosition signature registered by manifest
            var packet = PacketBuilder.BuildSync(0, 1, 0, 0, 1, p);
            pSession.Send(packet, packet.Length);
        }
    }
}
