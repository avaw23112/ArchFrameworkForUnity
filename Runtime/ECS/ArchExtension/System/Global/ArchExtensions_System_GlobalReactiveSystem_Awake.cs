using Arch.Core;
using Arch.Core.Extensions;

namespace Arch
{
	//产生的闭包在可接受范围内，而且不会引起频繁GC	

	/// <summary>
	/// 初始版本，外界可完全掌控查询条件和过滤条件
	/// </summary>
	public abstract class GlobalAwakeSystem : GlobalReactiveSystem, IReactiveAwake
	{

		public void SubcribeEntityAwake()
		{
			foreach (World worldNamed in NamedWorld.Instance.NamedWorlds)
			{
				worldNamed.SubscribeEntityCreated((in Entity entity) =>
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
	public abstract class GlobalAwakeSystem<T> : GlobalReactiveSystem<T>, IReactiveAwake where T : struct, IComponent
	{
		public void SubcribeEntityAwake()
		{
			foreach (World WorldNamed in NamedWorld.Instance.NamedWorlds)
			{
				WorldNamed.SubscribeComponentAdded<T>((in Entity entity, ref T component) =>
				{
					world = WorldNamed;
					if (GetTrigger(entity))
						Run(entity, ref component);

				});
			}
		}
	}
	public abstract class GlobalAwakeSystem<T1, T2> : GlobalReactiveSystem<T1, T2>, IReactiveAwake
	where T1 : struct, IComponent
	where T2 : struct, IComponent
	{

		public void SubcribeEntityAwake()
		{
			foreach (World WorldNamed in NamedWorld.Instance.NamedWorlds)
			{
				WorldNamed.SubscribeEntityCreated((in Entity entity) =>
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

	public abstract class GlobalAwakeSystem<T1, T2, T3> : GlobalReactiveSystem<T1, T2, T3>, IReactiveAwake
		where T1 : struct, IComponent
		where T2 : struct, IComponent
		where T3 : struct, IComponent
	{


		public void SubcribeEntityAwake()
		{
			foreach (World WorldNamed in NamedWorld.Instance.NamedWorlds)
			{
				WorldNamed.SubscribeEntityCreated((in Entity entity) =>
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

	public abstract class GlobalAwakeSystem<T1, T2, T3, T4> : GlobalReactiveSystem<T1, T2, T3, T4>, IReactiveAwake
		where T1 : struct, IComponent
		where T2 : struct, IComponent
		where T3 : struct, IComponent
		where T4 : struct, IComponent
	{

		public void SubcribeEntityAwake()
		{
			foreach (World WorldNamed in NamedWorld.Instance.NamedWorlds)
			{
				WorldNamed.SubscribeEntityCreated((in Entity entity) =>
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

	public abstract class GlobalAwakeSystem<T1, T2, T3, T4, T5> : GlobalReactiveSystem<T1, T2, T3, T4, T5>, IReactiveAwake
		where T1 : struct, IComponent
		where T2 : struct, IComponent
		where T3 : struct, IComponent
		where T4 : struct, IComponent
		where T5 : struct, IComponent
	{


		public void SubcribeEntityAwake()
		{
			foreach (World WorldNamed in NamedWorld.Instance.NamedWorlds)
			{
				WorldNamed.SubscribeEntityCreated((in Entity entity) =>
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

	public abstract class GlobalAwakeSystem<T1, T2, T3, T4, T5, T6> : GlobalReactiveSystem<T1, T2, T3, T4, T5, T6>, IReactiveAwake
		where T1 : struct, IComponent
		where T2 : struct, IComponent
		where T3 : struct, IComponent
		where T4 : struct, IComponent
		where T5 : struct, IComponent
		where T6 : struct, IComponent
	{
		public void SubcribeEntityAwake()
		{
			foreach (World WorldNamed in NamedWorld.Instance.NamedWorlds)
			{
				WorldNamed.SubscribeEntityCreated((in Entity entity) =>
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
