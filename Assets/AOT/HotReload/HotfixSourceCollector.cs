using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Arch
{
	public class HotfixSourceCollector
	{
		// 热更源码根目录（可在框架配置中修改）
		public static string HotfixRootDir = Path.Combine("Assets", "HotFix", "Logic");

		// 需排除的文件/目录（支持通配符）
		private static readonly HashSet<string> ExcludePatterns = new()
		{
			"*.meta",          // Unity元文件
			"Editor/",         // 编辑器代码（不参与热更，重载）
			"Test/",           // 测试代码（不参与热更，重载）
		};

		/// <summary>
		/// 扫描并收集所有热更源码文件路径
		/// </summary>
		public static List<string> CollectSourceFiles()
		{
			var sourceFiles = new List<string>();

			if (!Directory.Exists(HotfixRootDir))
			{
				Debug.LogError($"热更目录不存在：{HotfixRootDir}");
				return sourceFiles;
			}

			// 递归扫描所有.cs文件
			foreach (var file in Directory.EnumerateFiles(HotfixRootDir, "*.cs", SearchOption.AllDirectories))
			{
				// 过滤需排除的文件
				if (IsExcluded(file))
					continue;

				sourceFiles.Add(file);
			}
			return sourceFiles;
		}

		// 修改 IsExcluded 方法：统一相对路径的分隔符为系统默认
		private static bool IsExcluded(string filePath)
		{
			// 转换为相对路径，并统一分隔符
			string relativePath = Path.GetRelativePath(Application.dataPath, filePath);
			// 将路径中的分隔符统一为系统默认（替换所有/为\或反之）
			relativePath = relativePath.Replace('/', Path.DirectorySeparatorChar)
									   .Replace('\\', Path.DirectorySeparatorChar);
			return ExcludePatterns.Any(pattern => MatchesPattern(relativePath, pattern));
		}

		// 修改 MatchesPattern 方法：适配统一后的分隔符
		private static bool MatchesPattern(string path, string pattern)
		{
			// 统一模式中的分隔符（例如将"Editor/"转为"Editor\"或"Editor/"，取决于系统）
			string normalizedPattern = pattern.Replace('/', Path.DirectorySeparatorChar)
											 .Replace('\\', Path.DirectorySeparatorChar);

			// 处理目录排除（如"Editor/"）
			if (normalizedPattern.EndsWith(Path.DirectorySeparatorChar.ToString()))
			{
				string dirPattern = normalizedPattern.TrimEnd(Path.DirectorySeparatorChar);
				// 检查路径是否包含目标目录（支持多级目录）
				return path.Split(Path.DirectorySeparatorChar).Any(part =>
					part.Equals(dirPattern, StringComparison.OrdinalIgnoreCase));
			}
			// 处理文件通配符（如"*.meta"）
			if (normalizedPattern.StartsWith("*."))
			{
				string extension = normalizedPattern.Substring(1);
				return Path.GetExtension(path).Equals(extension, StringComparison.OrdinalIgnoreCase);
			}
			// 其他精确匹配
			return path.IndexOf(normalizedPattern, StringComparison.OrdinalIgnoreCase) >= 0;
		}

	}

}
