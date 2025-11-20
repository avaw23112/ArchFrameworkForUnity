using System.Collections.Generic;
using System.Linq;

namespace Arch
{
	/// <summary>
	/// 默认系统排序器，根据 ArchBuildConfig 的配置进行排序。
	/// </summary>
	public class UnitySystemSorter : ISystemSorter
	{
		public void Sort<T>(List<T> systems)
		{
			if (systems == null || systems.Count <= 1)
				return;

			var cfg = ArchBuildConfig.LoadOrCreate();
			if (cfg == null)
				return;

			var orderList = GetOrderListForType<T>(cfg.systemBuildSetting);
			if (orderList == null || orderList.Count == 0)
				return;

			// ⚙️ 构建系统名→实例映射（使用 FullName 确保唯一）
			var map = new Dictionary<string, T>();
			foreach (var sys in systems)
			{
				string name = sys.GetType().FullName;
				if (!map.ContainsKey(name))
					map[name] = sys;
			}

			// ⚙️ 构建新的排序结果列表
			var sorted = new List<T>(systems.Count);

			// 1️⃣ 按配置顺序添加
			foreach (var name in orderList)
			{
				if (map.TryGetValue(name, out var sys))
					sorted.Add(sys);
			}

			// 2️⃣ 追加未出现在配置中的系统（保持原顺序）
			foreach (var sys in systems)
			{
				if (!sorted.Contains(sys))
					sorted.Add(sys);
			}

			// ⚙️ 原地覆盖（确保系统列表本身的顺序被修改）
			systems.Clear();
			systems.AddRange(sorted);
		}

		private List<string> GetOrderListForType<T>(SystemBuildSetting setting)
		{
			if (typeof(IPureAwake).IsAssignableFrom(typeof(T))) return setting.pureAwakeSystems;
			if (typeof(IReactiveAwake).IsAssignableFrom(typeof(T))) return setting.reactiveAwakeSystems;
			if (typeof(IUpdate).IsAssignableFrom(typeof(T))) return setting.updateSystems;
			if (typeof(ILateUpdate).IsAssignableFrom(typeof(T))) return setting.lateUpdateSystems;
			if (typeof(IPureDestroy).IsAssignableFrom(typeof(T))) return setting.pureDestroySystems;
			if (typeof(IReactiveDestroy).IsAssignableFrom(typeof(T))) return setting.reactiveDestroySystems;
			return null;
		}
	}
}