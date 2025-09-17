using Arch.Core;
using Cysharp.Threading.Tasks;

namespace Arch
{
	public abstract class ParallelReactiveSystem : BaseReactiveSystem
	{
		protected abstract UniTask Run(Entity entity);
	}

	public abstract class ParallelReactiveSystem<T> : BaseReactiveSystem
	{
		public override QueryDescription Filter()
		{
			return new QueryDescription().WithAll<T>();
		}
		protected abstract UniTask Run(Entity entity, ref T component_T1);
	}
	public abstract class ParallelReactiveSystem<T1, T2> : BaseReactiveSystem
		where T1 : struct
		where T2 : struct
	{
		public override QueryDescription Filter()
		{
			return new QueryDescription().WithAll<T1, T2>();
		}

		protected abstract UniTask Run(Entity entity, ref T1 component1, ref T2 component2);
	}

	public abstract class ParallelReactiveSystem<T1, T2, T3> : BaseReactiveSystem
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

	public abstract class ParallelReactiveSystem<T1, T2, T3, T4> : BaseReactiveSystem
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

	public abstract class ParallelReactiveSystem<T1, T2, T3, T4, T5> : BaseReactiveSystem
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

	public abstract class ParallelReactiveSystem<T1, T2, T3, T4, T5, T6> : BaseReactiveSystem
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


}
