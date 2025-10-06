using Arch;
using Arch.Tools;
using Attributes;

namespace Assets.Scripts.Test
{
    [Forget]
    [System]
    public class EntitasSystemTest_1_System : IPureAwake
    {
        public void Awake()
        {
            ArchLog.LogDebug("EntitasSystemTest_1_System.Execute()");
        }
    }

    [Forget]
    [System]
    public class EntitasSystemTest_2_System : IPureUpdate
    {
        public void Update()
        {
            ArchLog.LogError("EntitasSystemTest_2_System.Execute()");
        }
    }

    [Forget]
    [System]
    public class EntitasSystemTest_3_System : IPureLateUpdate
    {
        public void LateUpdate()
        {
            ArchLog.LogDebug("EntitasSystemTest_3_System.Cleanup()");
        }
    }

    [Forget]
    [System]
    public class EntitasSystemTest_4_System : IPureDestroy
    {
        public void Destroy()
        {
            ArchLog.LogDebug("EntitasSystemTest_4_System.TearDown()");
        }
    }
}