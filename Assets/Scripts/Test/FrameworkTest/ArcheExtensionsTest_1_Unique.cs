using Arch;
using Attributes;
using Events;


namespace Assets.Scripts.Test.FrameworkTest
{
	[Forget]
	internal class ArcheExtensionsTest_1_Unique : Event<ArchSystem_Test>
	{
		public override void Run(ArchSystem_Test value)
		{
			EntityBindingComponent component = SingletonComponent.GetOrAdd<EntityBindingComponent>();
			Tools.Logger.Debug($"{component}");
		}
	}
}
