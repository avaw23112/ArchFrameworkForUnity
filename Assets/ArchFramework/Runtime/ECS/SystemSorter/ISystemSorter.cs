using System.Collections.Generic;

namespace Arch
{
	public interface ISystemSorter
	{
		/// <summary>
		/// 对系统进行排序。
		/// </summary>
		/// <typeparam name="T">系统类型接口（如 IUpdate、IPureAwake 等）</typeparam>
		/// <param name="systems">要排序的系统列表</param>
		/// <returns>排序后的系统列表</returns>
		void Sort<T>(List<T> systems);
	}
}