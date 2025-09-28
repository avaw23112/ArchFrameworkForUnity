#if UNITY_EDITOR
using Arch.Editor;
using Arch.Tools;
using HybridCLR.Editor;
using HybridCLR.Editor.HotUpdate;
using System;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Arch.Compilation.Editor
{
	public static class Builder
	{
		static string MetaDLLResPath = "";
		static string HotDllResPath = "";

		[MenuItem("Tools/热更新打包 _F5")]
		public static void TriggerCompilation()
		{
			MetaDLLResPath = ArchConfig.Instance.metaDllOutputPath;
			HotDllResPath = ArchConfig.Instance.hotUpdateDllOutputPath;
			if (string.IsNullOrEmpty(MetaDLLResPath) || string.IsNullOrEmpty(HotDllResPath))
			{
				ArchLog.LogError("热更新程序集的资源目录为空!");
				return;
			}
			if (AssetDatabase.FindAssets("t:Script AOTGenericReferences").Length == 0)
			{
				EditorUtility.DisplayDialog("Error", "请执行华佗的Generate/all！", "OK");
				ArchLog.LogError("请执行华佗的Generate/all!");
				return;
			}
			HybridCLR.Editor.Commands.LinkGeneratorCommand.GenerateLinkXml();
			if (!CheckAccessMissingMetadata())
			{
				EditorUtility.DisplayDialog("Error", "环境缺失元数据，请执行华佗的Generate/all!", "OK");
				ArchLog.LogError("环境缺失元数据，请执行华佗的Generate/all!");
				return;
			}
			if (SettingsUtil.AOTAssemblyNames.Count == 0)
			{
				EditorUtility.DisplayDialog("Error", "请先根据AOTGenericReferences文件，到华佗设置中配置补充元数据AOT集名称！", "OK");
				ArchLog.LogError("请先根据AOTGenericReferences文件，到华佗设置中配置补充元数据AOT集名称!");
				return;
			}
			if (!CopyMetaDllInProject())
			{
				EditorUtility.DisplayDialog("Error", "请执行华佗的Generate/all，补全元数据集！", "OK");
				ArchLog.LogError("请执行华佗的Generate/all，补全元数据集!");
				return;
			}
			if (!Directory.Exists(SettingsUtil.HotUpdateDllsRootOutputDir))
			{
				EditorUtility.DisplayDialog("Error", "请先到华佗设置中指定热更新程序集目录与热更新程序集！", "OK");
				ArchLog.LogError("请先到华佗设置中指定热更新程序集目录与热更新程序集！");
				return;
			}
			if (!CopyHotUpdateDllInProject())
			{
				EditorUtility.DisplayDialog("Error", "请先到华佗设置中指定热更新程序集目录与热更新程序集！", "OK");
				ArchLog.LogError("请先到华佗设置中指定热更新程序集目录与热更新程序集！");
				return;
			}

			AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
			AddressableAssetSettings.BuildPlayerContent();
			ArchLog.LogInfo("打包成功");

			ResourceNameMapGenerator.GenerateResourceNameMap();
		}
		public static bool CopyHotUpdateDllInProject()
		{
			string basePath = Path.Combine(Application.dataPath, "..", SettingsUtil.HotUpdateDllsRootOutputDir, EditorUserBuildSettings.activeBuildTarget.ToString());
			DeleteAllFilesInDirectory(HotDllResPath);
			foreach (string szHotUpdateDllName in SettingsUtil.HotUpdateAssemblyNamesExcludePreserved)
			{
				string HotUpdatePath = Path.Combine(basePath, $"{szHotUpdateDllName}.dll");
				if (File.Exists(HotUpdatePath))
				{
					CopyCompiledDll(HotUpdatePath, HotDllResPath, ".bytes");
					ArchLog.LogInfo($"已将{szHotUpdateDllName}拷贝到资源目录");
				}
				else
				{
					ArchLog.LogInfo($"{SettingsUtil.HotUpdateDllsRootOutputDir}下，不存在{szHotUpdateDllName}！");
					return false;
				}
			}
			return true;
		}
		public static bool CopyMetaDllInProject()
		{
			string basePath = Path.Combine(Application.dataPath, "..", SettingsUtil.AssembliesPostIl2CppStripDir, EditorUserBuildSettings.activeBuildTarget.ToString());
			DeleteAllFilesInDirectory(MetaDLLResPath);
			foreach (string szMetaDllName in SettingsUtil.AOTAssemblyNames)
			{
				string MetaDllPath = Path.Combine(basePath, $"{szMetaDllName}.dll");
				if (File.Exists(MetaDllPath))
				{
					CopyCompiledDll(MetaDllPath, MetaDLLResPath, ".bytes");
					ArchLog.LogInfo($"已将{szMetaDllName}拷贝到资源目录");
				}
				else
				{
					ArchLog.LogInfo($"{SettingsUtil.AssembliesPostIl2CppStripDir}下，不存在{szMetaDllName}！");
					return false;
				}
			}
			return true;
		}

		public static bool CheckAccessMissingMetadata()
		{
			BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
			string aotDir = SettingsUtil.GetAssembliesPostIl2CppStripDir(target);
			var checker = new MissingMetadataChecker(aotDir, SettingsUtil.HotUpdateAssemblyNamesIncludePreserved);
			string hotUpdateDir = SettingsUtil.GetHotUpdateDllsOutputDirByTarget(target);
			foreach (var dll in SettingsUtil.HotUpdateAssemblyFilesExcludePreserved)
			{
				string dllPath = $"{hotUpdateDir}/{dll}";
				bool notAnyMissing = checker.Check(dllPath);
				if (!notAnyMissing)
				{
					return false;
				}
			}
			return true;
		}

		private static void CopyCompiledDll(string sourcePath, string targetDir, string suffix)
		{
			if (!File.Exists(sourcePath))
			{
				ArchLog.LogError($"源文件不存在: {sourcePath}");
				return;
			}

			if (string.IsNullOrEmpty(targetDir))
				return;

			try
			{
				if (!Directory.Exists(targetDir))
				{
					Directory.CreateDirectory(targetDir);
				}

				string fileName = Path.GetFileNameWithoutExtension(sourcePath);
				string newExtension = !string.IsNullOrEmpty(suffix) ? suffix : "dll";
				if (!newExtension.StartsWith("."))
					newExtension = "." + newExtension;

				string targetPath = Path.Combine(targetDir, $"{fileName}{newExtension}");
				File.Copy(sourcePath, targetPath, true);

				ArchLog.LogInfo($"已复制DLL到: {targetPath}");
			}
			catch (Exception ex)
			{
				ArchLog.LogError($"复制DLL失败: {ex.Message}");
			}
		}
		public static void DeleteAllFilesInDirectory(string targetDir)
		{
			if (!Directory.Exists(targetDir))
			{
				ArchLog.LogWarning($"目录不存在: {targetDir}");
				return;
			}

			try
			{
				// 获取所有文件（包括子目录）
				string[] files = Directory.GetFiles(targetDir, "*", SearchOption.AllDirectories);

				foreach (string file in files)
				{
					// 设置文件属性为普通（解除只读等限制）
					File.SetAttributes(file, FileAttributes.Normal);
					File.Delete(file);
					ArchLog.LogInfo($"已删除文件: {file}");
				}

				// 如果需要同时删除空目录，可加上：
				foreach (string dir in Directory.GetDirectories(targetDir))
				{
					Directory.Delete(dir, true);
				}
			}
			catch (System.Exception e)
			{
				ArchLog.LogError($"删除失败: {e.Message}");
			}
		}
	}
}
#endif