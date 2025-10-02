using Arch;
using Arch.Core;
using Assets.Scripts.Test.FrameworkTest;
using Attributes;
using Events;
using UnityEngine;



[Forget]
public class ArchSystemTest_2_ReactiveSystem : Event<ArchSystemTest_1_Event>
{
	public override void Run(ArchSystemTest_1_Event value)
	{
		Debug.Log("ReactiveSystem received event: " + value);
		World world = NamedWorld.GetNamed("ReactiveSystem");
		world.Create(new TestComponent() { value = 100 });
	}
}

[Forget]
[System]
[World("ReactiveSystem")]
public class ArchSystemTest_2_ReactiveSystem_Awake : AwakeSystem<TestComponent>
{
	protected override void Run(Entity entity, ref TestComponent component_T1)
	{
		Debug.Log(component_T1.value);
	}
}
