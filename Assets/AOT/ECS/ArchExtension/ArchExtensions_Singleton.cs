using Arch.Core;
using Arch.Core.Extensions;
using Arch.Tools;
using inEvent;
using RefEvent;
using System;
using System.Collections.Generic;

namespace Arch
{
	public static class SingletonComponent
	{
		private static World m_worldSingleton;
		private static Entity m_entitySingleton;

		public static World WorldSingleton
		{
			get
			{
				if (m_worldSingleton == null)
				{
					m_worldSingleton = NamedWorld.DefaultWord;
				}
				return m_worldSingleton;
			}
		}

		public static Entity EntitySingleton
		{
			get
			{
				if (m_entitySingleton == null)
				{
					m_entitySingleton = WorldSingleton.Create();
				}
				return m_entitySingleton;
			}
		}

		private static Dictionary<World, Entity> m_dicSingleEntity = new Dictionary<World, Entity>();

		public static T GetSingle<T>(this World self) where T : IComponent
		{
			Entity entity;
			if (!m_dicSingleEntity.TryGetValue(self, out entity))
			{
				entity = self.Create<T>();
				m_dicSingleEntity.Add(self, entity);
			}
			if (entity.Has<T>())
			{
				return entity.Get<T>();
			}
			else
			{
				entity.Add<T>();
				return entity.Get<T>();
			}
		}

		public static void SetSingle<T>(this World self, T value) where T : IComponent
		{
			Entity entity;
			if (!m_dicSingleEntity.TryGetValue(self, out entity))
			{
				entity = self.Create<T>();
				m_dicSingleEntity.Add(self, entity);
			}
			if (entity.Has<T>())
			{
				entity.Set(value);
			}
			else
			{
				entity.Add<T>();
				entity.Set(value);
			}
		}

		public static T GetOrAdd<T>() where T : IComponent
		{
			World self = WorldSingleton;
			if (!m_dicSingleEntity.TryGetValue(self, out m_entitySingleton))
			{
				m_entitySingleton = self.Create<T>();
				m_dicSingleEntity.Add(self, m_entitySingleton);
			}
			if (!m_entitySingleton.Has<T>())
			{
				throw new System.Exception($"Component {typeof(T)} is not singleton component");
			}
			return m_entitySingleton.Get<T>();
		}

		public static void Set<T>(T value) where T : IComponent
		{
			World self = WorldSingleton;
			if (!m_dicSingleEntity.TryGetValue(self, out m_entitySingleton))
			{
				m_entitySingleton = self.Create<T>();
				m_dicSingleEntity.Add(self, m_entitySingleton);
			}
			if (m_entitySingleton.Has<T>())
			{
				m_entitySingleton.Set(value);
			}
			else
			{
				m_entitySingleton.Add<T>();
				m_entitySingleton.Set(value);
			}
		}

		public static bool RemoveSingle<T>(this World self) where T : IComponent
		{
			Entity entity;
			if (m_dicSingleEntity.TryGetValue(self, out entity))
			{
				if (entity.Has<T>())
				{
					entity.Remove<T>();
					return true;
				}
			}
			return false;
		}
		public static void Clear()
		{
			try
			{
				m_worldSingleton.Destroy(m_entitySingleton);
				foreach (var kv in m_dicSingleEntity)
				{
					World world = kv.Key;
					Entity entity = kv.Value;
					if (world.IsAlive(entity) && entity.isVaild())
					{
						world.Destroy(entity);
					}
				}
			}
			catch (Exception e)
			{
				ArchLog.LogError(e);
				throw;
			}
		}

		public static void Getter<T>(InAction<T> action) where T : IComponent
		{
			var entity = EntitySingleton;
			if (!entity.Has<T>())
			{
				Tools.ArchLog.LogError($"{entity} not has the required components");
				throw new NullReferenceException($"{entity} not has the component of {typeof(T)}");
			}
			T sComponent = entity.Get<T>();
			action(in sComponent);
		}

		public static void Setter<T>(RefAction<T> action) where T : IComponent
		{
			var entity = EntitySingleton;
			if (!entity.Has<T>())
			{
				Tools.ArchLog.LogError($"{entity} not has the required components");
				throw new NullReferenceException($"{entity} not has the component of {typeof(T)}");
			}
			T sComponent = entity.Get<T>();
			action(ref sComponent);
			entity.Set<T>(in sComponent);
		}
	}
}