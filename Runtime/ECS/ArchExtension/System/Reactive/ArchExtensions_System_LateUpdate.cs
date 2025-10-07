using Arch.Core;


namespace Arch
{
	public abstract class LateUpdateSystem : ReactiveSystem, IReactiveLateUpdate
	{
		public void LateUpdate()
		{
			QueryDescription query = Filter();
			world.Query(in query, entity =>
			{
				if (GetTrigger(entity))
					Run(entity);
			});
		}
	}

	public abstract class LateUpdateSystem<T> : ReactiveSystem<T>, IReactiveLateUpdate where T : IComponent
	{

		public void LateUpdate()
		{
			QueryDescription query = Filter();
			world.Query(in query, (Entity e, ref T c) =>
			{
				if (GetTrigger(e))
					Run(e, ref c);
			});
		}
	}

	public abstract class LateUpdateSystem<T1, T2> : ReactiveSystem<T1, T2>, IReactiveLateUpdate
		where T1 : IComponent where T2 : IComponent
	{
		public void LateUpdate()
		{
			QueryDescription query = Filter();
			world.Query(in query, (Entity e, ref T1 c1, ref T2 c2) =>
			{
				if (GetTrigger(e))
					Run(e, ref c1, ref c2);
			});
		}
	}

	public abstract class LateUpdateSystem<T1, T2, T3> : ReactiveSystem<T1, T2, T3>, IReactiveLateUpdate
		where T1 : IComponent where T2 : IComponent where T3 : IComponent
	{

		public void LateUpdate()
		{
			QueryDescription query = Filter();
			world.Query(in query, (Entity e, ref T1 c1, ref T2 c2, ref T3 c3) =>
			{
				if (GetTrigger(e))
					Run(e, ref c1, ref c2, ref c3);
			});
		}
	}
	// 4 参数版本
	public abstract class LateUpdateSystem<T1, T2, T3, T4> : ReactiveSystem<T1, T2, T3, T4>, IReactiveLateUpdate
		where T1 : IComponent
		where T2 : IComponent
		where T3 : IComponent
		where T4 : IComponent
	{


		public void LateUpdate()
		{
			QueryDescription query = Filter();
			world.Query(in query, (Entity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4) =>
			{
				if (GetTrigger(e))
					Run(e, ref c1, ref c2, ref c3, ref c4);
			});
		}
	}

	// 5 参数版本
	public abstract class LateUpdateSystem<T1, T2, T3, T4, T5> : ReactiveSystem<T1, T2, T3, T4, T5>, IReactiveLateUpdate
		where T1 : IComponent
		where T2 : IComponent
		where T3 : IComponent
		where T4 : IComponent
		where T5 : IComponent
	{
		public void LateUpdate()
		{
			QueryDescription query = Filter();
			world.Query(in query, (Entity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5) =>
			{
				if (GetTrigger(e))
					Run(e, ref c1, ref c2, ref c3, ref c4, ref c5);
			});
		}
	}

	// 6 参数版本
	public abstract class LateUpdateSystem<T1, T2, T3, T4, T5, T6> : ReactiveSystem<T1, T2, T3, T4, T5, T6>, IReactiveLateUpdate
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
			world.Query(in query, (Entity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6) =>
			{
				if (GetTrigger(e))
					Run(e, ref c1, ref c2, ref c3, ref c4, ref c5, ref c6);
			});
		}
	}

}
