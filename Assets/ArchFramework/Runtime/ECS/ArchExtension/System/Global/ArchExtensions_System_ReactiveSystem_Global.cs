
using Arch.Core;

namespace Arch
{
	public abstract class GlobalReactiveSystem : BaseReactiveSystem, IReactiveSystem, IGlobalSystem
	{
		protected abstract void Run(Entity entity);
	}

	public abstract class GlobalReactiveSystem<T> : BaseReactiveSystem, IReactiveSystem, IGlobalSystem
	{
		public override QueryDescription Filter()
		{
			return new QueryDescription().WithAll<T>();
		}
		protected abstract void Run(Entity entity, ref T component_T1);
	}
	public abstract class GlobalReactiveSystem<T1, T2> : BaseReactiveSystem, IReactiveSystem, IGlobalSystem
		where T1 : IComponent
		where T2 : IComponent
	{
		public override QueryDescription Filter()
		{
			return new QueryDescription().WithAll<T1, T2>();
		}

		protected abstract void Run(Entity entity, ref T1 component1, ref T2 component2);
	}

	public abstract class GlobalReactiveSystem<T1, T2, T3> : BaseReactiveSystem, IReactiveSystem, IGlobalSystem
		where T1 : IComponent
		where T2 : IComponent
		where T3 : IComponent
	{
		public override QueryDescription Filter()
		{
			return new QueryDescription().WithAll<T1, T2, T3>();
		}

		protected abstract void Run(Entity entity, ref T1 component1, ref T2 component2, ref T3 component3);
	}

	public abstract class GlobalReactiveSystem<T1, T2, T3, T4> : BaseReactiveSystem, IReactiveSystem, IGlobalSystem
		where T1 : IComponent
		where T2 : IComponent
		where T3 : IComponent
		where T4 : IComponent
	{
		public override QueryDescription Filter()
		{
			return new QueryDescription().WithAll<T1, T2, T3, T4>();
		}
		protected abstract void Run(Entity entity, ref T1 component1, ref T2 component2, ref T3 component3, ref T4 component4);
	}

	public abstract class GlobalReactiveSystem<T1, T2, T3, T4, T5> : BaseReactiveSystem, IReactiveSystem, IGlobalSystem
		where T1 : IComponent
		where T2 : IComponent
		where T3 : IComponent
		where T4 : IComponent
		where T5 : IComponent
	{
		public override QueryDescription Filter()
		{
			return new QueryDescription().WithAll<T1, T2, T3, T4, T5>();
		}

		protected abstract void Run(Entity entity, ref T1 component1, ref T2 component2, ref T3 component3, ref T4 component4, ref T5 component5);
	}

	public abstract class GlobalReactiveSystem<T1, T2, T3, T4, T5, T6> : BaseReactiveSystem, IReactiveSystem, IGlobalSystem
		where T1 : IComponent
		where T2 : IComponent
		where T3 : IComponent
		where T4 : IComponent
		where T5 : IComponent
		where T6 : IComponent
	{
		public override QueryDescription Filter()
		{
			return new QueryDescription().WithAll<T1, T2, T3, T4, T5, T6>();
		}

		protected abstract void Run(Entity entity, ref T1 component1, ref T2 component2, ref T3 component3, ref T4 component4, ref T5 component5, ref T6 component6);
	}
}
