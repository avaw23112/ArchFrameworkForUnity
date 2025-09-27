#if UNITY_EDITOR
using Arch.Editor;
using System;
using System.IO;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

// 配置面板实现，显示在Project Settings中
namespace ConfigFramework.Editor
{
	public class ConfigSettingsProvider : SettingsProvider
	{
		// 配置数据的引用
		private ConfigData configData;
		// 滚动位置
		private Vector2 scrollPosition;
		// 构造函数
		public ConfigSettingsProvider(string path, SettingsScope scope = SettingsScope.Project)
			: base(path, scope) { }

		// 创建配置面板实例
		[SettingsProvider]
		public static SettingsProvider CreateConfigSettingsProvider()
		{
			// 注册配置面板到Project Settings
			var provider = new ConfigSettingsProvider("Project/Arch Framework", SettingsScope.Project);

			// 确保配置数据已加载
			provider.configData = ArchConfig.LoadConfig();

			return provider;
		}

		// 绘制配置面板UI
		public override void OnGUI(string searchContext)
		{
			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

			// 绘制配置标题
			EditorGUILayout.LabelField("应用程序配置", EditorStyles.boldLabel);
			EditorGUILayout.Space();

			// 检查配置数据是否存在
			if (configData == null)
			{
				EditorGUILayout.HelpBox("配置数据加载失败，请重新创建配置", MessageType.Error);
				if (GUILayout.Button("重新创建配置"))
				{
					configData = ArchConfig.CreateNewConfig();
				}
				EditorGUILayout.EndScrollView();
				return;
			}

			// 使用EditorGUI.BeginChangeCheck检测配置是否更改
			EditorGUI.BeginChangeCheck();

			// 补充元数据集输出目录
			configData.metaDllOutputPath = EditorGUILayout.TextField(
				"补充元数据集输出目录",
				string.IsNullOrEmpty(configData.metaDllOutputPath) ?
					Path.Combine(Application.dataPath, "HybridCLRGenerate", "MetaDataDll") :
					configData.metaDllOutputPath
			);

			// 热更新程序集输出目录
			configData.hotUpdateDllOutputPath = EditorGUILayout.TextField(
				"热更新程序集输出目录",
				string.IsNullOrEmpty(configData.hotUpdateDllOutputPath) ?
					Path.Combine(Application.dataPath, "HybridCLRGenerate", "HotFixDll") :
					configData.hotUpdateDllOutputPath
			);

			// 热重载输出目录
			configData.hotReloadOutputPath = EditorGUILayout.TextField(
				"热重载输出目录",
				string.IsNullOrEmpty(configData.hotReloadOutputPath) ?
					Path.Combine(Application.dataPath, "..", "HotfixOutput") :
					configData.hotReloadOutputPath
			);

			// 热更源码目录配置
			EditorGUILayout.LabelField("热更源码目录", EditorStyles.boldLabel);
			for (int i = 0; i < configData.hotfixSourceDirectories.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				configData.hotfixSourceDirectories[i] = EditorGUILayout.TextField(configData.hotfixSourceDirectories[i]);
				if (GUILayout.Button("移除", GUILayout.Width(60)))
				{
					configData.hotfixSourceDirectories.RemoveAt(i);
					i--;
				}
				EditorGUILayout.EndHorizontal();
			}

			if (GUILayout.Button("添加热更源码目录", GUILayout.Width(150)))
			{
				string path = EditorUtility.OpenFolderPanel("选择热更源码目录", Application.dataPath, "");
				if (!string.IsNullOrEmpty(path) && !configData.hotfixSourceDirectories.Contains(path))
				{
					configData.hotfixSourceDirectories.Add(path);
				}
			}

			// 源生成器集配置
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("源生成器集", EditorStyles.boldLabel);
			for (int i = 0; i < configData.m_sourceGeneratorList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				configData.m_sourceGeneratorList[i] = EditorGUILayout.TextField(configData.m_sourceGeneratorList[i]);
				if (GUILayout.Button("移除", GUILayout.Width(60)))
				{
					configData.m_sourceGeneratorList.RemoveAt(i);
					i--;
				}
				EditorGUILayout.EndHorizontal();
			}

			if (GUILayout.Button("添加源生成器", GUILayout.Width(120)))
			{
				configData.m_sourceGeneratorList.Add("");
			}

			// 如果配置有更改，保存配置
			if (EditorGUI.EndChangeCheck())
			{
				ArchConfig.SaveConfig(configData);
				EditorUtility.SetDirty(configData);
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("配置文件位置: " + ArchConfig.ConfigPath, EditorStyles.miniLabel);

			EditorGUILayout.EndScrollView();
		}
	}
}
#endif