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
	[UnitySystem, Before(typeof(EntitasSystemTest_2_SystemSort_2))]
	public class EntitasSystemTest_2_SystemSort_1 : IAwake
	{
		public void Awake()
		{
			Tools.Logger.Debug("1");
		}
	}

	[Forget]
	[UnitySystem]
	public class EntitasSystemTest_2_SystemSort_2 : IAwake
	{
		public void Awake()
		{
			Tools.Logger.Debug("2");
		}
	}

	[Forget]
	[UnitySystem, Before(typeof(EntitasSystemTest_2_SystemSort_1))]
	public class EntitasSystemTest_2_SystemSort_3 : IAwake
	{
		public void Awake()
		{
			Tools.Logger.Debug("3");
		}
	}
}