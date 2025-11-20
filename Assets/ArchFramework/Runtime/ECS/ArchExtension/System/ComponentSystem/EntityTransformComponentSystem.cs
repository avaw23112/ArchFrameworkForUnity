using Arch.Core;
using Arch.Tools;
using Unity.Collections;

namespace Arch
{
	[System]
	public class EntityBindingSystem : Unique.LifecycleSystem<EntityBindingComponent>
	{
		protected override void OnAwake(ref EntityBindingComponent component)
		{
			component.dicEntitiesBindings = new NativeMultiHashMap<Entity, Entity>(256, Allocator.Persistent);
			ArchLog.LogInfo("create entityBindingComponent success");
		}

		protected override void OnDestroy(ref EntityBindingComponent component)
		{
			component.dicEntitiesBindings.Dispose();
			ArchLog.LogInfo("Release entityBindingComponent success");
		}
	}

	[System]
	[After(typeof(EntityBindingSystem))]
	public class EntityAwakeSystem : GlobalAwakeSystem<EntityTransform>
	{
		protected override void Run(Entity entity, ref EntityTransform entityTransform)
		{
			if (!entityTransform.entities.IsCreated)
			{
				entityTransform.entities = new NativeList<Entity>(Allocator.Persistent);
				ArchLog.LogInfo("create EntityTransform success");
			}
			else
			{
				Tools.ArchLog.LogError($"{entity} 重复创建组件{typeof(EntityTransform)}！");
				throw new System.Exception($"{entity} 重复创建组件{typeof(EntityTransform)}！");
			}
			EntityBindingComponent sEntityBindingComponent = Unique.Component<EntityBindingComponent>.GetOrAdd();
			if (!sEntityBindingComponent.dicEntitiesBindings.IsCreated)
			{
				Tools.ArchLog.LogError($"{typeof(EntityBindingComponent)} 组件不存在或已被销毁！");
				throw new System.Exception($"{typeof(EntityBindingComponent)} 组件不存在或已被销毁！");
			}
			foreach (var vSubEntity in entityTransform.entities)
			{
				sEntityBindingComponent.dicEntitiesBindings.Add(entity, vSubEntity);
			}
		}
	}

	[System]
	[Before(typeof(EntityBindingSystem))]
	public class EntityDestroySystem : GlobalDestroySystem<EntityTransform>
	{
		protected override void Run(Entity entity, ref EntityTransform entityTransform)
		{
			if (!entityTransform.entities.IsCreated)
			{
				return;
			}
			foreach (var subEntity in entityTransform.entities)
			{
				DestroyEntityCommend(subEntity);
			}

			EntityBindingComponent sEntityBindingComponent = Unique.Component<EntityBindingComponent>.GetOrAdd();
			if (!sEntityBindingComponent.dicEntitiesBindings.IsCreated)
			{
				Tools.ArchLog.LogError($"{typeof(EntityBindingComponent)} 组件不存在或已被销毁！");
				throw new System.Exception($"{typeof(EntityBindingComponent)} 组件不存在或已被销毁！");
			}

			sEntityBindingComponent.dicEntitiesBindings.Remove(entity);

			if (entityTransform.entities.IsCreated)
			{
				entityTransform.entities.Dispose();
				ArchLog.LogInfo("Release entityTransform success");
			}
			else
			{
				Tools.ArchLog.LogError($"{entity} 重复销毁组件{typeof(EntityTransform)}！");
				throw new System.Exception($"{entity} 重复销毁组件{typeof(EntityTransform)}！");
			}
		}
	}
}