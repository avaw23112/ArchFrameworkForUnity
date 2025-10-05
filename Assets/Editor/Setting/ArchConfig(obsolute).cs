#if UNITY_EDITOR

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Arch.Editor
{
	// 配置数据类，存储所有需要保存的配置
	[CreateAssetMenu(fileName = "ConfigData", menuName = "ArchFramework/ConfigData")]
	public class ConfigData : ScriptableObject
	{
		[Header("补充元数据集输出目录")]
		public string metaDllOutputPath = "";

		[Header("热更新程序集输出目录")]
		public string hotUpdateDllOutputPath = "";

		[Header("=== 热重载核心路径配置 ===")]
		[Tooltip("热重载输出目录（存放编译后的DLL）")]
		public string hotReloadOutputPath = "";

		[Tooltip("热更源码目录（存放需要热更的C#脚本）")]
		public List<string> hotfixSourceDirectories = new List<string>();

		[Header("=== 引用程序集配置 ===")]
		[Tooltip("需要手动引用的外部DLL路径（如第三方库、自定义模块）")]
		public List<string> referenceAssemblyPaths = new List<string>();

		[Tooltip("编译器查找依赖的目录（自动解析DLL依赖）")]
		public List<string> referenceResolvePaths = new List<string>()
		{
			Path.GetDirectoryName(typeof(object).Assembly.Location)
		};

		[Header("=== 源生成器配置 ===")]
		[Tooltip("源生成器DLL路径（编译时自动生成代码的模块）")]
		public List<string> sourceGeneratorPaths = new List<string>();
	}

	// 全局静态配置管理类
	public static class ArchConfig
	{
		// 配置文件路径
		public static string ConfigPath => Path.Combine("Assets/Resources/Config", "ConfigData.asset");

		// 缓存的配置实例
		private static ConfigData _instance;

		// 全局访问点
		public static ConfigData Instance
		{
			get
			{
				if (_instance == null)
				{
					_instance = LoadConfig();
				}
				return _instance;
			}
		}

		// 加载配置
		public static ConfigData LoadConfig()
		{
			// 尝试从Resources加载
			var loaded = Resources.Load<ConfigData>("Config/ConfigData");

			// 如果不存在则创建新的配置
			if (loaded == null)
			{
				return CreateNewConfig();
			}

			return loaded;
		}

		// 创建新的配置
		public static ConfigData CreateNewConfig()
		{
			// 创建配置实例
			var newConfig = ScriptableObject.CreateInstance<ConfigData>();

			// 确保目录存在
			var directory = Path.GetDirectoryName(ConfigPath);
			if (!Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

			// 保存配置到资产数据库
#if UNITY_EDITOR
			UnityEditor.AssetDatabase.CreateAsset(newConfig, ConfigPath);
			UnityEditor.AssetDatabase.SaveAssets();
			UnityEditor.AssetDatabase.Refresh();
#endif

			_instance = newConfig;
			return newConfig;
		}

		// 保存配置
		public static void SaveConfig(ConfigData data)
		{
			if (data == null) return;

			_instance = data;

#if UNITY_EDITOR
			UnityEditor.EditorUtility.SetDirty(data);
			UnityEditor.AssetDatabase.SaveAssets();
#endif
		}

		// 编辑器模式下重置配置
		public static void ResetConfig()
		{
#if UNITY_EDITOR
			if (File.Exists(ConfigPath))
			{
				UnityEditor.AssetDatabase.DeleteAsset(ConfigPath);
				UnityEditor.AssetDatabase.Refresh();
			}

			_instance = CreateNewConfig();
#endif
		}
	}
}

#endif