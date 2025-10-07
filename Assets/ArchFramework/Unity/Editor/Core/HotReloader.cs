#if UNITY_EDITOR

using Arch.Compilation.Editor;
using Arch.Tools;
using Cysharp.Threading.Tasks;
using Events;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;

namespace Arch.Editor
{
	public static class HotReloader
	{
		private static bool isReloading = false;
		private static List<Assembly> assemblies = new List<Assembly>();

		public static void LoadHotReloadAssembly(IsolatedAssembly iso)
		{
			string dllPath = Path.Combine(iso.outputDir, $"{iso.assemblyName}.dll");
			if (!File.Exists(dllPath))
			{
				ArchLog.LogError($"[HotReload] 未找到 {dllPath}");
				return;
			}

			var bytes = File.ReadAllBytes(dllPath);
			var asm = Assembly.Load(bytes);
			assemblies.Add(asm);
			ArchLog.LogInfo($"[HotReload] 已加载程序集：{iso.assemblyName}");
		}

		public static async UniTask HotReload()
		{
			if (!EditorApplication.isPlaying)
			{
				ArchLog.LogWarning("热重载仅支持在「编辑模式运行」时使用！");
				return;
			}
			if (isReloading)
			{
				ArchLog.LogWarning("当前已在热重载中，请勿重复触发！");
				return;
			}

			await UniTask.SwitchToMainThread();
			foreach (var assembly in assemblies)
				Assemblys.Register(assembly);
			HotReloadInternal();
			assemblies.Clear();
		}

		private static void HotReloadInternal()
		{
			//注册事件总线
			EventBus.RegisterEvents();

			//调度特性处理系统
			Attributes.Attributes.RemoveMapping();
			Attributes.Collector.CollectBaseAttributes();
			Attributes.Attributes.RegisterHotReloadableAttributeSystems();

			//注册所有被标注[System]的系统
			ArchSystems.ReloadArchSystem();

			//重新订阅ReactiveSystem的事件
			ArchSystems.SubcribeEntityAwake();
			ArchSystems.SubcribeEntityDestroy();

			ArchLog.LogInfo("热重载执行完成！");
		}
	}
}

#endif