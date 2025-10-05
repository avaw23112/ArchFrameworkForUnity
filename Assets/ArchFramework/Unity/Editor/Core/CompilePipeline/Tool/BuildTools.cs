#if UNITY_EDITOR

using Arch.Tools;
using HybridCLR.Editor;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Arch.Compilation.Editor
{
	public static class Builder
	{
		public static void EditorModifyDllPath(SerializedObject config)
		{
			var metaPathProp = config.FindProperty("buildSetting.MetaDllPath");
			var hotfixPathProp = config.FindProperty("buildSetting.HotFixDllPath");

			EditorGUILayout.LabelField("热更新路径设置", EditorStyles.boldLabel);

			// ===== 元数据路径 =====
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PropertyField(metaPathProp, new GUIContent("元数据程序集路径"));
			if (GUILayout.Button("选择路径", GUILayout.Width(80)))
			{
				string selected = EditorUtility.OpenFolderPanel("选择元数据程序集目录", metaPathProp.stringValue, "");
				if (!string.IsNullOrEmpty(selected))
				{
					// 如果是 Assets 目录下的路径，可自动转换相对路径
					if (selected.StartsWith(Application.dataPath))
						selected = "Assets" + selected.Substring(Application.dataPath.Length);
					metaPathProp.stringValue = selected;
				}
			}
			EditorGUILayout.EndHorizontal();

			// ===== 热更新路径 =====
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PropertyField(hotfixPathProp, new GUIContent("热更新程序集路径"));
			if (GUILayout.Button("选择路径", GUILayout.Width(80)))
			{
				string selected = EditorUtility.OpenFolderPanel("选择热更新程序集目录", hotfixPathProp.stringValue, "");
				if (!string.IsNullOrEmpty(selected))
				{
					if (selected.StartsWith(Application.dataPath))
						selected = "Assets" + selected.Substring(Application.dataPath.Length);
					hotfixPathProp.stringValue = selected;
				}
			}
			EditorGUILayout.EndHorizontal();

			// 立即应用修改
			config.ApplyModifiedProperties();
		}

		public static bool CheckAccessMissingMetadata()
		{
			BuildTarget target = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
			string aotDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
			var checker = new HybridCLR.Editor.HotUpdate.MissingMetadataChecker(aotDir, SettingsUtil.HotUpdateAssemblyNamesIncludePreserved);
			string hotUpdateDir = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
			foreach (var dll in SettingsUtil.HotUpdateAssemblyFilesExcludePreserved)
			{
				string dllPath = $"{hotUpdateDir}/{dll}";
				if (!checker.Check(dllPath))
					return false;
			}
			return true;
		}

		public static void CopyCompiledDll(string sourcePath, string targetDir, string suffix)
		{
			if (!File.Exists(sourcePath)) return;
			if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

			string fileName = Path.GetFileNameWithoutExtension(sourcePath);
			if (!suffix.StartsWith(".")) suffix = "." + suffix;
			string targetPath = Path.Combine(targetDir, $"{fileName}{suffix}");
			File.Copy(sourcePath, targetPath, true);
			ArchLog.LogInfo($"已复制DLL到: {targetPath}");
		}

		public static void DeleteAllFilesInDirectory(string targetDir)
		{
			if (!Directory.Exists(targetDir)) return;
			foreach (string file in Directory.GetFiles(targetDir, "*", SearchOption.AllDirectories))
			{
				File.SetAttributes(file, FileAttributes.Normal);
				File.Delete(file);
			}
			foreach (string dir in Directory.GetDirectories(targetDir))
				Directory.Delete(dir, true);
		}
	}
}

#endif