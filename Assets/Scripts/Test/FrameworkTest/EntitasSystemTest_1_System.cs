using Arch;
using Attributes;
using Tools;

namespace Assets.Scripts.Test
{
	[Forget]
	[UnitySystem]
	public class EntitasSystemTest_1_System : IAwake
	{
		public void Awake()
		{
			Logger.Debug("EntitasSystemTest_1_System.Execute()");
		}
	}

	[Forget]
	[UnitySystem]
	public class EntitasSystemTest_2_System : IUpdate
	{
		public void Update()
		{
			Logger.Debug("EntitasSystemTest_2_System.Execute()");
		}
	}

	[Forget]
	[UnitySystem]
	public class EntitasSystemTest_3_System : ILateUpdate
	{
		public void LateUpdate()
		{
			Logger.Debug("EntitasSystemTest_3_System.Cleanup()");
		}
	}

	[Forget]
	[UnitySystem]
	public class EntitasSystemTest_4_System : IDestroy
	{
		public void Destroy()
		{
			Logger.Debug("EntitasSystemTest_4_System.TearDown()");
		}
	}
}