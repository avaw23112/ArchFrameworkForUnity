using System;


namespace Arch
{
	public static class PlayerLoopExtensions
	{ // 链式批量插入API
		public static T[] InsertAfter<T>(this T[] source, int index, T newItem)
		{
			if (index < 0 || index >= source.Length)
				throw new IndexOutOfRangeException();

			T[] newArray = new T[source.Length + 1];

			// 复制插入点之前的元素
			Array.Copy(source, 0, newArray, 0, index + 1);

			// 插入新元素
			newArray[index + 1] = newItem;

			// 复制插入点之后的元素
			Array.Copy(source, index + 1, newArray, index + 2, source.Length - index - 1);

			return newArray;
		}
	}
}