
using Arch.Buffer;
using Arch.Core;
using System;

namespace Arch
{
	public abstract class BaseReactiveSystem : IReactiveSystem
	{
		protected World world;
		protected CommandBuffer commandBuffer;
		public void BuildIn(World world)
		{
			if (world == null)
			{
				throw new System.ArgumentNullException("world is null");
			}
			this.world = world;
			this.commandBuffer = new CommandBuffer();
		}
		public abstract QueryDescription Filter();
		public virtual bool GetTrigger(Entity entity) => true;

		#region 常用方法
		public Entity Create()
		{
			if (world == null)
			{
				throw new System.ArgumentNullException("world is null");
			}
			return world.Create();
		}
		public Entity Create<T>(T component) where T : struct, IComponent
		{
			if (world == null)
			{
				throw new System.ArgumentNullException("world is null");
			}
			return world.Create<T>(component);
		}
		public Entity CreateCommend<T>(T component) where T : struct, IComponent
		{
			if (world == null)
			{
				throw new System.ArgumentNullException("world is null");
			}
			if (commandBuffer == null)
			{
				throw new System.ArgumentNullException("commandBuffer is null");
			}
			return commandBuffer.Create(new Signature(typeof(T)));
		}
		public Entity Create<T1, T2>()
			where T1 : struct, IComponent
			where T2 : struct, IComponent
		{
			if (world == null)
			{
				throw new System.ArgumentNullException("world is null");
			}
			return world.Create<T1, T2>();
		}
		public Entity CreateCommend<T1, T2>()
			where T1 : struct, IComponent
			where T2 : struct, IComponent
		{
			if (world == null)
			{
				throw new System.ArgumentNullException("world is null");
			}
			if (commandBuffer == null)
			{
				throw new System.ArgumentNullException("commandBuffer is null");
			}
			return commandBuffer.Create(new Signature(typeof(T1), typeof(T2)));
		}

		public Entity Create<T1, T2, T3>()
			where T1 : struct, IComponent
			where T2 : struct, IComponent
			where T3 : struct, IComponent
		{
			if (world == null)
			{
				throw new System.ArgumentNullException("world is null");
			}
			return world.Create<T1, T2, T3>();
		}
		public Entity CreateCommend<T1, T2, T3>()
			where T1 : struct, IComponent
			where T2 : struct, IComponent
			where T3 : struct, IComponent
		{
			if (world == null)
			{
				throw new System.ArgumentNullException("world is null");
			}
			if (commandBuffer == null)
			{
				throw new System.ArgumentNullException("commandBuffer is null");
			}
			return commandBuffer.Create(new Signature(typeof(T1), typeof(T2), typeof(T3)));
		}

		public Entity Create<T1, T2, T3, T4>()
			where T1 : struct, IComponent
			where T2 : struct, IComponent
			where T3 : struct, IComponent
			where T4 : struct, IComponent
		{
			if (world == null)
			{
				throw new System.ArgumentNullException("world is null");
			}
			return world.Create<T1, T2, T3, T4>();
		}

		public Entity CreateCommend<T1, T2, T3, T4>()
			where T1 : struct, IComponent
			where T2 : struct, IComponent
			where T3 : struct, IComponent
			where T4 : struct, IComponent
		{
			if (world == null)
			{
				throw new System.ArgumentNullException("world is null");
			}
			if (commandBuffer == null)
			{
				throw new System.ArgumentNullException("commandBuffer is null");
			}
			return commandBuffer.Create(new Signature(typeof(T1), typeof(T2), typeof(T3), typeof(T4)));
		}
		public Entity Create<T1, T2, T3, T4, T5>()
			where T1 : struct, IComponent
			where T2 : struct, IComponent
			where T3 : struct, IComponent
			where T4 : struct, IComponent
			where T5 : struct, IComponent
		{
			if (world == null)
			{
				throw new System.ArgumentNullException("world is null");
			}
			return world.Create<T1, T2, T3, T4, T5>();
		}
		public Entity CreateCommend<T1, T2, T3, T4, T5>()
			where T1 : struct, IComponent
			where T2 : struct, IComponent
			where T3 : struct, IComponent
			where T4 : struct, IComponent
			where T5 : struct, IComponent
		{
			if (world == null)
			{
				throw new System.ArgumentNullException("world is null");
			}
			if (commandBuffer == null)
			{
				throw new System.ArgumentNullException("commandBuffer is null");
			}
			return commandBuffer.Create(new Signature(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5)));
		}

		public Entity Create<T1, T2, T3, T4, T5, T6>()
			where T1 : struct, IComponent
			where T2 : struct, IComponent
			where T3 : struct, IComponent
			where T4 : struct, IComponent
			where T5 : struct, IComponent
			where T6 : struct, IComponent
		{
			if (world == null)
			{
				throw new System.ArgumentNullException("world is null");
			}
			return world.Create<T1, T2, T3, T4, T5, T6>();
		}
		public Entity CreateCommend<T1, T2, T3, T4, T5, T6>()
			where T1 : struct, IComponent
			where T2 : struct, IComponent
			where T3 : struct, IComponent
			where T4 : struct, IComponent
			where T5 : struct, IComponent
			where T6 : struct, IComponent
		{
			if (world == null)
			{
				throw new System.ArgumentNullException("world is null");
			}
			if (commandBuffer == null)
			{
				throw new System.ArgumentNullException("commandBuffer is null");
			}
			return commandBuffer.Create(new Signature(typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6)));
		}

		public void DestroyEntity(Entity entity)
		{
			if (world == null)
			{
				throw new System.ArgumentNullException("world is null");
			}
			world.Destroy(entity);
		}

		public void DestroyCommend(Entity entity)
		{
			if (world == null)
			{
				throw new System.ArgumentNullException("world is null");
			}
			if (commandBuffer == null)
			{
				throw new System.ArgumentNullException("commandBuffer is null");
			}
			commandBuffer.Destroy(entity);
		}

		#endregion

	}
	public abstract class ReactiveSystem : BaseReactiveSystem, IReactiveSystem
	{
		protected abstract void Run(Entity entity);
	}

	public abstract class ReactiveSystem<T> : BaseReactiveSystem, IReactiveSystem
	{
		public override QueryDescription Filter()
		{
			return new QueryDescription().WithAll<T>();
		}
		protected abstract void Run(Entity entity, ref T component_T1);
	}
	public abstract class ReactiveSystem<T1, T2> : BaseReactiveSystem, IReactiveSystem
		where T1 : struct
		where T2 : struct
	{
		public override QueryDescription Filter()
		{
			return new QueryDescription().WithAll<T1, T2>();
		}

		protected abstract void Run(Entity entity, ref T1 component1, ref T2 component2);
	}

	public abstract class ReactiveSystem<T1, T2, T3> : BaseReactiveSystem, IReactiveSystem
		where T1 : struct
		where T2 : struct
		where T3 : struct
	{
		public override QueryDescription Filter()
		{
			return new QueryDescription().WithAll<T1, T2, T3>();
		}

		protected abstract void Run(Entity entity, ref T1 component1, ref T2 component2, ref T3 component3);
	}

	public abstract class ReactiveSystem<T1, T2, T3, T4> : BaseReactiveSystem, IReactiveSystem
		where T1 : struct
		where T2 : struct
		where T3 : struct
		where T4 : struct
	{
		public override QueryDescription Filter()
		{
			return new QueryDescription().WithAll<T1, T2, T3, T4>();
		}
		protected abstract void Run(Entity entity, ref T1 component1, ref T2 component2, ref T3 component3, ref T4 component4);
	}

	public abstract class ReactiveSystem<T1, T2, T3, T4, T5> : BaseReactiveSystem, IReactiveSystem
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

		protected abstract void Run(Entity entity, ref T1 component1, ref T2 component2, ref T3 component3, ref T4 component4, ref T5 component5);
	}

	public abstract class ReactiveSystem<T1, T2, T3, T4, T5, T6> : BaseReactiveSystem, IReactiveSystem
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

		protected abstract void Run(Entity entity, ref T1 component1, ref T2 component2, ref T3 component3, ref T4 component4, ref T5 component5, ref T6 component6);
	}

}
