using Arch.Core;
using Arch.Core.Extensions;

namespace Arch
{
	/// <summary>
	/// 初始版本，外界可完全掌控查询条件和过滤条件
	/// </summary>
	public abstract class GlobalDestroySystem : GlobalReactiveSystem, IReactiveDestroy
	{
		public void SubcribeEntityDestroy()
		{
			foreach (World worldNamed in NamedWorld.Instance.NamedWorlds)
			{
				worldNamed.SubscribeEntityDestroyed((in Entity entity) =>
				{
					world = worldNamed;
					if (GetTrigger(entity))
						Run(entity);
				});
			}
		}
	}

	/// <summary>
	/// 当带有T组件的实体创建时，触发系统的运行
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class GlobalDestroySystem<T> : GlobalReactiveSystem<T>, IReactiveDestroy where T : struct, IComponent
	{
		public void SubcribeEntityDestroy()
		{
			foreach (World WorldNamed in NamedWorld.Instance.NamedWorlds)
			{
				world = WorldNamed;
				WorldNamed.SubscribeComponentRemoved((in Entity entity, ref T component) =>
				{
					if (GetTrigger(entity))
						Run(entity, ref component);

				});
			}
		}
	}
	public abstract class GlobalDestroySystem<T1, T2> : GlobalReactiveSystem<T1, T2>, IReactiveDestroy
	where T1 : struct, IComponent
	where T2 : struct, IComponent
	{

		public void SubcribeEntityDestroy()
		{
			foreach (World WorldNamed in NamedWorld.Instance.NamedWorlds)
			{
				WorldNamed.SubscribeEntityDestroyed((in Entity entity) =>
			{
				if (entity.TryGet(out T1 component1) && entity.TryGet(out T2 component2))
				{
					world = WorldNamed;
					if (GetTrigger(entity))
						Run(entity, ref component1, ref component2);

				}
			});
			}
		}
	}

	public abstract class GlobalDestroySystem<T1, T2, T3> : GlobalReactiveSystem<T1, T2, T3>, IReactiveDestroy
		where T1 : struct, IComponent
		where T2 : struct, IComponent
		where T3 : struct, IComponent
	{


		public void SubcribeEntityDestroy()
		{
			foreach (World WorldNamed in NamedWorld.Instance.NamedWorlds)
			{
				WorldNamed.SubscribeEntityDestroyed((in Entity entity) =>
				{
					if (entity.TryGet(out T1 component1) &&
						entity.TryGet(out T2 component2) &&
						entity.TryGet(out T3 component3))
					{
						world = WorldNamed;
						if (GetTrigger(entity))
							Run(entity, ref component1, ref component2, ref component3);

					}
				});
			}
		}
	}

	public abstract class GlobalDestroySystem<T1, T2, T3, T4> : GlobalReactiveSystem<T1, T2, T3, T4>, IReactiveDestroy
		where T1 : struct, IComponent
		where T2 : struct, IComponent
		where T3 : struct, IComponent
		where T4 : struct, IComponent
	{

		public void SubcribeEntityDestroy()
		{
			foreach (World WorldNamed in NamedWorld.Instance.NamedWorlds)
			{
				WorldNamed.SubscribeEntityDestroyed((in Entity entity) =>
				{
					if (entity.TryGet(out T1 component1) &&
						entity.TryGet(out T2 component2) &&
						entity.TryGet(out T3 component3) &&
						entity.TryGet(out T4 component4))
					{
						world = WorldNamed;
						if (GetTrigger(entity))
							Run(entity, ref component1, ref component2, ref component3, ref component4);
					}
				});
			}
		}
	}

	public abstract class GlobalDestroySystem<T1, T2, T3, T4, T5> : GlobalReactiveSystem<T1, T2, T3, T4, T5>, IReactiveDestroy
		where T1 : struct, IComponent
		where T2 : struct, IComponent
		where T3 : struct, IComponent
		where T4 : struct, IComponent
		where T5 : struct, IComponent
	{


		public void SubcribeEntityDestroy()
		{
			foreach (World WorldNamed in NamedWorld.Instance.NamedWorlds)
			{
				WorldNamed.SubscribeEntityDestroyed((in Entity entity) =>
			{
				if (entity.TryGet(out T1 component1) &&
					entity.TryGet(out T2 component2) &&
					entity.TryGet(out T3 component3) &&
					entity.TryGet(out T4 component4) &&
					entity.TryGet(out T5 component5))
				{
					world = WorldNamed;
					if (GetTrigger(entity))
						Run(entity, ref component1, ref component2, ref component3, ref component4, ref component5);
				}
			});
			}
		}
	}

	public abstract class GlobalDestroySystem<T1, T2, T3, T4, T5, T6> : GlobalReactiveSystem<T1, T2, T3, T4, T5, T6>, IReactiveDestroy
		where T1 : struct, IComponent
		where T2 : struct, IComponent
		where T3 : struct, IComponent
		where T4 : struct, IComponent
		where T5 : struct, IComponent
		where T6 : struct, IComponent
	{
		public void SubcribeEntityDestroy()
		{
			foreach (World WorldNamed in NamedWorld.Instance.NamedWorlds)
			{
				WorldNamed.SubscribeEntityDestroyed((in Entity entity) =>
				{
					if (entity.TryGet(out T1 component1) &&
						entity.TryGet(out T2 component2) &&
						entity.TryGet(out T3 component3) &&
						entity.TryGet(out T4 component4) &&
						entity.TryGet(out T5 component5) &&
						entity.TryGet(out T6 component6))
					{
						world = WorldNamed;
						if (GetTrigger(entity))
							Run(entity, ref component1, ref component2, ref component3, ref component4, ref component5, ref component6);
					}
				});
			}
		}
	}

}
