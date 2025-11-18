#if UNITY_EDITOR

using Arch.Tools;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Arch.Compilation.Editor
{
	internal static class FodyProcessorHelper
	{
		public static string UnityNativeDll => Path.GetDirectoryName(EditorApplication.applicationPath) + "/Data/Managed";

		public static List<WeaverEntry> weavers = new List<WeaverEntry>();

		public static void ProcessAssembly(ArchBuildConfig cfg, string assemblyPath, IAssemblyResolver assemblyResolver)
		{
			var readerParameters = new ReaderParameters();
			readerParameters.AssemblyResolver = assemblyResolver;
			var writerParameters = new WriterParameters();

			var weavers = InitializeWeavers(cfg, assemblyResolver);
			InitParameter(assemblyPath, readerParameters, writerParameters);

			var module = ModuleDefinition.ReadModule(assemblyPath, readerParameters);
			PrepareWeaversForModule(weavers, module);
			try
			{
				if (ProcessAssemblyInternal(assemblyPath, module, weavers))
				{
					module.Write(assemblyPath, writerParameters);
					ArchLog.LogInfo($"Weaver success for {assemblyPath}");
				}
			}
			catch (Exception e)
			{
				ArchLog.LogError(e);
				throw;
			}
		}

		private static void InitParameter(string assemblyPath, ReaderParameters readerParameters, WriterParameters writerParameters)
		{
			string originPath = RemoveDllSuffix(assemblyPath);
			string mdbPath = originPath + ".mdb";
			String pdbPath = originPath + ".pdb";
			if (File.Exists(pdbPath))
			{
				readerParameters.ReadSymbols = true;
				readerParameters.SymbolReaderProvider = new Mono.Cecil.Pdb.PdbReaderProvider();
				writerParameters.WriteSymbols = true;
				writerParameters.SymbolWriterProvider = new Mono.Cecil.Mdb.MdbWriterProvider();
			}
			else if (File.Exists(mdbPath))
			{
				readerParameters.ReadSymbols = true;
				readerParameters.SymbolReaderProvider = new Mono.Cecil.Mdb.MdbReaderProvider();
				writerParameters.WriteSymbols = true;
				writerParameters.SymbolWriterProvider = new Mono.Cecil.Mdb.MdbWriterProvider();
			}
			{
				readerParameters.ReadSymbols = false;
				readerParameters.SymbolReaderProvider = null;
				writerParameters.WriteSymbols = false;
				writerParameters.SymbolWriterProvider = null;
			}
		}

		public static List<WeaverEntry> InitializeWeavers(ArchBuildConfig cfg, IAssemblyResolver resolver)
		{
			foreach (var weaverConfig in cfg.compilePipeLineSetting.weaverPaths)
			{
				if (weavers.Any(item => item.AssemblyPath == weaverConfig))
				{
					continue;
				}

				var weaverEntry = new WeaverEntry();
				weaverEntry.AssemblyPath = weaverConfig;
				weaverEntry.AssemblyName = Path.GetFileName(weaverConfig);
				weaverEntry.TypeName = "ModuleWeaver";

				var assembly = LoadAssembly(weaverEntry.AssemblyPath);
				var weaverType = GetType(assembly, weaverEntry.TypeName);
				if (weaverType == null)
				{
					weavers.Remove(weaverEntry);
					continue;
				}

				weaverEntry.Activate(weaverType);
				SetProperties(weaverEntry, resolver);
				weavers.Add(weaverEntry);
			}
			return weavers;
		}

		public static bool ProcessAssemblyInternal(string assemblyPath, ModuleDefinition module, IEnumerable<WeaverEntry> weavers)
		{
			//过滤已经被编织的程序集
			if (module.Types.Any(t => t.Name == "ProcessedByFody"))
			{
				return false;
			}

			//对剩余程序集进行编织
			foreach (var weaver in weavers)
			{
				if (weaver.WeaverInstance == null) continue;
				try
				{
					weaver.Run("Execute");
				}
				catch (Exception e)
				{
					ArchLog.LogError($"Failed to run weaver {weaver.PrettyName()} on {assemblyPath}: {e}");
				}
			}

			//添加已编织标记
			AddProcessedFlag(module);
			return true;
		}

		public static void SetProperties(WeaverEntry weaverEntry, IAssemblyResolver resolver)
		{
			if (weaverEntry.WeaverInstance == null) return;
			weaverEntry.TrySetProperty("AssemblyResolver", resolver);
			weaverEntry.TryAddEvent("LogDebug", new Action<string>((str) => ArchLog.LogDebug(str)));
			weaverEntry.TryAddEvent("LogInfo", new Action<string>((str) => ArchLog.LogInfo(str)));
			weaverEntry.TryAddEvent("LogWarning", new Action<string>((str) => ArchLog.LogWarning(str)));
		}

		public static IAssemblyResolver CreateAssembly()
		{
			var assemblyResolver = new DefaultAssemblyResolver();
			try
			{
				EditorApplication.LockReloadAssemblies();

				var assetPath = Path.GetFullPath(Application.dataPath);

				HashSet<string> assemblyPaths = new HashSet<string>();
				HashSet<string> assemblySearchDirectories = new HashSet<string>();

				foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
				{
					if (assembly.IsDynamic)
						continue;

					try
					{
						if (assembly.Location.Replace('\\', '/').StartsWith(Application.dataPath.Substring(0, Application.dataPath.Length - 7)) &&
							!Path.GetFullPath(assembly.Location).StartsWith(assetPath)) //but not in the assets folder
						{
							assemblyPaths.Add(assembly.Location);
						}
						if (!string.IsNullOrWhiteSpace(assembly.Location))
							assemblySearchDirectories.Add(Path.GetDirectoryName(assembly.Location));
						else
							ArchLog.LogWarning("Assembly " + assembly.FullName + " has an empty path. Skipping");
					}
					catch (Exception e)
					{
						ArchLog.LogError($"{assembly.FullName} - {e}");
					}
				}

				foreach (string searchDirectory in assemblySearchDirectories)
				{
					assemblyResolver.AddSearchDirectory(searchDirectory);
				}
				assemblyResolver.AddSearchDirectory(UnityNativeDll);
			}
			catch (Exception e)
			{
				Debug.LogError(e);
			}
			finally
			{
				EditorApplication.UnlockReloadAssemblies();
			}
			return assemblyResolver;
		}

		private static Type GetType(Assembly assembly, string typeName)
		{
			return assembly.GetTypes().FirstOrDefault(t => t.Name == typeName);
		}

		public static void PrepareWeaversForModule(List<WeaverEntry> weavers, ModuleDefinition module)
		{
			foreach (var weaver in weavers)
			{
				weaver.SetProperty("ModuleDefinition", module);
			}
		}

		public static void AddProcessedFlag(ModuleDefinition module)
		{
			module.Types.Add(new TypeDefinition(null, "ProcessedByFody", Mono.Cecil.TypeAttributes.NotPublic | Mono.Cecil.TypeAttributes.Abstract | Mono.Cecil.TypeAttributes.Interface));
		}

		private static Dictionary<string, Assembly> assemblies = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

		public static Assembly LoadAssembly(string assemblyPath)
		{
			Assembly assembly;
			if (assemblies.TryGetValue(assemblyPath, out assembly))
			{
				return assembly;
			}
			return assemblies[assemblyPath] = LoadFromFile(assemblyPath);
		}

		public static Assembly LoadFromFile(string assemblyPath)
		{
			var pdbPath = Path.ChangeExtension(assemblyPath, "pdb");
			var rawAssembly = File.ReadAllBytes(assemblyPath);
			if (File.Exists(pdbPath))
			{
				return Assembly.Load(rawAssembly, File.ReadAllBytes(pdbPath));
			}
			var mdbPath = Path.ChangeExtension(assemblyPath, "mdb");
			if (File.Exists(mdbPath))
			{
				return Assembly.Load(rawAssembly, File.ReadAllBytes(mdbPath));
			}
			return Assembly.Load(rawAssembly);
		}

		/// <summary>
		/// 去除文件路径末尾的 .dll 后缀（不区分大小写）
		/// </summary>
		/// <param name="filePath">带 .dll 后缀的文件路径</param>
		/// <returns>去除 .dll 后的路径，若路径不以 .dll 结尾则返回原路径</returns>
		public static string RemoveDllSuffix(string filePath)
		{
			// 处理空路径或无效路径
			if (string.IsNullOrEmpty(filePath))
				return filePath;

			// 检查路径是否以 .dll 结尾（忽略大小写，如 .DLL 也会被处理）
			if (filePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
			{
				// 截取掉最后 4 个字符（.dll 共 4 个字符）
				return filePath.Substring(0, filePath.Length - 4);
			}

			// 若不以 .dll 结尾，直接返回原路径
			return filePath;
		}
	}
}

#endif