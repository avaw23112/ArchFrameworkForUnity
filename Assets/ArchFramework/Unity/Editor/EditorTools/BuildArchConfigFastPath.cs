#if UNITY_EDITOR

using Arch.Editor;
using Arch.Tools;
using UnityEditor;
using UnityEngine;

namespace Arch.Compilation.Editor
{
	/// <summary>
	/// 对外统一接口（菜单 & API）。
	/// </summary>
	public static class ArchBuild
	{
		public static ArchBuildConfig Config => ArchBuildConfig.LoadOrCreate();

		[MenuItem("Tools/独立编译 (Isolated) _F6")]
		public static bool CompileIsolated()
		{
			var ok = AssemblyBuilderPipeline.BuildIsolated(Config);
			EditorUtility.DisplayDialog("独立编译", ok ? "成功" : "失败", "OK");
			return ok;
		}

		[MenuItem("Tools/全联编 (FullLink) _F7")]
		public static bool CompileFullLink()
		{
			var ok = AssemblyBuilderPipeline.BuildFullLink(Config);
			EditorUtility.DisplayDialog("全联编", ok ? "成功" : "失败", "OK");
			return ok;
		}

		/// <summary>
		/// 一键热重载：编译首个 Isolated -> 加载 -> 调用 HotReloader。
		/// </summary>
		[MenuItem("Tools/热重载（编译+装载） _F4")]
		public static async void HotReload()
		{
			var cfg = ArchBuildConfig.LoadOrCreate();
			if (cfg == null || cfg.buildSetting.hotReloadAssemblies == null || cfg.buildSetting.hotReloadAssemblies.Count == 0)
			{
				EditorUtility.DisplayDialog("热重载", "未选择任何热重载程序集。", "OK");
				return;
			}

			foreach (var asmName in cfg.buildSetting.hotReloadAssemblies)
			{
				var iso = cfg.buildSetting.isolated.Find(i => i.assemblyName == asmName);
				if (iso == null)
				{
					Debug.LogWarning($"未找到 Isolated 配置项：{asmName}");
					continue;
				}

				ArchLog.LogInfo($"[HotReload] 编译并加载程序集：{asmName}");

				// 使用已有的 AssemblyBuilderPipeline 编译该程序集
				if (!AssemblyBuilderPipeline.BuildOne(iso))
				{
					Debug.LogError($"[HotReload] {asmName} 编译失败。");
					continue;
				}

				// 调用 HotReloader 加载
				HotReloader.LoadHotReloadAssembly(iso);
			}
			await HotReloader.HotReload();
			EditorUtility.DisplayDialog("热重载完成", "所有选中程序集已重新编译并加载。", "OK");
		}
	}
}

#endif