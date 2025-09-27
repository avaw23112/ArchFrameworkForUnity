// HotReloader.cs (完整更新版)
#if UNITY_EDITOR
using Arch.Compilation;
using Arch.Tools;
using Assets.Scripts;
using Attributes;
using Cysharp.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Arch.Editor
{
	public static class HotReloader
	{
		private static bool isReloading = false;
		// 热重载菜单（F4快捷键）
		[MenuItem("Tools/热重载 _F4", false, 100)]
		public static void TriggerHotReload()
		{
			// 基础校验
			if (!EditorApplication.isPlaying)
			{
				ArchLog.LogWarning("热重载仅支持在「编辑模式运行」时使用！");
				return;
			}
			if (isReloading)
			{
				ArchLog.LogWarning("当前已在热重载中，请勿重复触发！");
				return;
			}

			// 配置校验（新增引用/源生成器相关校验）
			var config = ArchConfig.Instance;
			if (!ValidateConfig(config))
				return;

			// 开始热重载（异步避免阻塞编辑器）
			isReloading = true;
			Task task = new Task(() =>
			{
				CompileHotfixCode(config); // 编译（整合新配置）
				AssemblyReload(config);    // 加载DLL
			});
			task.Start();
		}

		#region 新增：配置合法性校验（含引用/源生成器）
		private static bool ValidateConfig(ConfigData config)
		{
			// 核心路径校验
			if (string.IsNullOrEmpty(config.hotReloadOutputPath))
			{
				EditorUtility.DisplayDialog("配置缺失", "请先在「Hot Reload Settings」中设置「热重载输出目录」", "确定");
				return false;
			}
			if (config.hotfixSourceDirectories.Count == 0)
			{
				EditorUtility.DisplayDialog("配置缺失", "请先在「Hot Reload Settings」中添加「热更源码目录」", "确定");
				return false;
			}

			// 引用程序集合法性校验（仅警告，不阻断）
			foreach (var path in config.referenceAssemblyPaths)
			{
				if (!File.Exists(path))
				{
					EditorUtility.DisplayDialog("引用警告", $"引用程序集不存在：{path}\n建议删除无效路径", "确定");
				}
			}

			// 源生成器合法性校验（仅警告，不阻断）
			foreach (var path in config.sourceGeneratorPaths)
			{
				if (!File.Exists(path))
				{
					EditorUtility.DisplayDialog("源生成器警告", $"源生成器DLL不存在：{path}\n建议删除无效路径", "确定");
				}
			}

			return true;
		}
		#endregion

		#region 核心编译逻辑（整合引用/源生成器）
		private static void CompileHotfixCode(ConfigData config)
		{
			// 1. 初始化编译器（指定输出目录）
			string outputDir = config.hotReloadOutputPath;
			if (!Directory.Exists(outputDir))
				Directory.CreateDirectory(outputDir);
			var compiler = new AssemblyCompiler(outputDir);

			// 2. 添加「默认引用」（保留Unity基础依赖，避免丢失）
			AddDefaultReferences(compiler);

			// 3. 添加「配置的引用程序集」（用户手动选择的DLL）
			AddConfiguredReferences(compiler, config);

			// 4. 添加「引用解析路径」（自动查找依赖）
			AddReferenceResolvePaths(compiler, config);

			// 5. 添加「配置的源生成器」（从DLL加载生成器实例）
			AddConfiguredSourceGenerators(compiler, config);

			// 6. 添加「热更源码路径」
			AddHotfixSourcePaths(compiler, config);

			// 7. 执行编译
			bool compileSuccess = compiler.CompileAll();
			if (!compileSuccess)
			{
				ArchLog.LogDebug("热更代码编译失败！请查看控制台日志获取详细错误信息");
			}
			ArchLog.LogInfo($"热重载编译成功！DLL路径：{Path.Combine(outputDir, Assemblys.HOTFIX_ASSEMBLY)}");
		}
		#endregion

		#region 新增：加载配置的引用程序集
		private static void AddConfiguredReferences(AssemblyCompiler compiler, ConfigData config)
		{
			if (config.referenceAssemblyPaths.Count == 0)
				return;

			foreach (var assemblyPath in config.referenceAssemblyPaths)
			{
				try
				{
					// 加载DLL并添加为引用
					Assembly referenceAssembly = Assembly.LoadFrom(assemblyPath);
					compiler.AddReferencedAssembly(referenceAssembly);
					ArchLog.LogDebug($"已添加引用程序集：{referenceAssembly.FullName}");
				}
				catch (Exception ex)
				{
					throw new Exception($"加载引用程序集失败：{assemblyPath}\n原因：{ex.Message}");
				}
			}
		}
		#endregion

		#region 新增：添加引用解析路径
		private static void AddReferenceResolvePaths(AssemblyCompiler compiler, ConfigData config)
		{
			if (config.referenceResolvePaths.Count == 0)
				return;

			foreach (var resolvePath in config.referenceResolvePaths)
			{
				if (Directory.Exists(resolvePath))
				{
					compiler.AddSearchDirectory(resolvePath);
				}
				else
				{
					ArchLog.LogWarning($"引用解析路径不存在：{resolvePath}（已跳过）");
				}
			}

			// 额外添加「引用程序集所在目录」作为解析路径（避免依赖缺失）
			var assemblyDirs = new HashSet<string>();
			foreach (var assemblyPath in config.referenceAssemblyPaths)
			{
				string dir = Path.GetDirectoryName(assemblyPath);
				if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
					assemblyDirs.Add(dir);
			}
			foreach (var dir in assemblyDirs)
			{
				compiler.AddSearchDirectory(dir);
			}
		}
		#endregion

		#region 新增：加载配置的源生成器（反射实现）
		private static void AddConfiguredSourceGenerators(AssemblyCompiler compiler, ConfigData config)
		{
			if (config.sourceGeneratorPaths.Count == 0)
				return;

			foreach (var generatorPath in config.sourceGeneratorPaths)
			{
				try
				{
					// 1. 加载源生成器DLL
					if (!File.Exists(generatorPath))
					{
						ArchLog.LogWarning($"源生成器DLL不存在：{generatorPath}（已跳过）");
						continue;
					}
					Assembly generatorAssembly = Assembly.LoadFrom(generatorPath);

					// 2. 反射查找「源生成器类型」（支持ISourceGenerator/IIncrementalGenerator）
					foreach (var type in generatorAssembly.GetTypes())
					{
						// 排除接口、抽象类
						if (type.IsInterface || type.IsAbstract)
							continue;

						// 处理ISourceGenerator（旧版）
						if (typeof(ISourceGenerator).IsAssignableFrom(type))
						{
							ISourceGenerator generator = (ISourceGenerator)Activator.CreateInstance(type);
							compiler.AddSourceGenerator(generator);
						}

						// 处理IIncrementalGenerator（新版）
						if (typeof(IIncrementalGenerator).IsAssignableFrom(type))
						{
							IIncrementalGenerator generator = (IIncrementalGenerator)Activator.CreateInstance(type);
							compiler.AddSourceGenerator(generator);
						}
					}
				}
				catch (Exception ex)
				{
					throw new Exception($"加载源生成器失败：{generatorPath}\n原因：{ex.Message}");
				}
			}
		}
		#endregion

		#region 原有：保留默认引用（Unity基础依赖）
		private static void AddDefaultReferences(AssemblyCompiler compiler)
		{
			// Unity核心依赖
			compiler.AddReferencedAssembly(typeof(object).Assembly);
		}

		#endregion

		#region 原有：添加热更源码路径
		private static void AddHotfixSourcePaths(AssemblyCompiler compiler, ConfigData config)
		{
			foreach (var sourcePath in config.hotfixSourceDirectories)
			{
				if (Directory.Exists(sourcePath))
				{
					compiler.AddCodePath(sourcePath, Assemblys.HOTFIX_ASSEMBLY);
					ArchLog.LogDebug($"已添加热更源码路径：{sourcePath}");
				}
				else
				{
					throw new Exception($"热更源码路径不存在：{sourcePath}");
				}
			}
		}
		#endregion

		#region 原有：加载热更DLL并执行重载
		private static async void AssemblyReload(ConfigData config)
		{
			try
			{
				string hotfixDllPath = Path.Combine(config.hotReloadOutputPath, Assemblys.HOTFIX_ASSEMBLY + ".dll");
				if (!File.Exists(hotfixDllPath))
				{
					await UniTask.SwitchToMainThread();
					EditorUtility.DisplayDialog("热重载", "热重载失败!不存在热重载程序集！", "确定");
					return;
				}

				// 加载DLL字节流（避免文件占用）
				byte[] dllBytes = File.ReadAllBytes(hotfixDllPath);
				Assembly hotfixAssembly = Assembly.Load(dllBytes);
				await GameRoot.HotReload(hotfixAssembly);
			}
			catch (Exception ex)
			{
				Debug.LogError($"加载热更DLL时发生错误：{ex.Message}\n{ex.StackTrace}");
				EditorUtility.DisplayDialog("异常", $"加载失败：{ex.Message}", "确定");
			}
			finally
			{
				isReloading = false;
			}
		}
		#endregion
	}
}
#endif
