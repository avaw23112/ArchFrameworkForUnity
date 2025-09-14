using Arch.Core;
using System.Collections.Generic;
using Tools.Pool;
using UnityEngine;

namespace Arch
{
	[System]
	public class ViewModleSyncSysmte : DestroySystem<ViewComponent>
	{
		protected override void Run(Entity entity, ref ViewComponent component_T1)
		{
			if (component_T1.gameObject != null)
			{
				GameObject.Destroy(component_T1.gameObject);
			}
		}
	}

	#region 实体父子关系维护

	[System]
	public class EntityBindingAwakeSystem : AwakeSystem<EntityBindingComponent>
	{
		protected override void Run(Entity entity, ref EntityBindingComponent component_T1)
		{
			component_T1.dicEntitiesBinding = DictionaryPool<Entity, List<Entity>>.Get();
		}
	}

	[System]
	public class EntityBindingDestroySystem : DestroySystem<EntityBindingComponent>
	{
		protected override void Run(Entity entity, ref EntityBindingComponent component_T1)
		{
			if (component_T1.dicEntitiesBinding != null)
				DictionaryPool<Entity, List<Entity>>.Release(component_T1.dicEntitiesBinding);
		}
	}

	//怎么维护实体关系。

	//怎么将一个系统变成全局系统，即它会遍历目前所有存在的World

	#endregion
}

