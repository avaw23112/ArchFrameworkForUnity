using Arch;
using Attributes;
using UnityEngine;

namespace Assets.Scripts.Test.FrameworkTest
{
	public class EntitasSystemTest_3_SystemMultiSort : MonoBehaviour
	{
	}

	//3 , 1, 2正确顺序
	[System]
	[Before(typeof(EntitasSystemTest_3_SystemSort_2))]
	[After(typeof(EntitasSystemTest_3_SystemSort_3))]
	[Forget]
	public class EntitasSystemTest_3_SystemSort_1 : IAwake
	{
		public void Awake()
		{
			Tools.Logger.Debug("1");
		}
	}

	[Forget]
	[System]
	public class EntitasSystemTest_3_SystemSort_2 : IAwake
	{
		public void Awake()
		{
			Tools.Logger.Debug("2");
		}
	}

	[Forget]
	[System]
	public class EntitasSystemTest_3_SystemSort_3 : IAwake
	{
		public void Awake()
		{
			Tools.Logger.Debug("3");
		}
	}
}