using Arch.Core;
using Arch.Core.Extensions;
using Arch.Tools;
using inEvent;
using RefEvent;
using System;

namespace Arch
{
    internal static class ArchExtensions_Entity
    {
        #region Setter And Getter

        public static void Setter<T>(this Entity entity, RefAction<T> action) where T : struct, IComponent
        {
            if (!entity.Has<T>())
            {
                ArchLog.Error($"{entity} not has the required components");
                throw new NullReferenceException($"{entity} not has the component of {typeof(T)}");
            }
            T sComponent = entity.Get<T>();
            action(ref sComponent);
            entity.Set<T>(in sComponent);
        }

        public static void Setter<T1, T2>(this Entity entity, RefAction<T1, T2> action) where T1 : struct, IComponent
            where T2 : struct, IComponent
        {
            if (!entity.Has<T1, T2>())
            {
                ArchLog.Error($"{entity} not has the required components");
                throw new NullReferenceException($"{entity} not has required components");
            }
            Components<T1, T2> sComponent = entity.Get<T1, T2>();
            action(ref sComponent.t0.Value, ref sComponent.t1.Value);
            entity.Set(in sComponent.t0.Value, in sComponent.t1.Value);
        }

        public static void Setter<T1, T2, T3>(this Entity entity, RefAction<T1, T2, T3> action) where T1 : struct, IComponent
    where T2 : struct, IComponent where T3 : struct, IComponent
        {
            if (!entity.Has<T1, T2, T3>())
            {
                ArchLog.Error($"{entity} not has the required components");
                throw new NullReferenceException($"{entity} not has the required components");
            }
            Components<T1, T2, T3> sComponent = entity.Get<T1, T2, T3>();
            action(ref sComponent.t0.Value, ref sComponent.t1.Value, ref sComponent.t2.Value);
            entity.Set(in sComponent.t0.Value, in sComponent.t1.Value, in sComponent.t2.Value);
        }

        public static void Setter<T1, T2, T3, T4>(this Entity entity, RefAction<T1, T2, T3, T4> action) where T1 : struct, IComponent
    where T2 : struct, IComponent where T3 : struct, IComponent where T4 : struct, IComponent
        {
            if (!entity.Has<T1, T2, T3, T4>())
            {
                ArchLog.Error($"{entity} not has the required components");
                throw new NullReferenceException($"{entity} not has the required components");
            }
            Components<T1, T2, T3, T4> sComponent = entity.Get<T1, T2, T3, T4>();
            action(ref sComponent.t0.Value, ref sComponent.t1.Value, ref sComponent.t2.Value, ref sComponent.t3.Value);
            entity.Set(in sComponent.t0.Value, in sComponent.t1.Value, in sComponent.t2.Value, in sComponent.t3.Value);
        }

        // 5参数版本
        public static void Setter<T1, T2, T3, T4, T5>(this Entity entity, RefAction<T1, T2, T3, T4, T5> action)
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
        {
            if (!entity.Has<T1, T2, T3, T4, T5>())
            {
                ArchLog.Error($"{entity} not has the required components");
                throw new NullReferenceException($"{entity} not has the required components");
            }

            Components<T1, T2, T3, T4, T5> sComponent = entity.Get<T1, T2, T3, T4, T5>();
            action(ref sComponent.t0.Value, ref sComponent.t1.Value, ref sComponent.t2.Value,
                   ref sComponent.t3.Value, ref sComponent.t4.Value);
            entity.Set(in sComponent.t0.Value, in sComponent.t1.Value, in sComponent.t2.Value,
                      in sComponent.t3.Value, in sComponent.t4.Value);
        }

        // 6参数版本
        public static void Setter<T1, T2, T3, T4, T5, T6>(this Entity entity, RefAction<T1, T2, T3, T4, T5, T6> action)
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
        {
            if (!entity.Has<T1, T2, T3, T4, T5, T6>())
            {
                ArchLog.Error($"{entity} not has the required components");
                throw new NullReferenceException($"{entity} not has the required components");
            }

            Components<T1, T2, T3, T4, T5, T6> sComponent = entity.Get<T1, T2, T3, T4, T5, T6>();
            action(ref sComponent.t0.Value, ref sComponent.t1.Value, ref sComponent.t2.Value,
                   ref sComponent.t3.Value, ref sComponent.t4.Value, ref sComponent.t5.Value);
            entity.Set(in sComponent.t0.Value, in sComponent.t1.Value, in sComponent.t2.Value,
                      in sComponent.t3.Value, in sComponent.t4.Value, in sComponent.t5.Value);
        }

        public static void Getter<T>(this Entity entity, InAction<T> action) where T : struct, IComponent
        {
            if (!entity.Has<T>())
            {
                ArchLog.Error($"{entity} not has the required components");
                throw new NullReferenceException($"{entity} not has the component of {typeof(T)}");
            }
            T sComponent = entity.Get<T>();
            action(in sComponent);
        }

        public static void Getter<T1, T2>(this Entity entity, InAction<T1, T2> action) where T1 : struct, IComponent where T2 : struct, IComponent
        {
            if (!entity.Has<T1, T2>())
            {
                ArchLog.Error($"{entity} not has the component of {typeof(T1)},{typeof(T2)}");
                throw new NullReferenceException($"{entity} not has the component of {typeof(T1)},{typeof(T2)}");
            }
            Components<T1, T2> sComponent = entity.Get<T1, T2>();
            action(in sComponent.t0.Value, in sComponent.t1.Value);
        }

        // 3参数版本
        public static void Getter<T1, T2, T3>(this Entity entity, InAction<T1, T2, T3> action)
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
        {
            if (!entity.Has<T1, T2, T3>())
            {
                ArchLog.Error($"{entity} not has the component of {typeof(T1)}, {typeof(T2)}, {typeof(T3)}");
                throw new NullReferenceException($"{entity} not has the component of {typeof(T1)}, {typeof(T2)}, {typeof(T3)}");
            }
            Components<T1, T2, T3> sComponent = entity.Get<T1, T2, T3>();
            action(in sComponent.t0.Value, in sComponent.t1.Value, in sComponent.t2.Value);
        }

        // 4参数版本
        public static void Getter<T1, T2, T3, T4>(this Entity entity, InAction<T1, T2, T3, T4> action)
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
        {
            if (!entity.Has<T1, T2, T3, T4>())
            {
                ArchLog.Error($"{entity} not has the component of {typeof(T1)}, {typeof(T2)}, {typeof(T3)}, {typeof(T4)}");
                throw new NullReferenceException($"{entity} not has the component of {typeof(T1)}, {typeof(T2)}, {typeof(T3)}, {typeof(T4)}");
            }
            Components<T1, T2, T3, T4> sComponent = entity.Get<T1, T2, T3, T4>();
            action(in sComponent.t0.Value, in sComponent.t1.Value, in sComponent.t2.Value, in sComponent.t3.Value);
        }

        // 5参数版本
        public static void Getter<T1, T2, T3, T4, T5>(this Entity entity, InAction<T1, T2, T3, T4, T5> action)
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
        {
            if (!entity.Has<T1, T2, T3, T4, T5>())
            {
                ArchLog.Error($"{entity} not has the component of {typeof(T1)}, {typeof(T2)}, {typeof(T3)}, {typeof(T4)}, {typeof(T5)}");
                throw new NullReferenceException($"{entity} not has the component of {typeof(T1)}, {typeof(T2)}, {typeof(T3)}, {typeof(T4)}, {typeof(T5)}");
            }
            Components<T1, T2, T3, T4, T5> sComponent = entity.Get<T1, T2, T3, T4, T5>();
            action(in sComponent.t0.Value, in sComponent.t1.Value, in sComponent.t2.Value, in sComponent.t3.Value, in sComponent.t4.Value);
        }

        public static void Getter<T1, T2, T3, T4, T5, T6>(this Entity entity, InAction<T1, T2, T3, T4, T5, T6> action)
            where T1 : struct, IComponent
            where T2 : struct, IComponent
            where T3 : struct, IComponent
            where T4 : struct, IComponent
            where T5 : struct, IComponent
            where T6 : struct, IComponent
        {
            if (!entity.Has<T1, T2, T3, T4, T5, T6>())
            {
                ArchLog.Error($"{entity} not has the component of {typeof(T1)}, {typeof(T2)}, {typeof(T3)}, {typeof(T4)}, {typeof(T5)}, {typeof(T6)}");
                throw new NullReferenceException($"{entity} not has the component of {typeof(T1)}, {typeof(T2)}, {typeof(T3)}, {typeof(T4)}, {typeof(T5)}, {typeof(T6)}");
            }
            Components<T1, T2, T3, T4, T5, T6> sComponent = entity.Get<T1, T2, T3, T4, T5, T6>();
            action(in sComponent.t0.Value, in sComponent.t1.Value, in sComponent.t2.Value, in sComponent.t3.Value, in sComponent.t4.Value, in sComponent.t5.Value);
        }

        #endregion Setter And Getter

        public static bool isVaild(this Entity entity)
        {
            return entity != default(Entity);
        }

        public static T GetOrAdd<T>(this Entity entity) where T : struct, IComponent
        {
            T component;
            if (!entity.TryGet<T>(out component))
            {
                entity.Add<T>();
                component = entity.Get<T>();
            }
            return component;
        }

        /// <summary>
        /// 如果已经存在组件返回失败，否则添加组件并返回成功
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="entity"></param>
        /// <returns></returns>
        public static bool TryAdd<T>(this Entity entity)
        {
            if (!entity.Has<T>())
            {
                entity.Add<T>();
                return true;
            }
            return false;
        }

        #region 父子实体管理

        public static void SetParent(this Entity entity, Entity parent)
        {
            if (!entity.Has<EntityTransform>())
            {
                entity.Add<EntityTransform>();
            }
            if (!parent.Has<EntityTransform>())
            {
                parent.Add<EntityTransform>();
            }
            entity.Setter((ref EntityTransform entityTransform) =>
            {
                entityTransform.parent = parent;
            });
            parent.Setter(((ref EntityTransform entityTransform) =>
            {
                entityTransform.entities.Add(entity);
            }));
        }

        public static Entity GetParent(this Entity entity)
        {
            if (!entity.TryGet<EntityTransform>(out EntityTransform component))
            {
                Tools.ArchLog.Error($"{entity} 没有挂载父子管理组件");
                return default(Entity);
            }
            if (component.parent.isVaild())
            {
                Tools.ArchLog.Error($"{entity}的父实体不是有效实体");
                return default(Entity);
            }
            return component.parent;
        }

        #endregion 父子实体管理
    }
}