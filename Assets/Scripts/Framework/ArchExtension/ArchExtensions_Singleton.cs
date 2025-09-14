using Arch.Core;
using Arch.Core.Extensions;
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
					m_worldSingleton = World.Create();
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
		public static T GetSingle<T>(this World self) where T : struct, IComponent
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
		public static void SetSingle<T>(this World self, T value) where T : struct, IComponent
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
		public static T GetSingle<T>() where T : struct, IComponent
		{
			Entity entity;
			World self = WorldSingleton;
			if (!m_dicSingleEntity.TryGetValue(self, out entity))
			{
				entity = self.Create<T>();
				m_dicSingleEntity.Add(self, entity);
			}
			if (!entity.Has<T>())
			{
				throw new System.Exception($"Component {typeof(T)} is not singleton component");
			}
			return entity.Get<T>();
		}
		public static T GetOrAddSingle<T>() where T : struct, IComponent
		{
			Entity entity;
			World self = WorldSingleton;
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
		public static void SetSingle<T>(T value) where T : struct, IComponent
		{
			Entity entity;
			World self = WorldSingleton;
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
		public static bool RemoveSingle<T>(this World self) where T : struct, IComponent
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
	}
}
