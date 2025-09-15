using Arch.Tools;
using Arch.Tools.Pool;
using Attributes;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Events
{
    /// <summary>
    /// ��Ϣ�ܵ��ķ�����
    /// </summary>
    public partial class EventBus : Singleton<EventBus>
    {
        private const int MAX_DEPTH = 0;

        private static class Handlers<T> where T : struct
        {
            public static readonly List<Action<T>> actions = new List<Action<T>>();
            public static int publishDepth;
        }

        //�������÷�GC
        private List<IEvent> Events = new List<IEvent>();

        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            Handlers<T>.actions.Add(handler);
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
                throw new InvalidOperationException($"�ݹ鴥���¼� {typeof(T).Name} ����ֹ");
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

        /// <summary>
        /// ע��ȫ���¼�
        /// </summary>
        public static void RegisterEvents()
        {
            if (Instance.Events.Count > 0)
            {
                ArchLog.Debug("�¼��Ѿ�ע��");
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
                ArchLog.Error(e.Message);
                throw;
            }
            finally
            {
                ListPool<Type>.Release(aEvents);
            }
        }
    }
}