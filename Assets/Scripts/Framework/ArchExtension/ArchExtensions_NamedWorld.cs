using Arch.Core;
using System;
using System.Collections.Generic;
using Tools;

namespace Arch
{
	public class NamedWorld : Singleton<NamedWorld>
	{
		Dictionary<string, World> m_dicWorlds = new Dictionary<string, World>();

		public IEnumerable<World> NamedWorlds => m_dicWorlds.Values;

		public static void CreateNamed(string worldName)
		{
			if (Instance.m_dicWorlds.ContainsKey(worldName))
			{
				throw new Exception("World already exists!");
			}
			Instance.m_dicWorlds.Add(worldName, World.Create());

		}

		public static void DisposeNamed(string worldName)
		{
			World worldToDispose = null;
			if (!Instance.m_dicWorlds.TryGetValue(worldName, out worldToDispose))
			{
				throw new Exception("World not exists!");
			}
			worldToDispose.Dispose();
			Instance.m_dicWorlds.Remove(worldName);
		}

		public static World GetNamed(string worldName)
		{
			World worldToGet = null;
			if (!Instance.m_dicWorlds.TryGetValue(worldName, out worldToGet))
			{
				throw new Exception($"{worldName} World not exists!");
			}
			return worldToGet;
		}

		public static World GetOrCreateNamed(string worldName)
		{
			World worldToGet = null;
			if (!Instance.m_dicWorlds.TryGetValue(worldName, out worldToGet))
			{
				worldToGet = World.Create();
				Instance.m_dicWorlds.Add(worldName, worldToGet);
			}
			return worldToGet;
		}
	}
}
