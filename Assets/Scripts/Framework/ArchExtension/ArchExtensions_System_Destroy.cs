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
			commandBuffer.Playback(world);
		}
	}
	/// <summary>
	/// 当带有T组件的实体销毁时，触发系统的运行
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class DestroySystem<T> : ReactiveSystem<T>, IReactiveDestroy where T : struct, IComponent
	{
		public void SubcribeEntityDestroy()
		{
			world.SubscribeEntityDestroyed((in Entity entity) =>
			{
				if (entity.TryGet<T>(out T component))
				{
					if (GetTrigger(entity))
						Run(entity, ref component);
				}
			});
			commandBuffer.Playback(world);
		}
	}

	public abstract class DestroySystem<T1, T2> : ReactiveSystem<T1, T2>, IReactiveDestroy
		where T1 : struct, IComponent where T2 : struct, IComponent
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
			commandBuffer.Playback(world);
		}
	}

	public abstract class DestroySystem<T1, T2, T3> : ReactiveSystem<T1, T2, T3>, IReactiveDestroy
		where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent
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
			commandBuffer.Playback(world);
		}
	}
	public abstract class DestroySystem<T1, T2, T3, T4> : ReactiveSystem<T1, T2, T3, T4>, IReactiveDestroy
		where T1 : struct, IComponent
		where T2 : struct, IComponent
		where T3 : struct, IComponent
		where T4 : struct, IComponent
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
			commandBuffer.Playback(world);
		}
	}

	public abstract class DestroySystem<T1, T2, T3, T4, T5> : ReactiveSystem<T1, T2, T3, T4, T5>, IReactiveDestroy
		where T1 : struct, IComponent
		where T2 : struct, IComponent
		where T3 : struct, IComponent
		where T4 : struct, IComponent
		where T5 : struct, IComponent
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
			commandBuffer.Playback(world);
		}
	}

	public abstract class DestroySystem<T1, T2, T3, T4, T5, T6> : ReactiveSystem<T1, T2, T3, T4, T5, T6>, IReactiveDestroy
		where T1 : struct, IComponent
		where T2 : struct, IComponent
		where T3 : struct, IComponent
		where T4 : struct, IComponent
		where T5 : struct, IComponent
		where T6 : struct, IComponent
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
			commandBuffer.Playback(world);
		}
	}

}
