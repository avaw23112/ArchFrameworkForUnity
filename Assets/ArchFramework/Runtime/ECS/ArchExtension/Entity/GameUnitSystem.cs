using Arch.DI;
using UnityEngine;

namespace Arch.Core
{
	public class GameUnitSystem : GlobalUpdateSystem<Unit>
	{
		public void DestoryUnit(GameUnit gameUnit)
		{
			DestroyEntityCommend(gameUnit.entity);
			GameObject.Destroy(gameUnit.gameObject);
		}

		protected override void Run(Entity entity, ref Unit component_T1)
		{
			ArchKernel.Resolve<GameUnitManager>().Update(world, DestoryUnit);
		}
	}
}