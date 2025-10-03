using System;
using Arch.Core;
using Arch.Core.Extensions;

namespace Arch.Net
{
    /// <summary>
    /// NetworkEntity 工厂（推荐使用）
    /// - 目标：集中创建/补全网络实体元数据（NetworkOwner/NetworkEntityId），并提供可插入的额外配置回调。
    /// - 好处：可控性强（统一创建点）、可扩展（后续可接入对象池、统计、日志、事件等）。
    /// </summary>
    public static class NetworkEntityFactory
    {
        /// <summary>
        /// 创建空实体，并自动补全 NetworkOwner / NetworkEntityId。可选附加自定义配置。
        /// </summary>
        public static Entity Create(World world, int? ownerId = null, ulong? entityId = null, Action<Entity> configure = null)
        {
            var e = world.Create();
            EnsureMeta(ref e, ownerId, entityId);
            configure?.Invoke(e);
            return e;
        }

        /// <summary>
        /// 创建携带单个值类型组件的网络实体，并自动补全元数据。可选附加自定义配置。
        /// </summary>
        public static Entity Create<T>(World world, in T component, int? ownerId = null, ulong? entityId = null, Action<Entity> configure = null)
            where T : struct, IComponent
        {
            var e = world.Create(component);
            EnsureMeta(ref e, ownerId, entityId);
            configure?.Invoke(e);
            return e;
        }

        /// <summary>
        /// 为已有实体补全网络元数据（若缺失则添加）。
        /// </summary>
        public static void EnsureMeta(ref Entity entity, int? ownerId = null, ulong? entityId = null)
        {
            if (!entity.Has<NetworkOwner>()) entity.Add<NetworkOwner>();
            if (!entity.Has<NetworkEntityId>()) entity.Add<NetworkEntityId>();

            int finalOwner = ownerId ?? OwnershipService.MyClientId;
            ulong finalId = entityId.HasValue && entityId.Value != 0 ? entityId.Value : OwnershipService.GenerateEntityId();

            entity.Setter((ref NetworkOwner n) => n.OwnerClientId = finalOwner);
            entity.Setter((ref NetworkEntityId nid) => nid.Value = finalId);
        }
    }
}
