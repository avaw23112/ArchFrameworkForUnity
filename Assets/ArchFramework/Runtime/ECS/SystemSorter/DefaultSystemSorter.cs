using System.Collections.Generic;

namespace Arch
{
	public class DefaultSystemSorter : ISystemSorter
	{
		public void Sort<T>(List<T> systems)
		{
			Sorter.SortSystems(systems);
		}
	}
}