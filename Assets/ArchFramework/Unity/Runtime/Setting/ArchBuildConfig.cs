using Arch.Runtime;
using Arch.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Arch
{
	[Serializable]
	public class IsolatedAssembly
	{
		public string assemblyName = "Hotfix";
		public string outputDir = "";
		public List<string> sourceDirs = new();
		public List<string> additionalDefines = new();
		public List<string> additionalReferences = new();
		public bool useEngineModules = true;
		public bool editorAssembly = false;
	}

	[Serializable]
	public class FullLinkAssembly
	{
		public string assemblyName = "Game.FullLink";
		public string outputDir = "";
		public List<string> sourceDirs = new();
		public List<string> additionalDefines = new();
		public List<string> additionalReferences = new();
		public bool useEngineModules = true;
		public bool editorAssembly = false;
	}

	[Serializable]
	public class BuildSetting
	{
		[Header("元数据程序集路径")]
		public string MetaDllPath = "";

		[Header("热更新程序集路径")]
		public string HotFixDllPath = "";

		[Header("当前编译模式")]
		public AssemblyBuildMode buildMode = AssemblyBuildMode.Isolated;

		[Header("热重载程序集列表（按顺序加载）")]
		public List<string> hotReloadAssemblies = new();

		[Header("独立编译条目（用于 Hotfix/模块化 DLL）")]
		public List<IsolatedAssembly> isolated = new();

		[Header("全联编配置（把多个源码聚合为一个 DLL）")]
		public FullLinkAssembly fullLink = new FullLinkAssembly();

		public enum AssemblyBuildMode
		{
			Isolated,
			FullLink
		}
	}

	[Serializable]
	public class CompilePipeLineSetting
	{
		[Header("编译前处理设置")]
		public List<string> preBuildProcessors = new();

		[Header("后处理设置")]
		public List<string> postProcessors = new(); // 按顺序执行

		[Header("全局后处理设置")]
		public List<string> globalPostProcessors = new();

		[Header("后处理导出设置")]
		public string postExportDir = "Assets/BuildOutput";

		[Header("后处理导出后缀")]
		public string postExportSuffix = "hotfix";

		[Header("代码编织器的路径")]
		public List<string> weaverPaths = new();
	}

	[Serializable]
	public class SystemBuildSetting
	{
		[Header("PureAwake 系统列表")]
		public List<string> pureAwakeSystems = new();

		[Header("ReactiveAwake 系统列表")]
		public List<string> reactiveAwakeSystems = new();

		[Header("Update 系统列表")]
		public List<string> updateSystems = new();

		[Header("LateUpdate 系统列表")]
		public List<string> lateUpdateSystems = new();

		[Header("PureDestroy 系统列表")]
		public List<string> pureDestroySystems = new();

		[Header("ReactiveDestroy 系统列表")]
		public List<string> reactiveDestroySystems = new();
	}

	/// <summary>
	/// 统一配置（仅保留 AssemblyBuilder 所需字段）。
	/// </summary>
	[CreateAssetMenu(fileName = "ArchBuildConfig", menuName = "Arch/ArchBuildConfig")]
	public class ArchBuildConfig : ScriptableObject
	{
		[Header("编译管线设置")]
		public CompilePipeLineSetting compilePipeLineSetting = new CompilePipeLineSetting();

		[Header("构建设置")]
		public BuildSetting buildSetting = new BuildSetting();

		[Header("System 构建设置")]
		public SystemBuildSetting systemBuildSetting = new SystemBuildSetting();

		public static ArchBuildConfig LoadOrCreate()
		{
			AsyncOperationHandle<ArchBuildConfig> handle = default;
			ArchBuildConfig cfg = null;

#if UNITY_EDITOR
			if (!File.Exists(GameRoot.Setting.ArchSettingPath))
			{
				System.IO.Directory.CreateDirectory(GameRoot.Setting.SettingPath);
				cfg = ScriptableObject.CreateInstance<ArchBuildConfig>();
				UnityEditor.AssetDatabase.CreateAsset(cfg, GameRoot.Setting.ArchSettingPath);
				UnityEditor.AssetDatabase.SaveAssets();
				UnityEditor.AssetDatabase.Refresh();
			}
			else
			{
				cfg = AssetDatabase.LoadAssetAtPath<ArchBuildConfig>(GameRoot.Setting.ArchSettingPath);
			}
			return cfg;
#endif
			try
			{
				handle = Addressables.LoadAssetAsync<ArchBuildConfig>(GameRoot.Setting.ArchSettingPath);
				handle.WaitForCompletion();
				if (handle.Status == AsyncOperationStatus.Failed)
				{
					ArchLog.LogError("Load archBuildConfig failed!");
					return null;
				}
				cfg = handle.Result;
				//避免查找到address但类型转换失效的情况
				if (cfg == null)
				{
					ArchLog.LogError("Load archBuildConfig failed!");
					return null;
				}
				return cfg;
			}
			catch
			{
				ArchLog.LogError("Load archBuildConfig failed!");
				throw;
			}
		}
	}
}