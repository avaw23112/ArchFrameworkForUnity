#if UNITY_EDITOR
using Assets.Scripts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Arch.Editor
{
	public static class HotfixCompilerEditor
	{
		// 在Unity编辑器菜单中添加编译选项
		[MenuItem("Tools/HotReload", false, 100)]
		public static void HotReload()
		{
			CompileHotfixCode();
			AssemblyReload();
		}

		private static void AssemblyReload()
		{
			// 3. 设置输出路径
			string hotReloadDir = Path.Combine(Application.dataPath, "..\\HotfixOutput");
			string hotfixDll = Path.Combine(hotReloadDir, "Hotfix.dll");

			// 加载Assembly
			try
			{
				if (!File.Exists(hotfixDll))
				{
					EditorUtility.DisplayDialog("错误", $"DLL文件不存在：{hotfixDll}", "确定");
					return;
				}

				// 加载程序集
				byte[] rawBytes = File.ReadAllBytes(hotfixDll);
				Assembly hotfixAssembly = Assembly.Load(rawBytes);
				GameRoot.HotReload(hotfixAssembly);
			}
			catch (Exception ex)
			{
				Debug.LogError($"加载热更DLL时发生错误：{ex.Message}\n{ex.StackTrace}");
				EditorUtility.DisplayDialog("异常", $"加载失败：{ex.Message}", "确定");
			}
		}

		public static void CompileHotfixCode()
		{
			// 1. 收集热更源码文件
			var sourceFiles = HotfixSourceCollector.CollectSourceFiles();
			if (sourceFiles.Count == 0)
			{
				EditorUtility.DisplayDialog("警告", "未找到任何热更源码文件", "确定");
				return;
			}

			// 2. 收集引用文件
			var references = HotfixReferenceCollector.CollectAndSaveReferences();
			if (references.Count == 0)
			{
				if (EditorUtility.DisplayDialog("警告", "未找到任何引用文件，是否继续编译？", "是", "否"))
				{
					references = new List<string>();
				}
				else
				{
					return;
				}
			}

			// 3. 设置输出路径
			string outputDir = Path.Combine(Application.dataPath, "../HotfixOutput");
			string outputPath = Path.Combine(outputDir, "Hotfix.dll");

			// 确保输出目录存在
			if (!Directory.Exists(outputDir))
			{
				Directory.CreateDirectory(outputDir);
			}
			bool success = CustomCompiler.Compile(outputPath, sourceFiles, references);
			if (success)
			{
				EditorUtility.DisplayDialog("成功", $"热更代码编译成功！\n输出路径：{outputPath}", "确定");
			}
			else
			{
				EditorUtility.DisplayDialog("失败", "热更代码编译失败，请查看控制台日志", "确定");
			}
		}
	}
}
#endif