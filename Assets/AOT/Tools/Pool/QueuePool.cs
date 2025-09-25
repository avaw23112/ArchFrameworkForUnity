using System.Collections.Generic;

namespace Arch.Tools.Pool
{
	public class QueuePool<T>
	{
		private static readonly HashSet<Queue<T>> m_SetUsed = new HashSet<Queue<T>>();
		private static readonly Stack<Queue<T>> m_StackFree = new Stack<Queue<T>>();

		public static Queue<T> Get()
		{
			Queue<T> Queue;
			Queue = m_StackFree.Count > 0
				? m_StackFree.Pop()
				: new Queue<T>();
			m_SetUsed.Add(Queue);
			return Queue;
		}

		public static void Release(Queue<T> Queue)
		{
			if (Queue == null)
			{
				ArchLog.LogError("Queue is null.");
				throw new System.NullReferenceException("Queue is null.");
			}
			if (!m_SetUsed.Contains(Queue))
			{
				ArchLog.LogError("Queue is not in used set.");
				throw new System.Exception("Queue is not in used set.");
			}
			m_SetUsed.Remove(Queue);
			m_StackFree.Push(Queue);
			Queue.Clear();
		}

		public static void Clear()
		{
			m_SetUsed.Clear();
			m_StackFree.Clear();
		}
	}
}