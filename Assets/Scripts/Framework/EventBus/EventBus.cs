using Attributes;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using Tools;
using Tools.Pool;

namespace Events
{
	/// <summary>
	/// 消息管道的发布者
	/// </summary>
	public class EventBus : Singleton<EventBus>
	{
		private Dictionary<Type, IAEvent> m_eventDict = new Dictionary<Type, IAEvent>();

		/// <summary>
		/// 注册全部事件
		/// </summary>
		public static void RegisterEvents()
		{
			List<Type> aEvents = ListPool<Type>.Get();
			Collector.CollectTypes<IAEvent>(aEvents);
			try
			{
				foreach (var aEvent in aEvents)
				{
					if (aEvent.IsAbstract)
					{
						continue;
					}
					var genericArgs = aEvent.BaseType.GetGenericArguments();
					var aEventInstance = Activator.CreateInstance(aEvent) as IAEvent;
					if (aEventInstance == null)
					{
						throw new Exception("创建事件实例失败");
					}
					if (genericArgs.Length <= 0)
					{
						throw new Exception("事件类型参数不足");
					}
					Instance.m_eventDict.Add(genericArgs[0], aEventInstance);
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

		/// <summary>
		///  发布事件
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="eventData"></param>
		/// <exception cref="Exception"></exception>
		public static void Publish<T>(T eventData) where T : struct
		{
			IAEvent aEvent;
			MessagePipe.Instance.Push(eventData);
			if (Instance.m_eventDict.TryGetValue(typeof(T), out aEvent))
			{
				aEvent.Handle();
			}
			else
			{
				Logger.Error("事件类型未注册");
				throw new Exception("事件类型未注册");
			}
		}

		/// <summary>
		/// 发布事件，但在下一帧再执行事件
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="eventData"></param>
		/// <returns></returns>
		/// <exception cref="Exception"></exception>
		public static async void PublishAsync<T>(T eventData) where T : struct
		{
			IAEvent aEvent;
			MessagePipe.Instance.Push(eventData);
			await UniTask.Yield();
			if (Instance.m_eventDict.TryGetValue(typeof(T), out aEvent))
			{
				aEvent.Handle();
			}
			else
			{
				Logger.Error("事件类型未注册");
				throw new Exception("事件类型未注册");
			}
		}
	}
}