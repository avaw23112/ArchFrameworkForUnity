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
				if (Instance.IsAlive(Entity.Instance) && Entity.Instance.isVaild())
				{
					Instance.Destroy(Entity.Instance);
				}
			}
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