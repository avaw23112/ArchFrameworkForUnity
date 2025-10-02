using Arch;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.Net;
using Arch.Tools;

namespace Assets.Scripts.Test.Net
{
    /// <summary>
    /// Sets local client id and creates two entities with different owners to demo branch paths.
    /// </summary>
    [System]
    public sealed class TestOwnershipBootstrap : IAwake
    {
        public void Awake()
        {
            // Set local client id
            OwnershipService.MyClientId = 1;

            // Create two entities: one owned by local client, the other by another client
            var w = NamedWorld.DefaultWord;
            var eOwner = w.Create(new Assets.Scripts.Test.Net.TestPosition { x = 1, y = 1, z = 1 });
            eOwner.Add<NetworkOwner>();
            eOwner.Setter((ref NetworkOwner no) => no.OwnerClientId = 1);

            var eObserver = w.Create(new Assets.Scripts.Test.Net.TestPosition { x = 2, y = 2, z = 2 });
            eObserver.Add<NetworkOwner>();
            eObserver.Setter((ref NetworkOwner no) => no.OwnerClientId = 2);

            ArchLog.LogInfo("[TestOwnership] Bootstrap completed. MyClientId=1, created owner/observer entities.");
        }
    }
}

