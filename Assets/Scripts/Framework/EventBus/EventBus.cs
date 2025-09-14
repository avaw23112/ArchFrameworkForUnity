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
		// 修改Handlers类（需补充到原有结构中）
		private static class Handlers<T> where T : struct
		{
			public static readonly List<Action<T>> actions = new List<Action<T>>();
			public static readonly List<Func<T, UniTask>> asyncActions = new List<Func<T, UniTask>>();
			public static int publishDepth; // 新增发布深度计数器
		}

		private const int MAX_DEPTH = 0;

		private List<IEvent> Events = new List<IEvent>();

		public static void Subscribe<T>(Action<T> handler) where T : struct
		{
			Handlers<T>.actions.Add(handler);
		}
		public static void SubscribeAsync<T>(Func<T, UniTask> handler) where T : struct
		{
			Handlers<T>.asyncActions.Add(handler);
		}
		public static void UnsubscribeAsync<T>(Func<T, UniTask> handler) where T : struct
		{
			Handlers<T>.asyncActions.Remove(handler);
		}
		public static void Unsubscribe<T>(Action<T> handler) where T : struct
		{
			Handlers<T>.actions.Remove(handler);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Publish<T>(in T eventData) where T : struct
		{
			if (Handlers<T>.publishDepth++ > MAX_DEPTH)
			{
				throw new InvalidOperationException($"递归触发事件 {typeof(T).Name} 被禁止");
			}
			try
			{
				var actions = Handlers<T>.actions;
				for (int i = 0; i < actions.Count; i++)
				{
					actions[i](eventData);
				}
			}
			finally
			{
				Handlers<T>.publishDepth--;
			}
		}

		public static async UniTask PublishAsync<T>(T eventData) where T : struct
		{
			await UniTask.SwitchToMainThread();

			if (Handlers<T>.publishDepth++ > 0)
			{
				throw new InvalidOperationException($"递归触发事件 {typeof(T).Name} 被禁止");
			}
			try
			{
				var actions = Handlers<T>.asyncActions;
				for (int i = 0; i < actions.Count; i++)
				{
					await actions[i](eventData);
				}
			}
			finally
			{
				Handlers<T>.publishDepth--;
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
			Collector.CollectTypesParallel<IEvent>(aEvents);
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