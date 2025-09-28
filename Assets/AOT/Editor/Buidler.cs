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

		[MenuItem("Tools/�ȸ��´�� _F5")]
		public static void TriggerCompilation()
		{
			MetaDLLResPath = ArchConfig.Instance.metaDllOutputPath;
			HotDllResPath = ArchConfig.Instance.hotUpdateDllOutputPath;
			if (string.IsNullOrEmpty(MetaDLLResPath) || string.IsNullOrEmpty(HotDllResPath))
			{
				ArchLog.LogError("�ȸ��³��򼯵���ԴĿ¼Ϊ��!");
				return;
			}
			if (AssetDatabase.FindAssets("t:Script AOTGenericReferences").Length == 0)
			{
				EditorUtility.DisplayDialog("Error", "��ִ�л�٢��Generate/all��", "OK");
				ArchLog.LogError("��ִ�л�٢��Generate/all!");
				return;
			}
			HybridCLR.Editor.Commands.LinkGeneratorCommand.GenerateLinkXml();
			if (!CheckAccessMissingMetadata())
			{
				EditorUtility.DisplayDialog("Error", "����ȱʧԪ���ݣ���ִ�л�٢��Generate/all!", "OK");
				ArchLog.LogError("����ȱʧԪ���ݣ���ִ�л�٢��Generate/all!");
				return;
			}
			if (SettingsUtil.AOTAssemblyNames.Count == 0)
			{
				EditorUtility.DisplayDialog("Error", "���ȸ���AOTGenericReferences�ļ�������٢���������ò���Ԫ����AOT�����ƣ�", "OK");
				ArchLog.LogError("���ȸ���AOTGenericReferences�ļ�������٢���������ò���Ԫ����AOT������!");
				return;
			}
			if (!CopyMetaDllInProject())
			{
				EditorUtility.DisplayDialog("Error", "��ִ�л�٢��Generate/all����ȫԪ���ݼ���", "OK");
				ArchLog.LogError("��ִ�л�٢��Generate/all����ȫԪ���ݼ�!");
				return;
			}
			if (!Directory.Exists(SettingsUtil.HotUpdateDllsRootOutputDir))
			{
				EditorUtility.DisplayDialog("Error", "���ȵ���٢������ָ���ȸ��³���Ŀ¼���ȸ��³��򼯣�", "OK");
				ArchLog.LogError("���ȵ���٢������ָ���ȸ��³���Ŀ¼���ȸ��³��򼯣�");
				return;
			}
			if (!CopyHotUpdateDllInProject())
			{
				EditorUtility.DisplayDialog("Error", "���ȵ���٢������ָ���ȸ��³���Ŀ¼���ȸ��³��򼯣�", "OK");
				ArchLog.LogError("���ȵ���٢������ָ���ȸ��³���Ŀ¼���ȸ��³��򼯣�");
				return;
			}

			AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
			AddressableAssetSettings.BuildPlayerContent();
			ArchLog.LogInfo("����ɹ�");

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
					ArchLog.LogInfo($"�ѽ�{szHotUpdateDllName}��������ԴĿ¼");
				}
				else
				{
					ArchLog.LogInfo($"{SettingsUtil.HotUpdateDllsRootOutputDir}�£�������{szHotUpdateDllName}��");
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
					ArchLog.LogInfo($"�ѽ�{szMetaDllName}��������ԴĿ¼");
				}
				else
				{
					ArchLog.LogInfo($"{SettingsUtil.AssembliesPostIl2CppStripDir}�£�������{szMetaDllName}��");
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
				ArchLog.LogError($"Դ�ļ�������: {sourcePath}");
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

				ArchLog.LogInfo($"�Ѹ���DLL��: {targetPath}");
			}
			catch (Exception ex)
			{
				ArchLog.LogError($"����DLLʧ��: {ex.Message}");
			}
		}
		public static void DeleteAllFilesInDirectory(string targetDir)
		{
			if (!Directory.Exists(targetDir))
			{
				ArchLog.LogWarning($"Ŀ¼������: {targetDir}");
				return;
			}

			try
			{
				// ��ȡ�����ļ���������Ŀ¼��
				string[] files = Directory.GetFiles(targetDir, "*", SearchOption.AllDirectories);

				foreach (string file in files)
				{
					// �����ļ�����Ϊ��ͨ�����ֻ�������ƣ�
					File.SetAttributes(file, FileAttributes.Normal);
					File.Delete(file);
					ArchLog.LogInfo($"��ɾ���ļ�: {file}");
				}

				// �����Ҫͬʱɾ����Ŀ¼���ɼ��ϣ�
				foreach (string dir in Directory.GetDirectories(targetDir))
				{
					Directory.Delete(dir, true);
				}
			}
			catch (System.Exception e)
			{
				ArchLog.LogError($"ɾ��ʧ��: {e.Message}");
			}
		}
	}
}
#endif