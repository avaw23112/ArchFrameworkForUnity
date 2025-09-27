// ConfigSettingsProvider.cs (扩展UI部分)
#if UNITY_EDITOR
using Arch.Editor;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class ConfigSettingsProvider : SettingsProvider
{
	private ConfigData configData;
	private Vector2 scrollPosition;

	// 临时输入字段（对应新增配置）
	private string newAssemblyPath = "";
	private string newResolvePath = "";
	private string newSourceGeneratorPath = "";
	private string newHotfixSourcePath = "";

	public ConfigSettingsProvider(string path, SettingsScope scope = SettingsScope.Project)
		: base(path, scope) { }

	public override void OnActivate(string searchContext, VisualElement rootElement)
	{
		configData = ArchConfig.LoadConfig(); // 确保配置实例存在
		if (configData == null)
		{
			configData = ArchConfig.CreateNewConfig();
		}
	}

	public override void OnGUI(string searchContext)
	{
		scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
		EditorGUILayout.Space(10);

		DrawBuildSection();

		// 1. 热重载核心路径配置
		DrawHotReloadCoreSection();

		// 2. 引用程序集配置（新增）
		DrawReferenceAssemblySection();

		// 3. 引用解析路径配置（新增）
		DrawReferenceResolveSection();

		// 4. 源生成器配置（新增）
		DrawSourceGeneratorSection();

		EditorGUILayout.EndScrollView();
	}

	private void DrawBuildSection()
	{
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
	}

	#region 新增：引用程序集配置UI
	private void DrawReferenceAssemblySection()
	{
		GUILayout.Label("引用程序集配置", EditorStyles.boldLabel);
		EditorGUILayout.HelpBox("手动添加需要引用的外部DLL（如第三方库、自定义模块），支持.dll/.exe格式", MessageType.Info);

		// 输入+添加+浏览行
		EditorGUILayout.BeginHorizontal();
		newAssemblyPath = EditorGUILayout.TextField("程序集路径", newAssemblyPath);
		if (GUILayout.Button("添加", GUILayout.Width(60)))
		{
			AddPath(configData.referenceAssemblyPaths, newAssemblyPath, isFile: true);
			newAssemblyPath = "";
		}
		if (GUILayout.Button("浏览", GUILayout.Width(60)))
		{
			string selectedPath = EditorUtility.OpenFilePanel("选择引用程序集", Application.dataPath, "dll;exe");
			AddPath(configData.referenceAssemblyPaths, selectedPath, isFile: true);
		}
		EditorGUILayout.EndHorizontal();

		// 显示已添加的程序集列表+删除功能
		ShowPathList(configData.referenceAssemblyPaths,
			(index) => configData.referenceAssemblyPaths.RemoveAt(index));

		EditorGUILayout.Space(15);
	}
	#endregion

	#region 新增：引用解析路径配置UI
	private void DrawReferenceResolveSection()
	{
		GUILayout.Label("引用解析路径配置", EditorStyles.boldLabel);
		EditorGUILayout.HelpBox("编译器自动查找依赖DLL的目录（如Unity引擎DLL目录、NuGet包目录）", MessageType.Info);

		// 输入+添加+浏览行
		EditorGUILayout.BeginHorizontal();
		newResolvePath = EditorGUILayout.TextField("解析目录", newResolvePath);
		if (GUILayout.Button("添加", GUILayout.Width(60)))
		{
			AddPath(configData.referenceResolvePaths, newResolvePath, isFile: false);
			newResolvePath = "";
		}
		if (GUILayout.Button("浏览", GUILayout.Width(60)))
		{
			string selectedPath = EditorUtility.OpenFolderPanel("选择解析目录", Application.dataPath, "");
			AddPath(configData.referenceResolvePaths, selectedPath, isFile: false);
		}
		EditorGUILayout.EndHorizontal();

		// 显示已添加的解析目录列表+删除功能
		ShowPathList(configData.referenceResolvePaths,
			(index) => configData.referenceResolvePaths.RemoveAt(index));

		EditorGUILayout.Space(15);
	}
	#endregion

	#region 新增：源生成器配置UI
	private void DrawSourceGeneratorSection()
	{
		GUILayout.Label("源生成器配置", EditorStyles.boldLabel);
		EditorGUILayout.HelpBox("添加源生成器DLL（编译时自动生成代码，需实现ISourceGenerator/IIncrementalGenerator）", MessageType.Info);

		// 输入+添加+浏览行
		EditorGUILayout.BeginHorizontal();
		newSourceGeneratorPath = EditorGUILayout.TextField("生成器DLL路径", newSourceGeneratorPath);
		if (GUILayout.Button("添加", GUILayout.Width(60)))
		{
			AddPath(configData.sourceGeneratorPaths, newSourceGeneratorPath, isFile: true);
			newSourceGeneratorPath = "";
		}
		if (GUILayout.Button("浏览", GUILayout.Width(60)))
		{
			string selectedPath = EditorUtility.OpenFilePanel("选择源生成器DLL", Application.dataPath, "dll");
			AddPath(configData.sourceGeneratorPaths, selectedPath, isFile: true);
		}
		EditorGUILayout.EndHorizontal();

		// 显示已添加的源生成器列表+删除功能
		ShowPathList(configData.sourceGeneratorPaths,
			(index) => configData.sourceGeneratorPaths.RemoveAt(index));

		EditorGUILayout.Space(15);
	}
	#endregion

	#region 原有：热重载核心路径UI（保留并优化）
	private void DrawHotReloadCoreSection()
	{
		GUILayout.Label("热重载核心配置", EditorStyles.boldLabel);

		// 热重载输出目录
		EditorGUILayout.BeginHorizontal();
		configData.hotReloadOutputPath = EditorGUILayout.TextField("热重载输出目录",
			string.IsNullOrEmpty(configData.hotReloadOutputPath) ?
				Path.Combine(Application.dataPath, "..", "HotfixOutput") :
				configData.hotReloadOutputPath);
		if (GUILayout.Button("浏览", GUILayout.Width(60)))
		{
			string selectedPath = EditorUtility.OpenFolderPanel("选择热重载输出目录", Application.dataPath, "");
			if (!string.IsNullOrEmpty(selectedPath))
				configData.hotReloadOutputPath = selectedPath;
		}
		EditorGUILayout.EndHorizontal();

		// 热更源码目录
		EditorGUILayout.BeginHorizontal();
		newHotfixSourcePath = EditorGUILayout.TextField("热更源码目录", newHotfixSourcePath);
		if (GUILayout.Button("添加", GUILayout.Width(60)))
		{
			AddPath(configData.hotfixSourceDirectories, newHotfixSourcePath, isFile: false);
			newHotfixSourcePath = "";
		}
		if (GUILayout.Button("浏览", GUILayout.Width(60)))
		{
			string selectedPath = EditorUtility.OpenFolderPanel("选择热更源码目录", Application.dataPath, "");
			AddPath(configData.hotfixSourceDirectories, selectedPath, isFile: false);
		}
		EditorGUILayout.EndHorizontal();
		ShowPathList(configData.hotfixSourceDirectories,
			(index) => configData.hotfixSourceDirectories.RemoveAt(index));

		EditorGUILayout.Space(15);
	}
	#endregion


	#region 通用工具方法（参考脚本逻辑）
	/// <summary>
	/// 添加路径到列表（含合法性校验）
	/// </summary>
	private void AddPath(List<string> pathList, string path, bool isFile)
	{
		// 空路径校验
		if (string.IsNullOrEmpty(path))
		{
			EditorUtility.DisplayDialog("错误", "路径不能为空！", "确定");
			return;
		}

		// 存在性校验（文件/目录区分）
		bool pathExists = isFile ? File.Exists(path) : Directory.Exists(path);
		if (!pathExists)
		{
			EditorUtility.DisplayDialog("错误", $"路径不存在：{path}", "确定");
			return;
		}

		// 重复添加校验
		if (pathList.Contains(path))
		{
			EditorUtility.DisplayDialog("提示", "该路径已添加，无需重复添加！", "确定");
			return;
		}

		// 添加并保存
		pathList.Add(path);
		ArchConfig.SaveConfig(configData);
		EditorUtility.SetDirty(configData);
		AssetDatabase.SaveAssets();
	}

	/// <summary>
	/// 显示路径列表（含删除功能）
	/// </summary>
	private void ShowPathList(List<string> pathList, System.Action<int> onRemove)
	{
		if (pathList.Count == 0)
		{
			EditorGUILayout.HelpBox("尚未添加任何路径", MessageType.Info);
			return;
		}

		// 循环显示每个路径+删除按钮
		foreach (var i in System.Linq.Enumerable.Range(0, pathList.Count))
		{
			EditorGUILayout.BeginHorizontal();
			// 路径显示（超出部分自动省略）
			EditorGUILayout.TextField(pathList[i], GUILayout.ExpandWidth(true));
			// 删除按钮
			if (GUILayout.Button("删除", GUILayout.Width(60)))
			{
				onRemove?.Invoke(i);
				// 删除后保存配置
				ArchConfig.SaveConfig(configData);
				EditorUtility.SetDirty(configData);
				AssetDatabase.SaveAssets();
				break; // 避免循环索引异常
			}
			EditorGUILayout.EndHorizontal();
		}
	}
	#endregion

	#region 配置入口（添加到Project Settings）
	[SettingsProvider]
	public static SettingsProvider CreateHotReloadSettingsProvider()
	{
		// 配置入口：Edit > Project Settings > Hot Reload Settings
		return new ConfigSettingsProvider("Project/Arch Framework", SettingsScope.Project);
	}
	#endregion
}
#endif