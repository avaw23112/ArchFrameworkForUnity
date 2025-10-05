#if UNITY_2020_1_OR_NEWER

using Assets.src.AOT.ECS.SystemScheduler;
using System;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace Arch
{
	public class UnityPlayerLoopScheduler : ISystemScheduler
	{
		private PlayerLoopSystem originalLoop;

		public void Start(Action update, Action lateUpdate)
		{
			originalLoop = PlayerLoop.GetCurrentPlayerLoop();
			var loop = PlayerLoop.GetCurrentPlayerLoop();

			var updateSys = new PlayerLoopSystem
			{
				type = typeof(UnityPlayerLoopScheduler),
				updateDelegate = () => update?.Invoke()
			};
			var lateSys = new PlayerLoopSystem
			{
				type = typeof(UnityPlayerLoopScheduler),
				updateDelegate = () => lateUpdate?.Invoke()
			};

			var subs = loop.subSystemList;
			subs = subs.InsertAfter(typeof(Update), updateSys);
			subs = subs.InsertAfter(typeof(PreLateUpdate), lateSys);
			loop.subSystemList = subs;
			PlayerLoop.SetPlayerLoop(loop);
		}

		public void Stop()
		{
			PlayerLoop.SetPlayerLoop(originalLoop);
		}
	}

	internal static class PlayerLoopHelperExt
	{
		public static PlayerLoopSystem[] InsertAfter(this PlayerLoopSystem[] list, Type type, PlayerLoopSystem item)
		{
			var newList = new System.Collections.Generic.List<PlayerLoopSystem>(list);
			for (int i = 0; i < newList.Count; i++)
			{
				if (newList[i].type == type)
				{
					newList.Insert(i + 1, item);
					break;
				}
			}
			return newList.ToArray();
		}
	}
}

#endif