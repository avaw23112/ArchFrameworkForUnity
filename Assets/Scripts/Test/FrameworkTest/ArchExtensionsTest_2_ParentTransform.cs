using Arch;
using Arch.Core;
using Arch.Tools;
using Attributes;
using Events;

namespace Assets.Scripts.Test.FrameworkTest
{
    internal class ArchExtensionsTest_2_ParentTransform : Event<Events.ArchSystem_1_Test>
    {
        public override void Run(Events.ArchSystem_1_Test value)
        {
            Entity entityParent = NamedWorld.DefaultWord.Create(new TestParentComponent { id = 0 });
            Entity subEntity = NamedWorld.DefaultWord.Create(new TestParentComponent { id = 1 });
            subEntity.SetParent(entityParent);
            NamedWorld.DefaultWord.Destroy(entityParent);
        }
    }

    public struct TestParentComponent : IComponent
    {
        public int id;
    }

    [System, Forget]
    public class TestParentAwakeSystem : AwakeSystem<TestParentComponent>
    {
        protected override void Run(Entity entity, ref TestParentComponent component_T1)
        {
            ArchLog.Debug($"Awake : {entity} is awake,ID :{component_T1.id}");
        }
    }

    [System, Forget]
    public class TestParentDestroySystem : DestroySystem<TestParentComponent>
    {
        protected override void Run(Entity entity, ref TestParentComponent component_T1)
        {
            ArchLog.Debug($"Destroy : {entity} is awake,ID :{component_T1.id}");
        }
    }
}