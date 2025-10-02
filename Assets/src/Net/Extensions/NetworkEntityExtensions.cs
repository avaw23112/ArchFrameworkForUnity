using Arch.Core;
using Arch.Core.Extensions;

namespace Arch.Net
{
    /// <summary>
    /// Network 实体快捷创建与元数据（Owner/EntityId）自动补全扩展。
    /// 目标：将常见的三行样板代码合并为一行，默认使用本端 ClientId 与自动生成的 EntityId。
    /// </summary>
    public static class NetworkEntityExtensions
    {
        /// <summary>
        /// 创建一个带网络标识的实体：自动附加 NetworkOwner 与 NetworkEntityId。
        /// - ownerId 省略时使用 OwnershipService.MyClientId
        /// - id 省略/为 0 时使用 OwnershipService.GenerateEntityId()
        /// </summary>
        public static Entity CreateNetworked<T>(this World world, in T component, int ownerId = int.MinValue, ulong id = 0)
            where T : struct, IComponent
        {
            var e = world.Create(component);
            return e.EnsureNetworkMeta(ownerId, id);
        }

        /// <summary>
        /// 为已有实体补全网络元数据（若缺失则添加）。
        /// </summary>
        public static Entity EnsureNetworkMeta(this Entity entity, int ownerId = int.MinValue, ulong id = 0)
        {
            if (!entity.Has<NetworkOwner>()) entity.Add<NetworkOwner>();
            if (!entity.Has<NetworkEntityId>()) entity.Add<NetworkEntityId>();

            if (ownerId == int.MinValue) ownerId = OwnershipService.MyClientId;
            if (id == 0) id = OwnershipService.GenerateEntityId();

            entity.Setter((ref NetworkOwner n) => n.OwnerClientId = ownerId);
            entity.Setter((ref NetworkEntityId nid) => nid.Value = id);
            return entity;
        }
    }
}

