using Attributes;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Tools;
using Tools.Pool;

namespace Events
{
	/// <summary>
	/// 消息管道的发布者
	/// </summary>
	public class EventBus : Singleton<EventBus>
	{
		// 每个事件类型独立存储处理器
		private static class Handlers<T> where T : struct
		{
			public static readonly List<Action<T>> actions = new List<Action<T>>(4);
			public static readonly List<Func<T, UniTask>> asyncActions = new List<Func<T, UniTask>>(2);
		}

		private List<IEvent> Events = new List<IEvent>();

		// 同步事件注册
		public static void Subscribe<T>(Action<T> handler) where T : struct
		{
			Handlers<T>.actions.Add(handler);
		}
		// 异步事件注册
		public static void SubscribeAsync<T>(Func<T, UniTask> handler) where T : struct
		{
			Handlers<T>.asyncActions.Add(handler);
		}
		public static void UnsubscribeAsync<T>(Func<T, UniTask> handler) where T : struct
		{
			Handlers<T>.asyncActions.Remove(handler);
		}
		// 同步事件取消注册
		public static void Unsubscribe<T>(Action<T> handler) where T : struct
		{
			Handlers<T>.actions.Remove(handler);
		}

		// 高性能同步发布（Burst兼容）
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Publish<T>(in T eventData) where T : struct
		{
			var actions = Handlers<T>.actions;
			for (int i = 0; i < actions.Count; i++)
			{
				actions[i](eventData);
			}
		}
		// 异步发布（自动处理线程切换）
		public static async UniTask PublishAsync<T>(T eventData) where T : struct
		{
			await UniTask.SwitchToMainThread();

			var actions = Handlers<T>.asyncActions;
			for (int i = 0; i < actions.Count; i++)
			{
				await actions[i](eventData);
			}
		}

		/// <summary>
		/// 注册全部事件
		/// </summary>
		public static void RegisterEvents()
		{
			if (Instance.Events.Count > 0)
			{
				Logger.Debug("事件已经注册");
				return;
			}
			List<Type> aEvents = ListPool<Type>.Get();
			Collector.CollectTypes<IEvent>(aEvents);
			try
			{
				foreach (var aEvent in aEvents)
				{
					if (aEvent.IsAbstract)
					{
						continue;
					}
					if (Collector.isForget(aEvent))
					{
						continue;
					}
					var genericArgs = aEvent.BaseType.GetGenericArguments();
					var aEventInstance = Activator.CreateInstance(aEvent) as IEvent;
					if (aEventInstance == null)
					{
						throw new Exception("创建事件实例失败");
					}
					if (genericArgs.Length <= 0)
					{
						throw new Exception("事件类型参数不足");
					}
					aEventInstance.Handle();
					Instance.Events.Add(aEventInstance);
				}
			}
			catch (Exception e)
			{
				Logger.Error(e.Message);
				throw;
			}
			finally
			{
				ListPool<Type>.Release(aEvents);
			}
		}
	}
}