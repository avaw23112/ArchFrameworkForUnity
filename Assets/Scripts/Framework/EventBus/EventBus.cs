using Attributes;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using Tools;
using Tools.Pool;

namespace Events
{
	/// <summary>
	/// ��Ϣ�ܵ��ķ�����
	/// </summary>
	public class EventBus : Singleton<EventBus>
	{
		private Dictionary<Type, IAEvent> m_eventDict = new Dictionary<Type, IAEvent>();

		/// <summary>
		/// ע��ȫ���¼�
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
						throw new Exception("�����¼�ʵ��ʧ��");
					}
					if (genericArgs.Length <= 0)
					{
						throw new Exception("�¼����Ͳ�������");
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
		///  �����¼�
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
				Logger.Error("�¼�����δע��");
				throw new Exception("�¼�����δע��");
			}
		}

		/// <summary>
		/// �����¼���������һ֡��ִ���¼�
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
				Logger.Error("�¼�����δע��");
				throw new Exception("�¼�����δע��");
			}
		}
	}
}