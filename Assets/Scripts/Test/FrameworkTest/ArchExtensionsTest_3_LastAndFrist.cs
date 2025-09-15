using Arch;
using Arch.Core;
using Attributes;
using Events;
using Tools;

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

    [Forget]
    [System]
    public class TestFristComponentAwakeSystem : AwakeSystem<TestOrderComponent>
    {
        protected override void Run(Entity entity, ref TestOrderComponent component_T1)
        {
            Logger.Debug($"1");
        }
    }

    [System]
    [Last]
    public class TestFristComponent_1_AwakeSystem : AwakeSystem<TestOrderComponent>
    {
        protected override void Run(Entity entity, ref TestOrderComponent component_T1)
        {
            Logger.Debug($"2");
        }
    }

    [System]
    [First]
    public class TestFristComponent_2_AwakeSystem : AwakeSystem<TestOrderComponent>
    {
        protected override void Run(Entity entity, ref TestOrderComponent component_T1)
        {
            Logger.Debug($"3");
        }
    }
}