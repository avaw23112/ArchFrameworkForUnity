using Arch.Core;
using System;
using System.Collections.Generic;
using Tools;

namespace Arch
{
	public class NamedWorld : Singleton<NamedWorld>
	{
		private static readonly Dictionary<string, World> m_dicWorlds = new Dictionary<string, World>();
		private World m_defaultWorld;

		public IEnumerable<World> NamedWorlds => m_dicWorlds.Values;
		public World DefaultWord
		{
			get
			{
				if (m_defaultWorld == null)
				{
					if (!m_dicWorlds.TryGetValue("Default", out m_defaultWorld))
					{
						Tools.Logger.Error("not found the default word!");
						throw new Exception("not found the default word!");
					}
				}
				return m_defaultWorld;
			}
		}


		public static void CreateNamed(string worldName)
		{
			if (m_dicWorlds.ContainsKey(worldName))
			{
				throw new Exception("World already exists!");
			}
			m_dicWorlds.Add(worldName, World.Create());

		}

		public static void DisposeNamed(string worldName)
		{
			World worldToDispose = null;
			if (!m_dicWorlds.TryGetValue(worldName, out worldToDispose))
			{
				throw new Exception("World not exists!");
			}
			worldToDispose.Dispose();
			m_dicWorlds.Remove(worldName);
		}

		public static World GetNamed(string worldName)
		{
			World worldToGet = null;
			if (!m_dicWorlds.TryGetValue(worldName, out worldToGet))
			{
				throw new Exception($"{worldName} World not exists!");
			}
			return worldToGet;
		}

		public static World GetOrCreateNamed(string worldName)
		{
			World worldToGet = null;
			if (!m_dicWorlds.TryGetValue(worldName, out worldToGet))
			{
				worldToGet = World.Create();
				m_dicWorlds.Add(worldName, worldToGet);
			}
			return worldToGet;
		}
	}
}
