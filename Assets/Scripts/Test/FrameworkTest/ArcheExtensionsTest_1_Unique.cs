using Arch;
using Events;


namespace Assets.Scripts.Test.FrameworkTest
{
	internal class ArcheExtensionsTest_1_Unique : Event<ArchSystem_Unique_Test>
	{
		public override void Run(ArchSystem_Unique_Test value)
		{
			EntityBindingComponent component = SingletonComponent.GetSingle<EntityBindingComponent>();
			Tools.Logger.Debug($"{component}");
		}
	}
}
