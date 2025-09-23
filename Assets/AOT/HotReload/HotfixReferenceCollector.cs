using Attributes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Arch
{
	public class HotfixReferenceCollector
	{
		// ��֪�ĳ�������Ŀ¼����ܿ����õ���չ�㣩
		public static readonly List<string> SearchDirectories = new()
		{
			Path.GetFullPath(Path.Combine(Application.dataPath, "..\\HybridCLRData\\AssembliesPostIl2CppStrip\\StandaloneWindows64")),
			Path.GetDirectoryName(typeof(object).Assembly.Location),
			Path.GetDirectoryName(Path.Combine(Application.dataPath, "..\\Library\\ScriptAssemblies")),
		};

		/// <summary>
		/// �ռ���ǰHotfix.dll������·�������浽�ļ�
		/// </summary>
		/// <param name="hotfixAssembly">��ǰ���ص�Hotfix����</param>
		/// <param name="outputFilePath">�����б���·��</param>
		public static List<string> CollectAndSaveReferences()
		{
			// 1. ��ȡHotfix.dllֱ�����õ����г�������
			//��������ScriptAssembly�ĳ���
			Assembly hotfixAssembly = Assembly.Load(Assemblys.HOTFIX_ASSEMBLY);
			var referencedAssemblyNames = hotfixAssembly.GetReferencedAssemblies();

			// 2. ����ÿ�����õ�����·��
			var referencePaths = new List<string>();

			AddReferenceIfExists(referencePaths, SearchDirectories[1], "System.dll");
			AddReferenceIfExists(referencePaths, SearchDirectories[1], "System.Core.dll");

			foreach (var assemblyName in referencedAssemblyNames)
			{
				// ���ҳ����ļ�·��
				string assemblyPath = FindAssemblyPath(assemblyName);
				if (!string.IsNullOrEmpty(assemblyPath) && File.Exists(assemblyPath))
				{
					referencePaths.Add(assemblyPath);
				}
				else
				{
					Debug.LogWarning($"δ�ҵ����õĳ��򼯣�{assemblyName.FullName}");
				}
			}

			return referencePaths;
		}

		/// <summary>
		/// ������Ŀ¼�в��ҳ����ļ�
		/// </summary>
		private static string FindAssemblyPath(AssemblyName assemblyName)
		{
			string fileName = $"{assemblyName.Name}.dll";
			foreach (var dir in SearchDirectories)
			{
				if (!Directory.Exists(dir)) continue;

				var foundFiles = Directory.EnumerateFiles(dir, fileName, SearchOption.AllDirectories)
					.Select(Path.GetFullPath); // �淶���ҵ����ļ�·��
				foreach (var file in foundFiles)
				{
					if (CheckAssemblyVersion(file, assemblyName.Version))
					{
						return file; // �����ѹ淶����·��
					}
				}
			}
			return null;
		}

		/// <summary>
		/// У����򼯰汾�Ƿ�ƥ�䣨��ѡ��
		/// </summary>
		private static bool CheckAssemblyVersion(string assemblyPath, Version targetVersion)
		{
			if (targetVersion == null) return true; // ��У��汾

			try
			{
				var assembly = Assembly.LoadFrom(assemblyPath);
				return assembly.GetName().Version == targetVersion;
			}
			catch
			{
				return false;
			}
		}
		// ��������������ļ���������ӵ������б�
		private static void AddReferenceIfExists(List<string> references, string dir, string dllName)
		{
			string dllPath = Path.Combine(dir, dllName);
			if (File.Exists(dllPath))
			{
				references.Add(Path.GetFullPath(dllPath));
			}
			else
			{
				Debug.LogWarning($"��Ŀ¼ {dir} ��δ�ҵ� {dllName}����������ʹ��������Ϳ��ܵ��±������");
			}
		}
	}


}
