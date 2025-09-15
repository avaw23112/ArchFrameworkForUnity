using Arch;
using Arch.Core;
using Events;
using Tools;

namespace Assets.Scripts.Test.FrameworkTest
{
    internal class ArchExtensionsTest_2_ParentTransform : Event<ArchSystem_Test>
    {
        public override void Run(ArchSystem_Test value)
        {
            Entity entityParent = NamedWorld.Instance.DefaultWord.Create(new TestParentComponent { id = 0 });
            Entity subEntity = NamedWorld.Instance.DefaultWord.Create(new TestParentComponent { id = 1 });
            subEntity.SetParent(entityParent);
            NamedWorld.Instance.DefaultWord.Destroy(entityParent);
        }
    }

    public struct TestParentComponent : IComponent
    {
        public int id;
    }

    [System]
    public class TestParentAwakeSystem : AwakeSystem<TestParentComponent>
    {
        protected override void Run(Entity entity, ref TestParentComponent component_T1)
        {
            Logger.Debug($"Awake : {entity} is awake,ID :{component_T1.id}");
        }
    }

    [System]
    public class TestParentDestroySystem : DestroySystem<TestParentComponent>
    {
        protected override void Run(Entity entity, ref TestParentComponent component_T1)
        {
            Logger.Debug($"Destroy : {entity} is awake,ID :{component_T1.id}");
        }
    }
}