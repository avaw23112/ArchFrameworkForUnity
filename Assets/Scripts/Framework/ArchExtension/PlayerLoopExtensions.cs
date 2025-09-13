using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.LowLevel;

namespace Arch
{
	public static class PlayerLoopExtensions
	{ // 链式批量插入API
		public static T[] InsertAfter<T>(this T[] source, int index, T newItem)
		{
			if (index < 0 || index >= source.Length)
				throw new IndexOutOfRangeException();

			T[] newArray = new T[source.Length + 1];

			// 复制插入点之前的元素
			Array.Copy(source, 0, newArray, 0, index + 1);

			// 插入新元素
			newArray[index + 1] = newItem;

			// 复制插入点之后的元素
			Array.Copy(source, index + 1, newArray, index + 2, source.Length - index - 1);

			return newArray;
		}
		public static PlayerLoopSystem InsertSystems(
			this PlayerLoopSystem root,
			params (Type target, PlayerLoopSystem system)[] inserts)
		{
			var insertList = new List<(int index, PlayerLoopSystem system)>();
			var subsystems = root.subSystemList;

			// 定位所有插入点
			for (int i = 0; i < subsystems.Length; i++)
			{
				foreach (var insert in inserts)
				{
					if (subsystems[i].type == insert.target)
					{
						insertList.Add((i + 1, insert.system));
					}
				}
			}

			// 按逆序执行插入
			var list = new List<PlayerLoopSystem>(subsystems);
			foreach (var item in insertList.OrderByDescending(x => x.index))
			{
				if (item.index <= list.Count)
				{
					list.Insert(item.index, item.system);
				}
			}

			root.subSystemList = list.ToArray();
			return root;
		}
		public static void InsertSystemAfter<T>(this PlayerLoopSystem system, PlayerLoopSystem newSystem)
			where T : struct
		{
			var loopList = system.subSystemList.ToList();
			int index = loopList.FindIndex(s => s.type == typeof(T));
			if (index >= 0)
			{
				loopList.Insert(index + 1, newSystem);
				system.subSystemList = loopList.ToArray();
			}
		}

		public static void InsertSystemBefore<T>(this PlayerLoopSystem system, PlayerLoopSystem newSystem)
			where T : struct
		{
			var loopList = system.subSystemList.ToList();
			int index = loopList.FindIndex(s => s.type == typeof(T));
			if (index >= 0)
			{
				loopList.Insert(index, newSystem);
				system.subSystemList = loopList.ToArray();
			}
		}

		public static void InsertSystemWhenDestroy(this PlayerLoopSystem system, PlayerLoopSystem destroySystem)
		{
#if UNITY_EDITOR
			EditorApplication.playModeStateChanged += (state) => destroySystem.updateDelegate();
#else
            Application.quitting += () => destroySystem.updateDelegate();
#endif
		}
	}
}