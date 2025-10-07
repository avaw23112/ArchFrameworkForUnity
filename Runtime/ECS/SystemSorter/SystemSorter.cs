using System.Collections.Generic;

namespace Arch
{
	/// <summary>
	/// 系统排序门面。持有一个可替换的排序策略。
	/// </summary>
	public static class SystemSorter
	{
		private static ISystemSorter _sorter = new DefaultSystemSorter();

		/// <summary>
		/// 设置外部自定义排序策略。
		/// </summary>
		public static void SetSorter(ISystemSorter sorter)
		{
			_sorter = sorter ?? new DefaultSystemSorter();
		}

		/// <summary>
		/// 对系统进行排序（门面调用）。
		/// </summary>
		public static void SortSystems<T>(List<T> systems)
		{
			_sorter.Sort(systems);
		}
	}
}