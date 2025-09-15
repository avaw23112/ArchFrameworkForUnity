using Arch;
using Arch.Tools;
using Attributes;

namespace Assets.Scripts.Test
{
    [Forget]
    [System]
    public class EntitasSystemTest_1_System : IAwake
    {
        public void Awake()
        {
            ArchLog.Debug("EntitasSystemTest_1_System.Execute()");
        }
    }

    [Forget]
    [System]
    public class EntitasSystemTest_2_System : IUpdate
    {
        public void Update()
        {
            ArchLog.Error("EntitasSystemTest_2_System.Execute()");
        }
    }

    [Forget]
    [System]
    public class EntitasSystemTest_3_System : ILateUpdate
    {
        public void LateUpdate()
        {
            ArchLog.Debug("EntitasSystemTest_3_System.Cleanup()");
        }
    }

    [Forget]
    [System]
    public class EntitasSystemTest_4_System : IDestroy
    {
        public void Destroy()
        {
            ArchLog.Debug("EntitasSystemTest_4_System.TearDown()");
        }
    }
}