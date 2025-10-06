using Arch;
using Attributes;
using UnityEngine;

namespace Assets.Scripts.Test.FrameworkTest
{
    public class EntitasSystemTest_2_SystemSort : MonoBehaviour
    {
        private void Start()
        {
        }

        private void Update()
        {
        }
    }

    //3 , 1, 2正确顺序
    [Forget]
    [System, Before(typeof(EntitasSystemTest_2_SystemSort_2))]
    public class EntitasSystemTest_2_SystemSort_1 : IPureAwake
    {
        public void Awake()
        {
            Arch.Tools.ArchLog.LogDebug("1");
        }
    }

    [Forget]
    [System]
    public class EntitasSystemTest_2_SystemSort_2 : IPureAwake
    {
        public void Awake()
        {
            Arch.Tools.ArchLog.LogDebug("2");
        }
    }

    [Forget]
    [System, Before(typeof(EntitasSystemTest_2_SystemSort_1))]
    public class EntitasSystemTest_2_SystemSort_3 : IPureAwake
    {
        public void Awake()
        {
            Arch.Tools.ArchLog.LogDebug("3");
        }
    }
}