#if UNITY_EDITOR

using Arch.Tools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;

namespace Arch.Compilation.Editor
{
	internal static class AssemblyBuilderPipeline
	{
		/// <summary>
		/// 逐条构建 Isolated 配置（用于热更/模块化）。
		/// </summary>
		public static bool BuildIsolated(ArchBuildConfig cfg)
		{
			ExecutePreBuildProcessors();
			bool ok = true;
			foreach (var item in cfg.buildSetting.isolated)
			{
				ok &= BuildOne(item);
			}
			ExecuteGlobalPostProcessors();
			return ok;
		}

		/// <summary>
		/// 构建全联编 DLL。
		/// </summary>
		public static bool BuildFullLink(ArchBuildConfig cfg)
		{
			ExecutePreBuildProcessors();
			bool ok = BuildOne(cfg.buildSetting.fullLink);
			ExecuteGlobalPostProcessors();
			return ok;
		}

		public static bool BuildOne(IsolatedAssembly a)
		{
			var scripts = CollectScripts(a.sourceDirs);
			if (scripts.Length == 0)
			{
				ArchLog.LogWarning($"[AssemblyBuilder] 找不到源码：{string.Join(", ", a.sourceDirs)}");
				return false;
			}
			EnsureDir(a.outputDir);
			var dllPath = Path.GetFullPath(Path.Combine(a.outputDir, $"{a.assemblyName}.dll"));
			return BuildInternal(scripts, dllPath, a.additionalDefines, a.additionalReferences, a.useEngineModules, a.editorAssembly);
		}

		public static bool BuildOne(FullLinkAssembly a)
		{
			var scripts = CollectScripts(a.sourceDirs);
			if (scripts.Length == 0)
			{
				ArchLog.LogWarning($"[AssemblyBuilder] 找不到源码：{string.Join(", ", a.sourceDirs)}");
				return false;
			}
			EnsureDir(a.outputDir);
			var dllPath = Path.GetFullPath(Path.Combine(a.outputDir, $"{a.assemblyName}.dll"));
			return BuildInternal(scripts, dllPath, a.additionalDefines, a.additionalReferences, a.useEngineModules, a.editorAssembly);
		}

		/// <summary>
		/// 统一调度 Unity 的 AssemblyBuilder。
		/// </summary>
		private static bool BuildInternal(
			string[] scriptPaths,
			string dllPath,
			List<string> defines,
			List<string> additionalRefs,
			bool useEngineModules,
			bool editorAssembly)
		{
			var ab = new AssemblyBuilder(dllPath, scriptPaths);

			if (defines is { Count: > 0 })
				ab.additionalDefines = defines.ToArray();

			if (additionalRefs is { Count: > 0 })
				ab.additionalReferences = additionalRefs.ToArray();

			ab.referencesOptions = useEngineModules
				? ReferencesOptions.UseEngineModules
				: ReferencesOptions.None;

			ab.flags = editorAssembly
				? AssemblyBuilderFlags.EditorAssembly
				: AssemblyBuilderFlags.None;

			bool buildOk = true;

			ab.buildStarted += path =>
			{
				ArchLog.LogInfo($"[AB] Start: {path}");
			};

			ab.buildFinished += (path, messages) =>
			{
				var errs = messages?.Where(m => m.type == CompilerMessageType.Error).ToList();
				var warns = messages?.Where(m => m.type == CompilerMessageType.Warning).ToList();

				if (warns is { Count: > 0 })
				{
					foreach (var w in warns)
						ArchLog.LogWarning($"[AB][{Path.GetFileName(path)}] {w.message}\n{w.file}:{w.line}");
				}

				if (errs is { Count: > 0 })
				{
					buildOk = false;
					foreach (var e in errs)
						ArchLog.LogError($"[AB][{Path.GetFileName(path)}] {e.message}\n{e.file}:{e.line}");
				}
			};

			if (!ab.Build())
			{
				ArchLog.LogError("[AB] 构建未能启动（可能正在编译或参数错误）");
				return false;
			}

			// 简单阻塞等待（编辑器环境安全；如需异步可改回调）
			while (ab.status == AssemblyBuilderStatus.IsCompiling)
				System.Threading.Thread.Sleep(50);

			ArchLog.LogInfo($"[AB] Done: {dllPath}");
			AssetDatabase.Refresh();
			if (buildOk = buildOk && File.Exists(dllPath))
			{
				ExecutePostProcessors(dllPath);
			}
			return buildOk;
		}

		private static void ExecuteGlobalPostProcessors()
		{
			ArchBuildConfig cfg = ArchBuildConfig.LoadOrCreate();
			if (cfg == null || cfg.compilePipeLineSetting.globalPostProcessors == null || cfg.compilePipeLineSetting.globalPostProcessors.Count == 0)
				return;

			ArchLog.LogInfo($"[PostBuild] 开始执行全局后处理，共 {cfg.compilePipeLineSetting.globalPostProcessors.Count} 个。");

			foreach (var name in cfg.compilePipeLineSetting.globalPostProcessors)
			{
				if (AttributeTargetRegistry.TryGet<GlobalPostBuildProcessorRegistry, IGlobalPostProcessor>(name, out var processor))
				{
					try
					{
						processor.Process(cfg);
					}
					catch (System.Exception ex)
					{
						ArchLog.LogError($"[PostBuild] 全局后处理器 {name} 执行失败: {ex.Message}");
					}
				}
				else
				{
					ArchLog.LogWarning($"[PostBuild] 未找到全局后处理器: {name}");
				}
			}

			ArchLog.LogInfo("[PostBuild] 全部全局后处理完成。");
		}

		private static void ExecutePostProcessors(string builtDllPath)
		{
			ArchBuildConfig cfg = ArchBuildConfig.LoadOrCreate();

			if (cfg == null || cfg.compilePipeLineSetting.postProcessors == null || cfg.compilePipeLineSetting.postProcessors.Count == 0)
				return;

			ArchLog.LogInfo($"[PostBuild] 执行后处理流程，共 {cfg.compilePipeLineSetting.postProcessors.Count} 项。");

			foreach (var name in cfg.compilePipeLineSetting.postProcessors)
			{
				if (AttributeTargetRegistry.TryGet<UnitPostBuildProcessorRegistry, IUnitPostBuildProcessor>(name, out var processor))
				{
					try
					{
						processor.Process(cfg, builtDllPath);
					}
					catch (System.Exception ex)
					{
						ArchLog.LogError($"[PostBuild] 处理器 {name} 执行失败：{ex.Message}");
					}
				}
				else
				{
					ArchLog.LogWarning($"[PostBuild] 未找到处理器: {name}");
				}
			}
		}

		private static void ExecutePreBuildProcessors()
		{
			ArchBuildConfig cfg = ArchBuildConfig.LoadOrCreate();
			if (cfg == null || cfg.compilePipeLineSetting.preBuildProcessors == null || cfg.compilePipeLineSetting.preBuildProcessors.Count == 0)
				return;

			ArchLog.LogInfo($"[PreBuild] 开始执行编译前处理，共 {cfg.compilePipeLineSetting.preBuildProcessors.Count} 项。");

			foreach (var name in cfg.compilePipeLineSetting.preBuildProcessors)
			{
				if (AttributeTargetRegistry.TryGet<PreBuildProcessorRegistry, IPreBuildProcessor>(name, out var processor))
				{
					try
					{
						processor.Process(cfg);
						ArchLog.LogInfo($"[PreBuild] 已执行: {processor.Name}");
					}
					catch (Exception ex)
					{
						ArchLog.LogError($"[PreBuild] {processor.Name} 执行失败: {ex.Message}");
					}
				}
				else
				{
					ArchLog.LogWarning($"[PreBuild] 未找到处理器: {name}");
				}
			}

			ArchLog.LogInfo("[PreBuild] 全部编译前处理完成。");
		}

		private static string[] CollectScripts(IEnumerable<string> dirs)
		{
			var list = new List<string>();
			foreach (var d in dirs ?? Enumerable.Empty<string>())
			{
				if (string.IsNullOrEmpty(d)) continue;
				var full = Path.GetFullPath(d);
				if (!Directory.Exists(full)) continue;

				list.AddRange(Directory.GetFiles(full, "*.cs", SearchOption.AllDirectories)
					.Where(p => !p.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)));
			}
			return list.Distinct().ToArray();
		}

		private static void EnsureDir(string dir)
		{
			if (string.IsNullOrEmpty(dir)) return;
			var full = Path.GetFullPath(dir);
			if (!Directory.Exists(full)) Directory.CreateDirectory(full);
		}
	}
}

#endif