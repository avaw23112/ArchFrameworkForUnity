using System;
using System.Collections.Generic;
using Tools;

namespace Events
{
    internal class MessagePipe : Singleton<MessagePipe>
    {
        private Dictionary<Type, object> m_dicTypeStores = new Dictionary<Type, object>();

        public void Push<T>(T value) where T : struct
        {
            if (!m_dicTypeStores.TryGetValue(typeof(T), out var store))
            {
                store = new Queue<T>();
                m_dicTypeStores[typeof(T)] = store;
            }
            ((Queue<T>)store).Enqueue(value);
        }

        public Queue<T> OutPipe<T>() where T : struct
        {
            if (!m_dicTypeStores.TryGetValue(typeof(T), out var store))
            {
                store = new Queue<T>();
                m_dicTypeStores[typeof(T)] = store;
            }
            return (m_dicTypeStores[typeof(T)]) as Queue<T>; // 仅类型转换，无拆箱
        }
    }
}