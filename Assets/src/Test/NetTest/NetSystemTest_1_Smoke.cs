using Arch;
using Arch.Core;
using Arch.Tools;
using Attributes;

namespace Assets.Scripts.Test.Net
{
    /// <summary>
    /// Network system smoke test: logs endpoint on awake.
    /// </summary>
    [System]
    public sealed class NetSystemTest_1_Smoke_Awake : GlobalAwakeSystem<global::Arch.Net.NetworkRuntime>
    {
        protected override void Run(Entity entity, ref global::Arch.Net.NetworkRuntime runtime)
        {
            ArchLog.LogInfo($"[NetSystemTest_1_Smoke_Awake] Endpoint: {runtime.Endpoint}");
        }
    }
}

