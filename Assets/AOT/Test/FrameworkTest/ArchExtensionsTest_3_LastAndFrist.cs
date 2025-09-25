using Arch;
using Arch.Core;
using Arch.Tools;
using Attributes;
using Events;

namespace Assets.Scripts.Test.FrameworkTest
{
	internal class ArchExtensionsTest_3_LastAndFrist : Event<ArchSystem_1_Test>
	{
		public override void Run(ArchSystem_1_Test value)
		{
			Entity entity = NamedWorld.DefaultWord.Create<TestOrderComponent>();
		}
	}

	public struct TestOrderComponent : IComponent
	{
		public int id;
	}

	[System]
	[Forget]
	public class TestFristComponentAwakeSystem : AwakeSystem<TestOrderComponent>
	{
		protected override void Run(Entity entity, ref TestOrderComponent component_T1)
		{
			ArchLog.LogDebug($"1");
		}
	}

	[System]
	[Forget]
	[Last]
	public class TestFristComponent_1_AwakeSystem : AwakeSystem<TestOrderComponent>
	{
		protected override void Run(Entity entity, ref TestOrderComponent component_T1)
		{
			ArchLog.LogDebug($"2");
		}
	}

	[System]
	[First]
	[Forget]
	public class TestFristComponent_2_AwakeSystem : AwakeSystem<TestOrderComponent>
	{
		protected override void Run(Entity entity, ref TestOrderComponent component_T1)
		{
			ArchLog.LogDebug($"3");
		}
	}
}