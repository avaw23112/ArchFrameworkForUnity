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
		// 已知的程序集搜索目录（框架可配置的扩展点）
		public static readonly List<string> SearchDirectories = new()
		{
			Path.GetFullPath(Path.Combine(Application.dataPath, "..\\HybridCLRData\\AssembliesPostIl2CppStrip\\StandaloneWindows64")),
			Path.GetDirectoryName(typeof(object).Assembly.Location),
			Path.GetDirectoryName(Path.Combine(Application.dataPath, "..\\Library\\ScriptAssemblies")),
		};

		/// <summary>
		/// 收集当前Hotfix.dll的引用路径并保存到文件
		/// </summary>
		/// <param name="hotfixAssembly">当前加载的Hotfix程序集</param>
		/// <param name="outputFilePath">引用列表保存路径</param>
		public static List<string> CollectAndSaveReferences()
		{
			// 1. 获取Hotfix.dll直接引用的所有程序集名称
			//这里利用ScriptAssembly的程序集
			Assembly hotfixAssembly = Assembly.Load(Assemblys.HOTFIX_ASSEMBLY);
			var referencedAssemblyNames = hotfixAssembly.GetReferencedAssemblies();

			// 2. 解析每个引用的物理路径
			var referencePaths = new List<string>();

			AddReferenceIfExists(referencePaths, SearchDirectories[1], "System.dll");
			AddReferenceIfExists(referencePaths, SearchDirectories[1], "System.Core.dll");

			foreach (var assemblyName in referencedAssemblyNames)
			{
				// 查找程序集文件路径
				string assemblyPath = FindAssemblyPath(assemblyName);
				if (!string.IsNullOrEmpty(assemblyPath) && File.Exists(assemblyPath))
				{
					referencePaths.Add(assemblyPath);
				}
				else
				{
					Debug.LogWarning($"未找到引用的程序集：{assemblyName.FullName}");
				}
			}

			return referencePaths;
		}

		/// <summary>
		/// 在搜索目录中查找程序集文件
		/// </summary>
		private static string FindAssemblyPath(AssemblyName assemblyName)
		{
			string fileName = $"{assemblyName.Name}.dll";
			foreach (var dir in SearchDirectories)
			{
				if (!Directory.Exists(dir)) continue;

				var foundFiles = Directory.EnumerateFiles(dir, fileName, SearchOption.AllDirectories)
					.Select(Path.GetFullPath); // 规范化找到的文件路径
				foreach (var file in foundFiles)
				{
					if (CheckAssemblyVersion(file, assemblyName.Version))
					{
						return file; // 返回已规范化的路径
					}
				}
			}
			return null;
		}

		/// <summary>
		/// 校验程序集版本是否匹配（可选）
		/// </summary>
		private static bool CheckAssemblyVersion(string assemblyPath, Version targetVersion)
		{
			if (targetVersion == null) return true; // 不校验版本

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
		// 辅助方法：如果文件存在则添加到引用列表
		private static void AddReferenceIfExists(List<string> references, string dir, string dllName)
		{
			string dllPath = Path.Combine(dir, dllName);
			if (File.Exists(dllPath))
			{
				references.Add(Path.GetFullPath(dllPath));
			}
			else
			{
				Debug.LogWarning($"在目录 {dir} 中未找到 {dllName}，若代码中使用相关类型可能导致编译错误");
			}
		}
	}


}
