using Arch.Core;
using System.Collections.Generic;
using Tools.Pool;

namespace Arch
{
    [System]
    public class EntityBindingAwakeSystem : IAwake
    {
        public void Awake()
        {
            SingletonComponent.Setter<EntityBindingComponent>((ref EntityBindingComponent component) =>
            {
                component.dicEntitiesBindings = DictionaryPool<Entity, List<Entity>>.Get();
            });
        }
    }

    [System]
    public class EntityBindingDestroySystem : IDestroy
    {
        public void Destroy()
        {
            SingletonComponent.Getter<EntityBindingComponent>((in EntityBindingComponent component) =>
            {
                DictionaryPool<Entity, List<Entity>>.Release(component.dicEntitiesBindings);
            });
        }
    }

    [System]
    [After(typeof(EntityBindingAwakeSystem))]
    public class EntityAwakeSystem : GlobalAwakeSystem<EntityTransform>
    {
        protected override void Run(Entity entity, ref EntityTransform entityTransform)
        {
            if (entityTransform.entities == null)
                entityTransform.entities = ListPool<Entity>.Get();
            else
            {
                Tools.Logger.Error($"{entity} 重复创建组件{typeof(EntityTransform)}！");
                throw new System.Exception($"{entity} 重复创建组件{typeof(EntityTransform)}！");
            }
            EntityBindingComponent sEntityBindingComponent = SingletonComponent.GetOrAdd<EntityBindingComponent>();
            if (sEntityBindingComponent.dicEntitiesBindings == null)
            {
                Tools.Logger.Error($"{typeof(EntityBindingComponent)} 组件不存在或已被销毁！");
                throw new System.Exception($"{typeof(EntityBindingComponent)} 组件不存在或已被销毁！");
            }
            sEntityBindingComponent.dicEntitiesBindings.Add(entity, entityTransform.entities);
        }
    }

    [System]
    [Before(typeof(EntityBindingDestroySystem))]
    public class EntityDestroySystem : GlobalDestroySystem<EntityTransform>
    {
        protected override void Run(Entity entity, ref EntityTransform entityTransform)
        {
            if (entityTransform.entities == null)
            {
                return;
            }
            foreach (var subEntity in entityTransform.entities)
            {
                DestroyEntity(subEntity);
            }

            EntityBindingComponent sEntityBindingComponent = SingletonComponent.GetOrAdd<EntityBindingComponent>();
            if (sEntityBindingComponent.dicEntitiesBindings == null)
            {
                Tools.Logger.Error($"{typeof(EntityBindingComponent)} 组件不存在或已被销毁！");
                throw new System.Exception($"{typeof(EntityBindingComponent)} 组件不存在或已被销毁！");
            }

            sEntityBindingComponent.dicEntitiesBindings.Remove(entity);

            if (entityTransform.entities != null)
            {
                ListPool<Entity>.Release(entityTransform.entities);
                entityTransform.entities = null;
            }
            else
            {
                Tools.Logger.Error($"{entity} 重复销毁组件{typeof(EntityTransform)}！");
                throw new System.Exception($"{entity} 重复销毁组件{typeof(EntityTransform)}！");
            }
        }
    }
}