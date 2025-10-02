using Arch;
using Arch.Core;
using Arch.Core.Extensions;

namespace Arch.Net
{
    [System]
    public abstract class NetSyncLateUpdateSystem : GlobalLateUpdateSystem
    {
        protected override void Run(Entity entity)
        {
            if (entity.TryGet<NetworkOwner>(out var owner) && OwnershipService.IsOwner(owner.OwnerClientId))
                RunByOwner(entity);
            else
                RunByObserver(entity);
        }
        protected abstract void RunByOwner(Entity entity);
        protected abstract void RunByObserver(Entity entity);
    }
    // 1
    [System]
    public abstract class NetSyncLateUpdateSystem<T> : GlobalLateUpdateSystem<T> where T : struct, IComponent
    {
        protected override void Run(Entity entity, ref T component_T1)
        {
            if (entity.TryGet<NetworkOwner>(out var owner) && OwnershipService.IsOwner(owner.OwnerClientId))
                RunByOwner(entity, ref component_T1);
            else
                RunByObserver(entity, in component_T1);
        }
        protected abstract void RunByOwner(Entity entity, ref T component);
        protected abstract void RunByObserver(Entity entity, in T component);
    }

    // 2
    [System]
    public abstract class NetSyncLateUpdateSystem<T1, T2> : GlobalLateUpdateSystem<T1, T2>
        where T1 : struct, IComponent where T2 : struct, IComponent
    {
        protected override void Run(Entity entity, ref T1 c1, ref T2 c2)
        {
            if (entity.TryGet<NetworkOwner>(out var owner) && OwnershipService.IsOwner(owner.OwnerClientId))
                RunByOwner(entity, ref c1, ref c2);
            else
                RunByObserver(entity, in c1, in c2);
        }
        protected abstract void RunByOwner(Entity entity, ref T1 c1, ref T2 c2);
        protected abstract void RunByObserver(Entity entity, in T1 c1, in T2 c2);
    }

    // 3
    [System]
    public abstract class NetSyncLateUpdateSystem<T1, T2, T3> : GlobalLateUpdateSystem<T1, T2, T3>
        where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent
    {
        protected override void Run(Entity entity, ref T1 c1, ref T2 c2, ref T3 c3)
        {
            if (entity.TryGet<NetworkOwner>(out var owner) && OwnershipService.IsOwner(owner.OwnerClientId))
                RunByOwner(entity, ref c1, ref c2, ref c3);
            else
                RunByObserver(entity, in c1, in c2, in c3);
        }
        protected abstract void RunByOwner(Entity entity, ref T1 c1, ref T2 c2, ref T3 c3);
        protected abstract void RunByObserver(Entity entity, in T1 c1, in T2 c2, in T3 c3);
    }

    // 4
    [System]
    public abstract class NetSyncLateUpdateSystem<T1, T2, T3, T4> : GlobalLateUpdateSystem<T1, T2, T3, T4>
        where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent where T4 : struct, IComponent
    {
        protected override void Run(Entity entity, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4)
        {
            if (entity.TryGet<NetworkOwner>(out var owner) && OwnershipService.IsOwner(owner.OwnerClientId))
                RunByOwner(entity, ref c1, ref c2, ref c3, ref c4);
            else
                RunByObserver(entity, in c1, in c2, in c3, in c4);
        }
        protected abstract void RunByOwner(Entity entity, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4);
        protected abstract void RunByObserver(Entity entity, in T1 c1, in T2 c2, in T3 c3, in T4 c4);
    }

    // 5
    [System]
    public abstract class NetSyncLateUpdateSystem<T1, T2, T3, T4, T5> : GlobalLateUpdateSystem<T1, T2, T3, T4, T5>
        where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent where T4 : struct, IComponent where T5 : struct, IComponent
    {
        protected override void Run(Entity entity, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5)
        {
            if (entity.TryGet<NetworkOwner>(out var owner) && OwnershipService.IsOwner(owner.OwnerClientId))
                RunByOwner(entity, ref c1, ref c2, ref c3, ref c4, ref c5);
            else
                RunByObserver(entity, in c1, in c2, in c3, in c4, in c5);
        }
        protected abstract void RunByOwner(Entity entity, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5);
        protected abstract void RunByObserver(Entity entity, in T1 c1, in T2 c2, in T3 c3, in T4 c4, in T5 c5);
    }

    // 6
    [System]
    public abstract class NetSyncLateUpdateSystem<T1, T2, T3, T4, T5, T6> : GlobalLateUpdateSystem<T1, T2, T3, T4, T5, T6>
        where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent where T4 : struct, IComponent where T5 : struct, IComponent where T6 : struct, IComponent
    {
        protected override void Run(Entity entity, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6)
        {
            if (entity.TryGet<NetworkOwner>(out var owner) && OwnershipService.IsOwner(owner.OwnerClientId))
                RunByOwner(entity, ref c1, ref c2, ref c3, ref c4, ref c5, ref c6);
            else
                RunByObserver(entity, in c1, in c2, in c3, in c4, in c5, in c6);
        }
        protected abstract void RunByOwner(Entity entity, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6);
        protected abstract void RunByObserver(Entity entity, in T1 c1, in T2 c2, in T3 c3, in T4 c4, in T5 c5, in T6 c6);
    }
}

