using System.Collections.Generic;

namespace Arch.Tools.Pool
{
	public class DictionaryPool<Key, Value>
	{
		private static readonly HashSet<Dictionary<Key, Value>> m_SetUsed = new HashSet<Dictionary<Key, Value>>();
		private static readonly Stack<Dictionary<Key, Value>> m_StackFree = new Stack<Dictionary<Key, Value>>();

		public static Dictionary<Key, Value> Get()
		{
			Dictionary<Key, Value> dictionary;
			dictionary = m_StackFree.Count > 0
				? m_StackFree.Pop()
				: new Dictionary<Key, Value>();
			m_SetUsed.Add(dictionary);
			return dictionary;
		}

		public static void Release(Dictionary<Key, Value> dictionary)
		{
			if (dictionary == null)
			{
				throw new System.NullReferenceException("Dictionary is null.");
			}
			if (!m_SetUsed.Contains(dictionary))
			{
				throw new System.Exception("Dictionary is not in used set.");
			}
			m_SetUsed.Remove(dictionary);
			m_StackFree.Push(dictionary);
			dictionary.Clear();

		}

		public static void Clear()
		{
			m_SetUsed.Clear();
			m_StackFree.Clear();
		}
	}
}