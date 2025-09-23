using System.Collections.Generic;

namespace Arch.Tools.Pool
{
	public class ListPool<T>
	{
		private static readonly HashSet<List<T>> m_SetUsed = new HashSet<List<T>>();
		private static readonly Stack<List<T>> m_StackFree = new Stack<List<T>>();

		public static List<T> Get()
		{
			List<T> List;
			List = m_StackFree.Count > 0
				? m_StackFree.Pop()
				: new List<T>();
			m_SetUsed.Add(List);
			return List;
		}

		public static void Release(List<T> List)
		{
			if (List == null)
			{
				ArchLog.Error("List is null.");
				throw new System.NullReferenceException("List is null.");
			}
			if (!m_SetUsed.Contains(List))
			{
				ArchLog.Error("List is not in used set.");
				throw new System.Exception("List is not in used set.");
			}
			m_SetUsed.Remove(List);
			m_StackFree.Push(List);
			List.Clear();
		}

		public static void Clear()
		{
			m_SetUsed.Clear();
			m_StackFree.Clear();
		}
	}
}