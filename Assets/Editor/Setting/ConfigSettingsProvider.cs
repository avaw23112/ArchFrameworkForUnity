#if UNITY_EDITOR
using Arch.Editor;
using Arch.Net;
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

	// Network settings (ScriptableObject: Resources/NetworkConfig.asset)
	private Arch.Net.NetworkConfig netConfig;
	private SerializedObject netConfigSO;

	// Temp inputs
	private string newAssemblyPath = string.Empty;
	private string newResolvePath = string.Empty;
	private string newSourceGeneratorPath = string.Empty;
	private string newHotfixSourcePath = string.Empty;

	public ConfigSettingsProvider(string path, SettingsScope scope = SettingsScope.Project)
		: base(path, scope) { }

	public override void OnActivate(string searchContext, VisualElement rootElement)
	{
		configData = ArchConfig.LoadConfig();
		if (configData == null)
		{
			configData = ArchConfig.CreateNewConfig();
		}
		EnsureNetworkConfigAsset();
	}

	public override void OnGUI(string searchContext)
	{
		scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
		EditorGUILayout.Space(8);

		DrawNetworkSettingsSection();
		EditorGUILayout.Space(12);

		DrawBuildSection();
		EditorGUILayout.Space(8);

		DrawHotReloadCoreSection();
		EditorGUILayout.Space(8);

		DrawReferenceAssemblySection();
		EditorGUILayout.Space(8);

		DrawReferenceResolveSection();
		EditorGUILayout.Space(8);

		DrawSourceGeneratorSection();

		EditorGUILayout.EndScrollView();
	}

    private void DrawNetworkSettingsSection()
    {
        GUILayout.Label("网络设置（Arch.Net）", EditorStyles.boldLabel);
        if (netConfig == null)
        {
            EditorGUILayout.HelpBox("未找到 NetworkConfig 资源。", MessageType.Warning);
            if (GUILayout.Button("创建/重新加载 NetworkConfig")) EnsureNetworkConfigAsset();
            return;
        }

        if (netConfigSO == null) netConfigSO = new SerializedObject(netConfig);
        netConfigSO.Update();
        EditorGUI.indentLevel++;

        // Driver
        EditorGUILayout.PropertyField(netConfigSO.FindProperty("m_szDefaultEndpoint"), new GUIContent("默认端点"));

        // Rates
        EditorGUILayout.LabelField("速率", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(netConfigSO.FindProperty("m_nCommandsPerFrame"), new GUIContent("每帧指令数"));
        EditorGUILayout.PropertyField(netConfigSO.FindProperty("m_nPacketsPerFrame"), new GUIContent("每帧数据包数"));
        EditorGUILayout.PropertyField(netConfigSO.FindProperty("m_nEntitiesPerPacket"), new GUIContent("每包实体数"));

        // Packet Flags
        EditorGUILayout.LabelField("数据包标志", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(netConfigSO.FindProperty("m_vRpcIncludeTimestamp"), new GUIContent("RPC 包含时间戳"));
        EditorGUILayout.PropertyField(netConfigSO.FindProperty("m_vSyncIncludeTimestamp"), new GUIContent("同步 包含时间戳"));
        EditorGUILayout.PropertyField(netConfigSO.FindProperty("m_vRpcIncludeChannel"), new GUIContent("RPC 包含通道"));
        EditorGUILayout.PropertyField(netConfigSO.FindProperty("m_vSyncIncludeChannel"), new GUIContent("同步 包含通道"));

        // Delivery (LiteNetLib)
        EditorGUILayout.LabelField("传输模式（LiteNetLib）", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(netConfigSO.FindProperty("m_vRpcDelivery"), new GUIContent("RPC 传输模式"));
        EditorGUILayout.PropertyField(netConfigSO.FindProperty("m_vSyncDelivery"), new GUIContent("同步 传输模式"));

        // Scan mode
        EditorGUILayout.LabelField("扫描模式", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(netConfigSO.FindProperty("m_vUseChunkScan"), new GUIContent("使用 Chunk 扫描"));

        // Compression
        EditorGUILayout.LabelField("压缩", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(netConfigSO.FindProperty("m_vEnableCompression"), new GUIContent("启用压缩"));
        EditorGUILayout.PropertyField(netConfigSO.FindProperty("m_nCompressThresholdBytes"), new GUIContent("压缩阈值（字节）"));

        // Sync Relay
        EditorGUILayout.LabelField("同步转发", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(netConfigSO.FindProperty("m_vEnableSyncRelay"), new GUIContent("启用同步转发"));
        EditorGUILayout.PropertyField(netConfigSO.FindProperty("m_nSyncRelayTtl"), new GUIContent("转发 TTL"));
        EditorGUILayout.PropertyField(netConfigSO.FindProperty("m_nSyncRelayDedupWindowFrames"), new GUIContent("去重窗口（帧）"));
        EditorGUILayout.PropertyField(netConfigSO.FindProperty("m_vEnableSyncRelayLog"), new GUIContent("启用转发日志"));
        EditorGUILayout.PropertyField(netConfigSO.FindProperty("m_nSyncRelayDedupCapacity"), new GUIContent("去重容量"));
        EditorGUILayout.PropertyField(netConfigSO.FindProperty("m_nSyncRelayLogSampleRate"), new GUIContent("转发日志采样率"));

        // Sync Apply Logging
        EditorGUILayout.LabelField("同步应用日志", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(netConfigSO.FindProperty("m_vEnableSyncApplyLog"), new GUIContent("启用同步应用日志"));
        EditorGUILayout.PropertyField(netConfigSO.FindProperty("m_nSyncApplyLogSampleRate"), new GUIContent("应用日志采样率"));

		EditorGUI.indentLevel--;
		if (netConfigSO.ApplyModifiedProperties())
		{
			EditorUtility.SetDirty(netConfig);
			AssetDatabase.SaveAssets();
		}
	}

    private void DrawBuildSection()
    {
        GUILayout.Label("构建输出", EditorStyles.boldLabel);
        configData.metaDllOutputPath = EditorGUILayout.TextField(
            "补充元数据集输出目录",
            string.IsNullOrEmpty(configData.metaDllOutputPath) ?
                Path.Combine(Application.dataPath, "HybridCLRGenerate", "MetaDataDll") :
                configData.metaDllOutputPath);

		configData.hotUpdateDllOutputPath = EditorGUILayout.TextField(
			"热更新程序集输出目录",
			string.IsNullOrEmpty(configData.hotUpdateDllOutputPath) ?
				Path.Combine(Application.dataPath, "HybridCLRGenerate", "HotFixDll") :
				configData.hotUpdateDllOutputPath);
	}

	private void DrawReferenceAssemblySection()
	{
		GUILayout.Label("引用程序集配置", EditorStyles.boldLabel);
		EditorGUILayout.HelpBox("手动添加需要引用的外部DLL（如第三方库、自定义模块），支持 .dll/.exe 格式", MessageType.Info);

		EditorGUILayout.BeginHorizontal();
		newAssemblyPath = EditorGUILayout.TextField("程序集路径", newAssemblyPath);
		if (GUILayout.Button("添加", GUILayout.Width(60)))
		{
			AddPath(configData.referenceAssemblyPaths, newAssemblyPath, isFile: true);
			newAssemblyPath = string.Empty;
		}
		if (GUILayout.Button("浏览", GUILayout.Width(60)))
		{
			string selectedPath = EditorUtility.OpenFilePanel("选择引用程序集", Application.dataPath, "dll");
			AddPath(configData.referenceAssemblyPaths, selectedPath, isFile: true);
		}
		EditorGUILayout.EndHorizontal();

		ShowPathList(configData.referenceAssemblyPaths, index => configData.referenceAssemblyPaths.RemoveAt(index));
	}

	private void DrawReferenceResolveSection()
	{
		GUILayout.Label("引用解析路径配置", EditorStyles.boldLabel);
		EditorGUILayout.HelpBox("编译器自动查找依赖DLL的目录（如 Unity 引擎DLL 目录、NuGet 包目录）", MessageType.Info);

		EditorGUILayout.BeginHorizontal();
		newResolvePath = EditorGUILayout.TextField("解析目录", newResolvePath);
		if (GUILayout.Button("添加", GUILayout.Width(60)))
		{
			AddPath(configData.referenceResolvePaths, newResolvePath, isFile: false);
			newResolvePath = string.Empty;
		}
		if (GUILayout.Button("浏览", GUILayout.Width(60)))
		{
			string selectedPath = EditorUtility.OpenFolderPanel("选择解析目录", Application.dataPath, "");
			AddPath(configData.referenceResolvePaths, selectedPath, isFile: false);
		}
		EditorGUILayout.EndHorizontal();

		ShowPathList(configData.referenceResolvePaths, index => configData.referenceResolvePaths.RemoveAt(index));
	}

	private void DrawSourceGeneratorSection()
	{
		GUILayout.Label("源生成器配置", EditorStyles.boldLabel);
		EditorGUILayout.HelpBox("添加 Roslyn Source Generators（可选）", MessageType.Info);

		EditorGUILayout.BeginHorizontal();
		newSourceGeneratorPath = EditorGUILayout.TextField("生成器路径", newSourceGeneratorPath);
		if (GUILayout.Button("添加", GUILayout.Width(60)))
		{
			AddPath(configData.sourceGeneratorPaths, newSourceGeneratorPath, isFile: true);
			newSourceGeneratorPath = string.Empty;
		}
		if (GUILayout.Button("浏览", GUILayout.Width(60)))
		{
			string selectedPath = EditorUtility.OpenFilePanel("选择生成器", Application.dataPath, "dll");
			AddPath(configData.sourceGeneratorPaths, selectedPath, isFile: true);
		}
		EditorGUILayout.EndHorizontal();

		ShowPathList(configData.sourceGeneratorPaths, index => configData.sourceGeneratorPaths.RemoveAt(index));
	}

	private void DrawHotReloadCoreSection()
	{
		GUILayout.Label("热重载核心配置", EditorStyles.boldLabel);

		EditorGUILayout.BeginHorizontal();
		configData.hotReloadOutputPath = EditorGUILayout.TextField("热重载输出目录",
			string.IsNullOrEmpty(configData.hotReloadOutputPath) ?
				Path.Combine(Application.dataPath, "..", "HotfixOutput") :
				configData.hotReloadOutputPath);
		if (GUILayout.Button("浏览", GUILayout.Width(60)))
		{
			string selectedPath = EditorUtility.OpenFolderPanel("选择热重载输出目录", Application.dataPath, "");
			if (!string.IsNullOrEmpty(selectedPath)) configData.hotReloadOutputPath = selectedPath;
		}
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();
		newHotfixSourcePath = EditorGUILayout.TextField("热更源码目录", newHotfixSourcePath);
		if (GUILayout.Button("添加", GUILayout.Width(60)))
		{
			AddPath(configData.hotfixSourceDirectories, newHotfixSourcePath, isFile: false);
			newHotfixSourcePath = string.Empty;
		}
		if (GUILayout.Button("浏览", GUILayout.Width(60)))
		{
			string selectedPath = EditorUtility.OpenFolderPanel("选择热更源码目录", Application.dataPath, "");
			AddPath(configData.hotfixSourceDirectories, selectedPath, isFile: false);
		}
		EditorGUILayout.EndHorizontal();

		ShowPathList(configData.hotfixSourceDirectories, index => configData.hotfixSourceDirectories.RemoveAt(index));
	}

	private void AddPath(List<string> pathList, string path, bool isFile)
	{
		if (pathList == null) return;
		if (string.IsNullOrEmpty(path))
		{
			EditorUtility.DisplayDialog("错误", "路径不能为空", "确定");
			return;
		}
		bool exists = isFile ? File.Exists(path) : Directory.Exists(path);
		if (!exists)
		{
			EditorUtility.DisplayDialog("错误", $"路径不存在：{path}", "确定");
			return;
		}
		if (pathList.Contains(path))
		{
			EditorUtility.DisplayDialog("提示", "该路径已添加，无需重复添加", "确定");
			return;
		}
		pathList.Add(path);
		ArchConfig.SaveConfig(configData);
		EditorUtility.SetDirty(configData);
		AssetDatabase.SaveAssets();
	}

	private void ShowPathList(List<string> pathList, Action<int> onRemove)
	{
		if (pathList == null || pathList.Count == 0)
		{
			EditorGUILayout.HelpBox("尚未添加任何路径", MessageType.Info);
			return;
		}
		for (int i = 0; i < pathList.Count; i++)
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.TextField(pathList[i], GUILayout.ExpandWidth(true));
			if (GUILayout.Button("删除", GUILayout.Width(60)))
			{
				onRemove?.Invoke(i);
				ArchConfig.SaveConfig(configData);
				EditorUtility.SetDirty(configData);
				AssetDatabase.SaveAssets();
				break;
			}
			EditorGUILayout.EndHorizontal();
		}
	}

	private void EnsureNetworkConfigAsset()
	{
		// Try find existing NetworkConfig asset
		netConfig = null;
		foreach (var guid in AssetDatabase.FindAssets("t:NetworkConfig"))
		{
			var path = AssetDatabase.GUIDToAssetPath(guid);
			var obj = AssetDatabase.LoadAssetAtPath<NetworkConfig>(path);
			if (obj != null) { netConfig = obj; break; }
		}
		if (netConfig == null)
		{
			if (!AssetDatabase.IsValidFolder("Assets/Resources"))
				AssetDatabase.CreateFolder("Assets", "Resources");
			netConfig = ScriptableObject.CreateInstance<NetworkConfig>();
			AssetDatabase.CreateAsset(netConfig, "Assets/Resources/NetworkConfig.asset");
			AssetDatabase.SaveAssets();
		}
		netConfigSO = new SerializedObject(netConfig);
	}

	[SettingsProvider]
	public static SettingsProvider CreateSettingsProvider()
	{
		// Project Settings > Arch Framework
		return new ConfigSettingsProvider("Project/Arch Framework", SettingsScope.Project);
	}
}
#endif
