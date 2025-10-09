using Arch.Core;
using Arch.Core.Extensions;
using inEvent;
using RefEvent;
using System;

namespace Arch
{
	public static class Unique
	{
		public class Entity
		{
			private static Core.Entity m_UniqueEntity;

			public static Core.Entity Instance
			{
				get
				{
					if (m_UniqueEntity == Core.Entity.Null || !m_UniqueEntity.isVaild())
					{
						m_UniqueEntity = Unique.World.Instance.Create();
					}
					return m_UniqueEntity;
				}
			}
		}

		public class World
		{
			private static Core.World m_UniqueWorld;

			public static Core.World Instance
			{
				get
				{
					if (m_UniqueWorld == null)
					{
						m_UniqueWorld = NamedWorld.DefaultWord;
					}
					return m_UniqueWorld;
				}
			}

			public static void TearDown()
			{
				if (Entity.Instance.isVaild())
				{
					Instance.Destroy(Entity.Instance);
				}
			}
		}

		public abstract class LifecycleSystem<T> : DestroySystem<T>, IPureAwake where T : IComponent

		{
			public void Awake()
			{
				Unique.Component<T>.Setter((ref T component) => OnAwake(ref component));
			}

			protected override void Run(Core.Entity entity, ref T component)
			{
				OnDestroy(ref component);
			}

			protected abstract void OnAwake(ref T component);

			protected abstract void OnDestroy(ref T component);
		}

		public abstract class UpdateSystem<T> : IPureUpdate where T : IComponent
		{
			public void Update() => Component<T>.Setter((ref T component) => OnUpdate(ref component));

			protected abstract void OnUpdate(ref T component);
		}

		public abstract class LateUpdateSystem<T> : IPureLateUpdate where T : IComponent
		{
			public void LateUpdate()
				=> Unique.Component<T>.Setter((ref T component) => OnLateUpdate(ref component));

			protected abstract void OnLateUpdate(ref T component);
		}

		public class Component
		{
			public static void Set(ComponentType componentType, object component)
			{
				if (World.Instance.Has(Entity.Instance, componentType))
				{
					Entity.Instance.Set(component);
				}
				else
				{
					Entity.Instance.Add(component);
				}
			}
		}

		public class Component<T> where T : IComponent
		{
			public static T GetOrAdd()
			{
				if (!Entity.Instance.Has<T>())
				{
					throw new System.Exception($"Component {typeof(T)} is not singleton component");
				}
				return Entity.Instance.Get<T>();
			}

			public static void Set(T value)
			{
				if (Entity.Instance.Has<T>())
				{
					Entity.Instance.Set(value);
				}
				else
				{
					Entity.Instance.Add<T>();
					Entity.Instance.Set(value);
				}
			}

			public static void Getter(InAction<T> action)
			{
				if (!Entity.Instance.Has<T>())
				{
					Tools.ArchLog.LogError($"{Entity.Instance} not has the required components");
					throw new NullReferenceException($"{Entity.Instance} not has the component of {typeof(T)}");
				}
				T sComponent = Entity.Instance.Get<T>();
				action(in sComponent);
			}

			public static void Setter(RefAction<T> action)
			{
				if (!Entity.Instance.Has<T>())
				{
					Tools.ArchLog.LogError($"{Entity.Instance} not has the required components");
					throw new NullReferenceException($"{Entity.Instance} not has the component of {typeof(T)}");
				}
				T sComponent = Entity.Instance.Get<T>();
				action(ref sComponent);
				Entity.Instance.Set<T>(in sComponent);
			}
		}
	}
}