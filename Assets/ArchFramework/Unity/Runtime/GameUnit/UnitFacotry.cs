using Arch.DI;

namespace Arch.Core
{
	public static class UnitFactory
	{
		public static T Create<T>(this World world) where T : GameUnit
		{
			return (T)ArchKernel.Resolve<GameUnitManager>().AddUnit(world);
		}
	}
}