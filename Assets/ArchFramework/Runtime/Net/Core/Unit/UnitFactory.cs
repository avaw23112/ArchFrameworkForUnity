using Arch.Core;
using Arch.Core.Extensions;
using System;

namespace Arch.Net
{
	public static class UnitFactory
	{
		public static Action<Entity> GlobalInitHook;

		public static Entity CreateUnit(World world, Action<Entity> configure = null)
		{
			var e = world.Create();

			var unitId = OwnershipService.GenerateEntityId();
			e.Add<Unit>();
			e.Setter((ref Unit u) => { u.UnitId = unitId; });

			configure?.Invoke(e);
			GlobalInitHook?.Invoke(e);
			return e;
		}


		public static Entity CreateNetworkUnit(World world, Action<Entity> configure = null)
		{
			var e = NetworkEntityFactory.Create(world, null, null, null);
			var netId = e.Get<NetworkEntityId>().Value;
			e.Add<Unit>();
			e.Setter((ref Unit u) => { u.UnitId = netId; });

			configure?.Invoke(e);
			GlobalInitHook?.Invoke(e);
			return e;
		}


		public static void EnsureAsUnit(ref Entity entity, bool networked = false)
		{
			if (networked)
			{
				NetworkEntityFactory.EnsureMeta(ref entity, null, null);
			}
			if (!entity.Has<Unit>()) entity.Add<Unit>();
			var id = networked ? entity.Get<NetworkEntityId>().Value : OwnershipService.GenerateEntityId();
			entity.Setter((ref Unit u) => { u.UnitId = id; });

			GlobalInitHook?.Invoke(entity);
		}
	}
}


