using Arch.Core.Extensions;
using System;
using UnityEngine;

namespace Arch.Core
{
	public struct Unit : IComponent
	{
	}

	public class GameUnit
	{
		public long Id
		{
			get
			{
				if (isValid())
					return entity.Id ^ gameObject.GetInstanceID();
				else return -1;
			}
		}

		public Entity entity;
		public GameObject gameObject;

		public virtual void OnCreated()
		{
			entity.Add<Unit>();
		}

		public virtual void OnUpdate()
		{
		}

		public bool isValid()
		{
			return entity.isVaild() && gameObject;
		}

		public override bool Equals(object obj)
		{
			if (obj is GameUnit otherUnit)
			{
				return Id == otherUnit.Id;
			}
			return base.Equals(obj);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Id, entity, gameObject);
		}
	}
}