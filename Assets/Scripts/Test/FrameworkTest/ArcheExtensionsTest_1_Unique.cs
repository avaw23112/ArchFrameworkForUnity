using Arch;
using Attributes;
using Events;

namespace Assets.Scripts.Test.FrameworkTest
{
    [Forget]
    internal class ArcheExtensionsTest_1_Unique : Event<Events.ArchSystem_1_Test>
    {
        public override void Run(Events.ArchSystem_1_Test value)
        {
            EntityBindingComponent component = SingletonComponent.GetOrAdd<EntityBindingComponent>();
            Tools.Logger.Debug($"{component}");
        }
    }
}