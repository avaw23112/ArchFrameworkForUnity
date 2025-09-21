using Arch.Buffer;
using Arch.Core;

namespace Arch
{
    public abstract class BaseReactiveSystem : IReactiveSystem
    {
        protected World world;
        public CommandBuffer commandBuffer;

        public void BuildIn(World world)
        {
            if (world == null)
            {
                throw new System.ArgumentNullException("world is null");
            }
            this.world = world;
        }

        public virtual QueryDescription Filter() => new QueryDescription();

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
            commandBuffer = world.GetCommendBuffer();
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
            commandBuffer = world.GetCommendBuffer();
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
            commandBuffer = world.GetCommendBuffer();
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
            commandBuffer = world.GetCommendBuffer();
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
            commandBuffer = world.GetCommendBuffer();
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
            commandBuffer = world.GetCommendBuffer();
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

        public void DestroyEntityCommend(Entity entity)
        {
            if (world == null)
            {
                throw new System.ArgumentNullException("world is null");
            }
            commandBuffer = world.GetCommendBuffer();
            if (commandBuffer == null)
            {
                throw new System.ArgumentNullException("commandBuffer is null");
            }
            commandBuffer.Destroy(entity);
        }

        public T GetUniqueComponent<T>() where T : struct, IComponent
        {
            if (world == null)
            {
                throw new System.ArgumentNullException("world is null");
            }
            return SingletonComponent.GetOrAdd<T>();
        }

        public void SetUniqueComponent<T>(T component) where T : struct, IComponent
        {
            if (world == null)
            {
                throw new System.ArgumentNullException("world is null");
            }
            SingletonComponent.Set<T>(component);
        }

        #endregion 常用方法
    }
}