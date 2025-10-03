using System;
using Arch.Core;
using Arch.Core.Extensions;

namespace Arch.Net
{
    /// <summary>
    /// Unit 工厂：参�?ET �?Unit 设计，提供统一�?Unit 创建入口与网络单元创建入口�?    /// - 可�?Hook：允许上层注入全局初始化逻辑（如挂默认组件、绑定表现对象等）�?    /// </summary>
    public static class UnitFactory
    {
        public static Action<Entity> GlobalInitHook;

        /// <summary>
        /// 创建本地 Unit（非强制网络），支持配置 Id/名称/额外配置�?        /// </summary>
        public static Entity CreateUnit(World world, Action<Entity> configure = null)
        {
            var e = world.Create();
            // UnitId 本地生成（与网络 id 一致的 64 位生成策略），非网络也可使用
            var unitId = OwnershipService.GenerateEntityId();
            e.Add<Unit>();
            e.Setter((ref Unit u) => { u.UnitId = unitId; });
            
            configure?.Invoke(e);
            GlobalInitHook?.Invoke(e);
            return e;
        }

        /// <summary>
        /// 创建网络 Unit：自动补�?NetworkOwner/NetworkEntityId，并对齐 Unit.UnitId�?        /// </summary>
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

        /// <summary>
        /// 从已有实体升级为 Unit（并可选升级为网络 Unit）�?        /// </summary>
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


