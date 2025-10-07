using Arch.Core;
using Arch.Core.Extensions;


namespace Arch
{
	/// <summary>
	/// 当实体销毁时，触发系统的运行
	/// </summary>
	public abstract class DestroySystem : ReactiveSystem, IReactiveDestroy
	{
		public void SubcribeEntityDestroy()
		{
			world.SubscribeEntityDestroyed((in Entity entity) =>
			{
				if (GetTrigger(entity))
					Run(entity);
			});
		}
	}
	/// <summary>
	/// 当带有T组件的实体销毁时，触发系统的运行
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class DestroySystem<T> : ReactiveSystem<T>, IReactiveDestroy where T : IComponent
	{
		public void SubcribeEntityDestroy()
		{
			world.SubscribeComponentRemoved((in Entity entity, ref T component) =>
			{
				if (GetTrigger(entity))
					Run(entity, ref component);

			});
		}
	}

	public abstract class DestroySystem<T1, T2> : ReactiveSystem<T1, T2>, IReactiveDestroy
		where T1 : IComponent where T2 : IComponent
	{
		public void SubcribeEntityDestroy()
		{
			world.SubscribeEntityDestroyed((in Entity entity) =>
			{
				if (entity.TryGet(out T1 component1) && entity.TryGet(out T2 component2))
				{
					if (GetTrigger(entity))
						Run(entity, ref component1, ref component2);

				}
			});
		}
	}

	public abstract class DestroySystem<T1, T2, T3> : ReactiveSystem<T1, T2, T3>, IReactiveDestroy
		where T1 : IComponent where T2 : IComponent where T3 : IComponent
	{
		public void SubcribeEntityDestroy()
		{
			world.SubscribeEntityDestroyed((in Entity entity) =>
			{
				if (entity.TryGet(out T1 component1) &&
					entity.TryGet(out T2 component2) &&
					entity.TryGet(out T3 component3))
				{
					if (GetTrigger(entity))
						Run(entity, ref component1, ref component2, ref component3);

				}
			});
		}
	}
	public abstract class DestroySystem<T1, T2, T3, T4> : ReactiveSystem<T1, T2, T3, T4>, IReactiveDestroy
		where T1 : IComponent
		where T2 : IComponent
		where T3 : IComponent
		where T4 : IComponent
	{

		public void SubcribeEntityDestroy()
		{
			world.SubscribeEntityDestroyed((in Entity entity) =>
			{
				if (entity.TryGet(out T1 component1) &&
					entity.TryGet(out T2 component2) &&
					entity.TryGet(out T3 component3) &&
					entity.TryGet(out T4 component4))
				{
					if (GetTrigger(entity))
						Run(entity, ref component1, ref component2, ref component3, ref component4);

				}
			});
		}
	}

	public abstract class DestroySystem<T1, T2, T3, T4, T5> : ReactiveSystem<T1, T2, T3, T4, T5>, IReactiveDestroy
		where T1 : IComponent
		where T2 : IComponent
		where T3 : IComponent
		where T4 : IComponent
		where T5 : IComponent
	{
		public void SubcribeEntityDestroy()
		{
			world.SubscribeEntityDestroyed((in Entity entity) =>
			{
				if (entity.TryGet(out T1 component1) &&
					entity.TryGet(out T2 component2) &&
					entity.TryGet(out T3 component3) &&
					entity.TryGet(out T4 component4) &&
					entity.TryGet(out T5 component5))
				{
					if (GetTrigger(entity))
						Run(entity, ref component1, ref component2, ref component3, ref component4, ref component5);

				}
			});
		}
	}

	public abstract class DestroySystem<T1, T2, T3, T4, T5, T6> : ReactiveSystem<T1, T2, T3, T4, T5, T6>, IReactiveDestroy
		where T1 : IComponent
		where T2 : IComponent
		where T3 : IComponent
		where T4 : IComponent
		where T5 : IComponent
		where T6 : IComponent
	{
		public void SubcribeEntityDestroy()
		{
			world.SubscribeEntityDestroyed((in Entity entity) =>
			{
				if (entity.TryGet(out T1 component1) &&
					entity.TryGet(out T2 component2) &&
					entity.TryGet(out T3 component3) &&
					entity.TryGet(out T4 component4) &&
					entity.TryGet(out T5 component5) &&
					entity.TryGet(out T6 component6))
				{
					if (GetTrigger(entity))
						Run(entity, ref component1, ref component2, ref component3, ref component4, ref component5, ref component6);

				}
			});
		}
	}

}
