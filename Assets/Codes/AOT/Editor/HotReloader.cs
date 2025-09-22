#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Arch.Editor
{
	public static class HotfixCompilerEditor
	{
		// 在Unity编辑器菜单中添加编译选项
		[MenuItem("Tools/Compile Hotfix Code", false, 100)]
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
				AssetDatabase.Refresh();
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