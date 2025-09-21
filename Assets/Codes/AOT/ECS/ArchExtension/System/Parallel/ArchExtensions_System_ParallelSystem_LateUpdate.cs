using Arch;
using Arch.Core;
using Cysharp.Threading.Tasks;


namespace Arch
{
	public abstract class ParallelLateUpdateSystem : ParallelReactiveSystem, IReactiveLateUpdate
	{
		public void LateUpdate()
		{
			QueryDescription query = Filter();
			world.ParallelQuery(in query, entity =>
			{
				if (GetTrigger(entity))
					Run(entity).Forget();
			});
			commandBuffer.Playback(world);
		}
	}

	public abstract class ParallelLateUpdateSystem<T> : ParallelReactiveSystem<T>, IReactiveLateUpdate where T : IComponent
	{

		public void LateUpdate()
		{
			QueryDescription query = Filter();
			world.ParallelQuery(in query, (Entity e, ref T c) =>
			{
				if (GetTrigger(e))
					Run(e, ref c).Forget();
			});
			commandBuffer.Playback(world);
		}
	}

	public abstract class ParallelLateUpdateSystem<T1, T2> : ParallelReactiveSystem<T1, T2>, IReactiveLateUpdate
		where T1 : IComponent where T2 : IComponent
	{
		public void LateUpdate()
		{
			QueryDescription query = Filter();
			world.ParallelQuery(in query, (Entity e, ref T1 c1, ref T2 c2) =>
			{
				if (GetTrigger(e))
					Run(e, ref c1, ref c2).Forget();
			});
			commandBuffer.Playback(world);
		}
	}

	public abstract class ParallelLateUpdateSystem<T1, T2, T3> : ParallelReactiveSystem<T1, T2, T3>, IReactiveLateUpdate
		where T1 : IComponent where T2 : IComponent where T3 : IComponent
	{

		public void LateUpdate()
		{
			QueryDescription query = Filter();
			world.ParallelQuery(in query, (Entity e, ref T1 c1, ref T2 c2, ref T3 c3) =>
			{
				if (GetTrigger(e))
					Run(e, ref c1, ref c2, ref c3).Forget();
			});
			commandBuffer.Playback(world);
		}
	}
	public abstract class ParallelLateUpdateSystem<T1, T2, T3, T4> : ParallelReactiveSystem<T1, T2, T3, T4>, IReactiveLateUpdate
		where T1 : IComponent
		where T2 : IComponent
		where T3 : IComponent
		where T4 : IComponent
	{


		public void LateUpdate()
		{
			QueryDescription query = Filter();
			world.ParallelQuery(in query, (Entity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4) =>
			{
				if (GetTrigger(e))
					Run(e, ref c1, ref c2, ref c3, ref c4).Forget();
			});
			commandBuffer.Playback(world);
		}
	}

	public abstract class ParallelLateUpdateSystem<T1, T2, T3, T4, T5> : ParallelReactiveSystem<T1, T2, T3, T4, T5>, IReactiveLateUpdate
		where T1 : IComponent
		where T2 : IComponent
		where T3 : IComponent
		where T4 : IComponent
		where T5 : IComponent
	{
		public void LateUpdate()
		{
			QueryDescription query = Filter();
			world.ParallelQuery(in query, (Entity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5) =>
			{
				if (GetTrigger(e))
					Run(e, ref c1, ref c2, ref c3, ref c4, ref c5).Forget();
			});
			commandBuffer.Playback(world);
		}
	}

	public abstract class ParallelLateUpdateSystem<T1, T2, T3, T4, T5, T6> : ParallelReactiveSystem<T1, T2, T3, T4, T5, T6>, IReactiveLateUpdate
		where T1 : IComponent
		where T2 : IComponent
		where T3 : IComponent
		where T4 : IComponent
		where T5 : IComponent
		where T6 : IComponent
	{
		public void LateUpdate()
		{
			QueryDescription query = Filter();
			world.ParallelQuery(in query, (Entity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6) =>
			{
				if (GetTrigger(e))
					Run(e, ref c1, ref c2, ref c3, ref c4, ref c5, ref c6).Forget();
			});
			commandBuffer.Playback(world);
		}
	}
}
