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
	/// ��Ϣ�ܵ��ķ�����
	/// </summary>
	public class EventBus : Singleton<EventBus>
	{
		// ÿ���¼����Ͷ����洢������
		private static class Handlers<T> where T : struct
		{
			public static readonly List<Action<T>> actions = new List<Action<T>>(4);
			public static readonly List<Func<T, UniTask>> asyncActions = new List<Func<T, UniTask>>(2);
		}

		private List<IEvent> Events = new List<IEvent>();

		// ͬ���¼�ע��
		public static void Subscribe<T>(Action<T> handler) where T : struct
		{
			Handlers<T>.actions.Add(handler);
		}
		// �첽�¼�ע��
		public static void SubscribeAsync<T>(Func<T, UniTask> handler) where T : struct
		{
			Handlers<T>.asyncActions.Add(handler);
		}
		public static void UnsubscribeAsync<T>(Func<T, UniTask> handler) where T : struct
		{
			Handlers<T>.asyncActions.Remove(handler);
		}
		// ͬ���¼�ȡ��ע��
		public static void Unsubscribe<T>(Action<T> handler) where T : struct
		{
			Handlers<T>.actions.Remove(handler);
		}

		// ������ͬ��������Burst���ݣ�
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Publish<T>(in T eventData) where T : struct
		{
			var actions = Handlers<T>.actions;
			for (int i = 0; i < actions.Count; i++)
			{
				actions[i](eventData);
			}
		}
		// �첽�������Զ������߳��л���
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
		/// ע��ȫ���¼�
		/// </summary>
		public static void RegisterEvents()
		{
			if (Instance.Events.Count > 0)
			{
				Logger.Debug("�¼��Ѿ�ע��");
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
						throw new Exception("�����¼�ʵ��ʧ��");
					}
					if (genericArgs.Length <= 0)
					{
						throw new Exception("�¼����Ͳ�������");
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