
using Arch.Core;
using Cysharp.Threading.Tasks;

namespace Arch
{
	public abstract class ParallelReactiveSystem : BaseReactiveSystem, IReactiveSystem
	{
		protected abstract UniTask Run(Entity entity);
	}

	public abstract class ParallelReactiveSystem<T> : BaseReactiveSystem, IReactiveSystem
	{
		public override QueryDescription Filter()
		{
			return new QueryDescription().WithAll<T>();
		}
		protected abstract UniTask Run(Entity entity, ref T component_T1);
	}
	public abstract class ParallelReactiveSystem<T1, T2> : BaseReactiveSystem, IReactiveSystem
		where T1 : struct
		where T2 : struct
	{
		public override QueryDescription Filter()
		{
			return new QueryDescription().WithAll<T1, T2>();
		}

		protected abstract UniTask Run(Entity entity, ref T1 component1, ref T2 component2);
	}

	public abstract class ParallelReactiveSystem<T1, T2, T3> : BaseReactiveSystem, IReactiveSystem
		where T1 : struct
		where T2 : struct
		where T3 : struct
	{
		public override QueryDescription Filter()
		{
			return new QueryDescription().WithAll<T1, T2, T3>();
		}

		protected abstract UniTask Run(Entity entity, ref T1 component1, ref T2 component2, ref T3 component3);
	}

	public abstract class ParallelReactiveSystem<T1, T2, T3, T4> : BaseReactiveSystem, IReactiveSystem
		where T1 : struct
		where T2 : struct
		where T3 : struct
		where T4 : struct
	{
		public override QueryDescription Filter()
		{
			return new QueryDescription().WithAll<T1, T2, T3, T4>();
		}
		protected abstract UniTask Run(Entity entity, ref T1 component1, ref T2 component2, ref T3 component3, ref T4 component4);
	}

	public abstract class ParallelReactiveSystem<T1, T2, T3, T4, T5> : BaseReactiveSystem, IReactiveSystem
		where T1 : struct
		where T2 : struct
		where T3 : struct
		where T4 : struct
		where T5 : struct
	{
		public override QueryDescription Filter()
		{
			return new QueryDescription().WithAll<T1, T2, T3, T4, T5>();
		}

		protected abstract UniTask Run(Entity entity, ref T1 component1, ref T2 component2, ref T3 component3, ref T4 component4, ref T5 component5);
	}

	public abstract class ParallelReactiveSystem<T1, T2, T3, T4, T5, T6> : BaseReactiveSystem, IReactiveSystem
		where T1 : struct
		where T2 : struct
		where T3 : struct
		where T4 : struct
		where T5 : struct
		where T6 : struct
	{
		public override QueryDescription Filter()
		{
			return new QueryDescription().WithAll<T1, T2, T3, T4, T5, T6>();
		}

		protected abstract UniTask Run(Entity entity, ref T1 component1, ref T2 component2, ref T3 component3, ref T4 component4, ref T5 component5, ref T6 component6);
	}
	/// <summary>
	/// 初始版本，外界可完全掌控查询条件和过滤条件
	/// </summary>
	public abstract class ParallelUpdateSystem : ParallelReactiveSystem, IReactiveUpdate
	{
		public void Update()
		{
			QueryDescription vQueryDescription = Filter();
			world.ParallelQuery(in vQueryDescription,
				 (Entity entity) =>
				{
					if (GetTrigger(entity))
						Run(entity).Forget();
				});
			commandBuffer.Playback(world);
		}
	}

	public abstract class ParallelUpdateSystem<T> : ParallelReactiveSystem<T>, IReactiveUpdate
		where T : struct, IComponent
	{
		public void Update()
		{
			QueryDescription vQueryDescription = Filter();
			world.ParallelQuery(in vQueryDescription,
				 (Entity entity, ref T component) =>
				{
					if (GetTrigger(entity))
						Run(entity, ref component).Forget();
				});
			commandBuffer.Playback(world);
		}
	}

	// 2 参数版本
	public abstract class ParallelUpdateSystem<T1, T2> : ParallelReactiveSystem<T1, T2>, IReactiveUpdate
		where T1 : struct, IComponent
		where T2 : struct, IComponent
	{
		public void Update()
		{
			QueryDescription query = Filter();
			world.ParallelQuery(in query,
				(Entity entity, ref T1 c1, ref T2 c2) =>
				{
					if (GetTrigger(entity))
						Run(entity, ref c1, ref c2).Forget();
				});
			commandBuffer.Playback(world);
		}
	}

	// 3 参数版本
	public abstract class ParallelUpdateSystem<T1, T2, T3> : ParallelReactiveSystem<T1, T2, T3>, IReactiveUpdate
		where T1 : struct, IComponent
		where T2 : struct, IComponent
		where T3 : struct, IComponent
	{
		public void Update()
		{
			QueryDescription query = Filter();
			world.ParallelQuery(in query,
				(Entity entity, ref T1 c1, ref T2 c2, ref T3 c3) =>
				{
					if (GetTrigger(entity))
						Run(entity, ref c1, ref c2, ref c3).Forget();
				});
			commandBuffer.Playback(world);
		}
	}

	// 4 参数版本
	public abstract class ParallelUpdateSystem<T1, T2, T3, T4> : ParallelReactiveSystem<T1, T2, T3, T4>, IReactiveUpdate
		where T1 : struct, IComponent
		where T2 : struct, IComponent
		where T3 : struct, IComponent
		where T4 : struct, IComponent
	{
		public void Update()
		{
			QueryDescription query = Filter();
			world.ParallelQuery(in query,
				(Entity entity, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4) =>
				{
					if (GetTrigger(entity))
						Run(entity, ref c1, ref c2, ref c3, ref c4).Forget();
				});
			commandBuffer.Playback(world);
		}
	}

	// 5 参数版本
	public abstract class ParallelUpdateSystem<T1, T2, T3, T4, T5> : ParallelReactiveSystem<T1, T2, T3, T4, T5>, IReactiveUpdate
		where T1 : struct, IComponent
		where T2 : struct, IComponent
		where T3 : struct, IComponent
		where T4 : struct, IComponent
		where T5 : struct, IComponent
	{
		public void Update()
		{
			QueryDescription query = Filter();
			world.ParallelQuery(in query,
				(Entity entity, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5) =>
				{
					if (GetTrigger(entity))
						Run(entity, ref c1, ref c2, ref c3, ref c4, ref c5).Forget();
				});
			commandBuffer.Playback(world);
		}
	}

	// 6 参数版本
	public abstract class ParallelUpdateSystem<T1, T2, T3, T4, T5, T6> : ParallelReactiveSystem<T1, T2, T3, T4, T5, T6>, IReactiveUpdate
		where T1 : struct, IComponent
		where T2 : struct, IComponent
		where T3 : struct, IComponent
		where T4 : struct, IComponent
		where T5 : struct, IComponent
		where T6 : struct, IComponent
	{
		public void Update()
		{
			QueryDescription query = Filter();
			world.ParallelQuery(in query,
				(Entity entity, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6) =>
				{
					if (GetTrigger(entity))
						Run(entity, ref c1, ref c2, ref c3, ref c4, ref c5, ref c6).Forget();
				});
			commandBuffer.Playback(world);
		}
	}

	public abstract class ParallelLateUpdateSystem : ParallelReactiveSystem, IReactiveLateUpdate
	{
		public void LateUpdate()
		{
			QueryDescription query = Filter();
			world.ParallelQuery(in query, entity =>
			{
				if (GetTrigger(entity))
					Run(entity);
			});
			commandBuffer.Playback(world);
		}
	}

	public abstract class ParallelLateUpdateSystem<T> : ParallelReactiveSystem<T>, IReactiveLateUpdate where T : struct, IComponent
	{

		public void LateUpdate()
		{
			QueryDescription query = Filter();
			world.ParallelQuery(in query, (Entity e, ref T c) =>
			{
				if (GetTrigger(e))
					Run(e, ref c);
			});
			commandBuffer.Playback(world);
		}
	}

	public abstract class ParallelLateUpdateSystem<T1, T2> : ParallelReactiveSystem<T1, T2>, IReactiveLateUpdate
		where T1 : struct, IComponent where T2 : struct, IComponent
	{
		public void LateUpdate()
		{
			QueryDescription query = Filter();
			world.ParallelQuery(in query, (Entity e, ref T1 c1, ref T2 c2) =>
			{
				if (GetTrigger(e))
					Run(e, ref c1, ref c2);
			});
			commandBuffer.Playback(world);
		}
	}

	public abstract class ParallelLateUpdateSystem<T1, T2, T3> : ParallelReactiveSystem<T1, T2, T3>, IReactiveLateUpdate
		where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent
	{

		public void LateUpdate()
		{
			QueryDescription query = Filter();
			world.ParallelQuery(in query, (Entity e, ref T1 c1, ref T2 c2, ref T3 c3) =>
			{
				if (GetTrigger(e))
					Run(e, ref c1, ref c2, ref c3);
			});
			commandBuffer.Playback(world);
		}
	}
	public abstract class ParallelLateUpdateSystem<T1, T2, T3, T4> : ParallelReactiveSystem<T1, T2, T3, T4>, IReactiveLateUpdate
		where T1 : struct, IComponent
		where T2 : struct, IComponent
		where T3 : struct, IComponent
		where T4 : struct, IComponent
	{


		public void LateUpdate()
		{
			QueryDescription query = Filter();
			world.ParallelQuery(in query, (Entity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4) =>
			{
				if (GetTrigger(e))
					Run(e, ref c1, ref c2, ref c3, ref c4);
			});
			commandBuffer.Playback(world);
		}
	}

	public abstract class ParallelLateUpdateSystem<T1, T2, T3, T4, T5> : ParallelReactiveSystem<T1, T2, T3, T4, T5>, IReactiveLateUpdate
		where T1 : struct, IComponent
		where T2 : struct, IComponent
		where T3 : struct, IComponent
		where T4 : struct, IComponent
		where T5 : struct, IComponent
	{
		public void LateUpdate()
		{
			QueryDescription query = Filter();
			world.ParallelQuery(in query, (Entity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5) =>
			{
				if (GetTrigger(e))
					Run(e, ref c1, ref c2, ref c3, ref c4, ref c5);
			});
			commandBuffer.Playback(world);
		}
	}

	public abstract class ParallelLateUpdateSystem<T1, T2, T3, T4, T5, T6> : ParallelReactiveSystem<T1, T2, T3, T4, T5, T6>, IReactiveLateUpdate
		where T1 : struct, IComponent
		where T2 : struct, IComponent
		where T3 : struct, IComponent
		where T4 : struct, IComponent
		where T5 : struct, IComponent
		where T6 : struct, IComponent
	{
		public void LateUpdate()
		{
			QueryDescription query = Filter();
			world.ParallelQuery(in query, (Entity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6) =>
			{
				if (GetTrigger(e))
					Run(e, ref c1, ref c2, ref c3, ref c4, ref c5, ref c6);
			});
			commandBuffer.Playback(world);
		}
	}
}
