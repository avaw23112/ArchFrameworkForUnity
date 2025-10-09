using Arch.DI;
using System;
using System.Collections.Generic;

namespace Arch.Core
{
	[Service(ServiceLifetime.Singleton, typeof(GameUnitManager))]
	public class GameUnitManager
	{
		//TODO:后续考虑分批管理，避免扩容太厉害
		private Dictionary<World, Dictionary<long, GameUnit>> gameUnits = new Dictionary<World, Dictionary<long, GameUnit>>();

		public void Update(World world, Action<GameUnit> DestroyCommend)
		{
			if (gameUnits.TryGetValue(world, out Dictionary<long, GameUnit> dic))
			{
				foreach (var v in dic.Values)
				{
					if (v.isValid())
						v.OnUpdate();
					else
					{
						DestroyCommend(v);
						dic.Remove(v.Id);
					}
				}
			}
		}

		public GameUnit AddUnit(World world)
		{
			GameUnit gameUnit = new GameUnit();
			if (gameUnits == null)
			{
				gameUnits = new Dictionary<World, Dictionary<long, GameUnit>>();
			}
			if (!gameUnits.TryGetValue(world, out Dictionary<long, GameUnit> dic))
			{
				dic = new Dictionary<long, GameUnit>();
				gameUnits.Add(world, dic);
			}
			gameUnit.entity = world.Create();
			gameUnit.gameObject = new UnityEngine.GameObject();
			gameUnit.OnCreated();
			dic.Add(gameUnit.Id, gameUnit);
			return gameUnit;
		}
	}
}