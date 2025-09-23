using Arch;
using Arch.Core;
using Arch.Core.Extensions;
using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Test.FrameworkTest
{
	public class ArchSystemTest_1_ReactiveSystem : MonoBehaviour
	{

		// Use this for initialization
		void Start()
		{
			World world = World.Create();
			world.Create(new TestComponent { value = 10 });
			ArchSystemTest_1_ReactiveSystem_Awake archSystemTest_1_ReactiveSystem_Awake = new ArchSystemTest_1_ReactiveSystem_Awake();
			archSystemTest_1_ReactiveSystem_Awake.BuildIn(world);
			archSystemTest_1_ReactiveSystem_Awake.SubcribeEntityAwake();
		}

		// Update is called once per frame
		void Update()
		{

		}
	}
	public struct TestComponent : IComponent
	{
		public int value;
	}

	public class ArchSystemTest_1_ReactiveSystem_Awake : AwakeSystem<TestComponent>
	{
		protected override void Run(Entity entity, ref TestComponent component_T1)
		{
			Debug.Log(entity.Get<TestComponent>().value);
			Debug.Log("ArchSystemTest_1_ReactiveSystem_Awake");
			component_T1.value = 100;
			Debug.Log(entity.Get<TestComponent>().value);
		}
	}
}