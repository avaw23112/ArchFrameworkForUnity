using Arch.Tools;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Arch.Compilation
{
	/// <summary>
	/// 程序集编译器，用于编译C#代码生成DLL
	/// </summary>
	public class AssemblyCompiler
	{
		// 存储需要使用的源生成器实例
		private readonly List<IIncrementalGenerator> m_listIncrementalGenerators = new List<IIncrementalGenerator>();     // 存储需要使用的源生成器实例
		private readonly List<ISourceGenerator> m_listSourceGenerators = new List<ISourceGenerator>();
		// 已编译进Unity的引用程序集
		private readonly List<Assembly> m_pReferencedAssemblies = new List<Assembly>();

		// 用于查找DLL的目录集合
		private readonly List<string> m_pSearchDirectories = new List<string>()
		{
			Path.GetDirectoryName(typeof(object).Assembly.Location),
		};

		// 代码路径与输出DLL名称的映射
		private readonly Dictionary<string, string> m_pCodePaths = new Dictionary<string, string>();

		// 输出根目录
		private string m_szOutputRootPath;

		// 编译后处理动作
		private readonly List<Action<string>> m_pPostCompileActions = new List<Action<string>>();

		// 用于去重已处理的程序集（避免循环引用）
		private readonly HashSet<string> _processedAssemblies = new HashSet<string>();

		// 排除的文件模式
		private readonly HashSet<string> m_pExcludePatterns = new HashSet<string>
		{
			"*.meta",
			"Editor/",
			"Test/"
		};

		/// <summary>
		/// 初始化程序集编译器
		/// </summary>
		/// <param name="szOutputRootPath">输出根目录路径</param>
		public AssemblyCompiler(string szOutputRootPath)
		{
			SetOutRootPath(szOutputRootPath);

		}



		/// <summary>
		/// 初始化程序集编译器
		/// </summary>
		public AssemblyCompiler()
		{

		}
		/// <summary>
		/// 重新实现引用收集逻辑：从已有引用出发，递归查找所有依赖
		/// </summary>
		private List<string> CollectAllReferences()
		{
			_processedAssemblies.Clear(); // 清空上次处理记录
			HashSet<string> referencePaths = new HashSet<string>();

			// 1. 处理已有引用的直接路径
			foreach (var assembly in m_pReferencedAssemblies)
			{
				try
				{
					string assemblyPath = assembly.Location;
					if (!string.IsNullOrEmpty(assemblyPath) && File.Exists(assemblyPath))
					{
						referencePaths.Add(Path.GetFullPath(assemblyPath));
						// 2. 递归查找该引用的所有依赖
						CollectReferencedAssemblies(assembly, referencePaths);
					}
				}
				catch (Exception ex)
				{
					ArchLog.LogWarning($"处理已有引用 [{assembly.FullName}] 时出错: {ex.Message}");
				}
			}

			return referencePaths.ToList();
		}
		/// <summary>
		/// 递归查找程序集的引用依赖
		/// </summary>
		private void CollectReferencedAssemblies(Assembly assembly, HashSet<string> referencePaths)
		{
			string assemblyFullName = assembly.FullName;
			if (_processedAssemblies.Contains(assemblyFullName))
				return; // 已处理过，避免循环引用

			_processedAssemblies.Add(assemblyFullName);

			// 遍历该程序集的所有引用
			foreach (var referencedName in assembly.GetReferencedAssemblies())
			{
				// 3. 到引用解析路径中查找实际的DLL
				string dllPath = FindAssemblyPath(referencedName);
				if (!string.IsNullOrEmpty(dllPath) && File.Exists(dllPath))
				{
					if (referencePaths.Add(dllPath)) // 仅添加新路径
					{
						try
						{
							// 加载找到的DLL，继续递归查找其依赖
							Assembly referencedAssembly = Assembly.LoadFrom(dllPath);
							CollectReferencedAssemblies(referencedAssembly, referencePaths);
						}
						catch (Exception ex)
						{
							ArchLog.LogWarning($"加载依赖DLL [{dllPath}] 失败，跳过其依赖查找: {ex.Message}");
						}
					}
				}
			}
		}

		/// <summary>
		/// 设置输出路径
		/// </summary>
		/// <param name="szOutputRootPath"></param>
		public void SetOutRootPath(string szOutputRootPath)
		{
			m_szOutputRootPath = Path.GetFullPath(szOutputRootPath);
			EnsureDirectoryExists(m_szOutputRootPath);
		}

		/// <summary>
		/// 添加源生成器到编译流程中
		/// </summary>
		/// <param name="generator">源生成器实例（需实现ISourceGenerator接口）</param>
		public void AddSourceGenerator(IIncrementalGenerator generator)
		{
			if (generator != null && !m_listIncrementalGenerators.Contains(generator))
			{
				m_listIncrementalGenerators.Add(generator);
			}
		}

		/// <summary>
		/// 添加源生成器到编译流程中
		/// </summary>
		/// <param name="generator">源生成器实例（需实现ISourceGenerator接口）</param>
		public void AddSourceGenerator(ISourceGenerator generator)
		{
			if (generator != null && !m_listSourceGenerators.Contains(generator))
			{
				m_listSourceGenerators.Add(generator);
			}
		}

		/// <summary>
		/// 批量添加源生成器
		/// </summary>
		public void AddSourceGenerators(IEnumerable<IIncrementalGenerator> generators)
		{
			foreach (var generator in generators)
			{
				AddSourceGenerator(generator);
			}
		}

		/// <summary>
		/// 添加已编译进Unity的引用程序集
		/// </summary>
		/// <param name="pAssembly">程序集对象</param>
		public void AddReferencedAssembly(Assembly pAssembly)
		{
			if (pAssembly == null) return;
			if (!m_pReferencedAssemblies.Contains(pAssembly))
			{
				m_pReferencedAssemblies.Add(pAssembly);
			}
		}

		/// <summary>
		/// 添加DLL搜索目录
		/// </summary>
		/// <param name="szDirectory">目录路径</param>
		public void AddSearchDirectory(string szDirectory)
		{
			if (string.IsNullOrEmpty(szDirectory)) return;

			string szFullPath = Path.GetFullPath(szDirectory);
			if (!m_pSearchDirectories.Contains(szFullPath) && Directory.Exists(szFullPath))
			{
				m_pSearchDirectories.Add(szFullPath);
			}
		}

		/// <summary>
		/// 添加代码路径，每个路径将编译为一个单独的DLL
		/// </summary>
		/// <param name="szCodePath">代码目录路径</param>
		/// <param name="szOutputDllName">输出DLL名称（不含扩展名）</param>
		public void AddCodePath(string szCodePath, string szOutputDllName)
		{
			if (string.IsNullOrEmpty(szCodePath) || string.IsNullOrEmpty(szOutputDllName)) return;

			string szFullPath = Path.GetFullPath(szCodePath);
			if (Directory.Exists(szFullPath) && !m_pCodePaths.ContainsKey(szFullPath))
			{
				m_pCodePaths[szFullPath] = szOutputDllName;
			}
		}

		/// <summary>
		/// 添加编译后处理动作
		/// </summary>
		/// <param name="pAction">处理动作，参数为DLL文件路径</param>
		public void AddPostCompileAction(Action<string> pAction)
		{
			if (pAction != null)
			{
				m_pPostCompileActions.Add(pAction);
			}
		}

		/// <summary>
		/// 设置输出根目录
		/// </summary>
		/// <param name="szOutputPath">输出目录路径</param>
		public void SetOutputRootPath(string szOutputPath)
		{
			if (!string.IsNullOrEmpty(szOutputPath))
			{
				m_szOutputRootPath = Path.GetFullPath(szOutputPath);
				EnsureDirectoryExists(m_szOutputRootPath);
			}
		}

		/// <summary>
		/// 编译所有添加的代码路径
		/// </summary>
		/// <returns>是否全部编译成功</returns>
		public bool CompileAll()
		{
			if (string.IsNullOrEmpty(m_szOutputRootPath))
			{
				ArchLog.LogError("编译工具的输出路径没有设置");
				return false;
			}

			bool bAllSuccess = true;

			foreach (var pCodePath in m_pCodePaths)
			{
				string szCodeDir = pCodePath.Key;
				string szDllName = pCodePath.Value;
				string szOutputPath = Path.Combine(m_szOutputRootPath, $"{szDllName}.dll");

				bool bSuccess = CompileSinglePath(szCodeDir, szOutputPath);
				if (!bSuccess)
				{
					bAllSuccess = false;
					ArchLog.LogError($"编译路径 {szCodeDir} 失败");
				}
			}

			return bAllSuccess;
		}

		/// <summary>
		/// 编译单个代码路径
		/// </summary>
		/// <param name="szCodeDir">代码目录</param>
		/// <param name="szOutputPath">输出DLL路径</param>
		/// <returns>是否编译成功</returns>
		private bool CompileSinglePath(string szCodeDir, string szOutputPath)
		{
			try
			{
				// 收集源码文件
				List<string> pSourceFiles = CollectSourceFiles(szCodeDir);
				if (pSourceFiles.Count == 0)
				{
					ArchLog.LogWarning($"在路径 {szCodeDir} 下未找到任何C#源码文件");
					return false;
				}

				// 收集引用
				List<string> pReferences = CollectAllReferences();
				if (pReferences.Count == 0)
				{
					if (!EditorUtility.DisplayDialog("警告", "未找到任何引用文件，是否继续编译？", "是", "否"))
					{
						return false;
					}
				}

				// 执行编译
				bool bSuccess = Compile(pSourceFiles, pReferences, szOutputPath);

				// 执行编译后处理
				if (bSuccess && File.Exists(szOutputPath))
				{
					ExecutePostCompileActions(szOutputPath);
				}

				return bSuccess;
			}
			catch (Exception pEx)
			{
				ArchLog.LogError($"编译失败: {pEx.Message}\n{pEx.StackTrace}");
				return false;
			}
		}

		/// <summary>
		/// 收集指定目录下的所有源码文件
		/// </summary>
		/// <param name="szCodeDir">代码目录</param>
		/// <returns>源码文件路径列表</returns>
		private List<string> CollectSourceFiles(string szCodeDir)
		{
			List<string> pSourceFiles = new List<string>();

			if (!Directory.Exists(szCodeDir))
			{
				ArchLog.LogError($"代码目录不存在: {szCodeDir}");
				return pSourceFiles;
			}

			foreach (var szFile in Directory.EnumerateFiles(szCodeDir, "*.cs", SearchOption.AllDirectories))
			{
				if (!IsExcluded(szFile, szCodeDir))
				{
					pSourceFiles.Add(Path.GetFullPath(szFile));
				}
			}

			return pSourceFiles;
		}

		/// <summary>
		/// 检查文件是否需要排除
		/// </summary>
		/// <param name="szFilePath">文件路径</param>
		/// <param name="szBaseDir">基准目录</param>
		/// <returns>是否需要排除</returns>
		private bool IsExcluded(string szFilePath, string szBaseDir)
		{
			string szRelativePath = Path.GetRelativePath(szBaseDir, szFilePath);
			szRelativePath = szRelativePath.Replace('/', Path.DirectorySeparatorChar)
										   .Replace('\\', Path.DirectorySeparatorChar);

			return m_pExcludePatterns.Any(szPattern => MatchesPattern(szRelativePath, szPattern));
		}

		/// <summary>
		/// 检查路径是否匹配模式
		/// </summary>
		/// <param name="szPath">路径</param>
		/// <param name="szPattern">模式</param>
		/// <returns>是否匹配</returns>
		private bool MatchesPattern(string szPath, string szPattern)
		{
			string szNormalizedPattern = szPattern.Replace('/', Path.DirectorySeparatorChar)
												 .Replace('\\', Path.DirectorySeparatorChar);

			if (szNormalizedPattern.EndsWith(Path.DirectorySeparatorChar.ToString()))
			{
				string szDirPattern = szNormalizedPattern.TrimEnd(Path.DirectorySeparatorChar);
				return szPath.Split(Path.DirectorySeparatorChar).Any(szPart =>
					szPart.Equals(szDirPattern, StringComparison.OrdinalIgnoreCase));
			}

			if (szNormalizedPattern.StartsWith("*."))
			{
				string szExtension = szNormalizedPattern.Substring(1);
				return Path.GetExtension(szPath).Equals(szExtension, StringComparison.OrdinalIgnoreCase);
			}

			return szPath.IndexOf(szNormalizedPattern, StringComparison.OrdinalIgnoreCase) >= 0;
		}


		/// <summary>
		/// 增强版：从引用解析路径中查找匹配的DLL（严格匹配名称和版本）
		/// </summary>
		private string FindAssemblyPath(AssemblyName targetAssemblyName)
		{
			string fileName = $"{targetAssemblyName.Name}.dll";
			// 优先搜索用户添加的解析路径，再搜索默认路径
			foreach (var dir in m_pSearchDirectories)
			{
				if (!Directory.Exists(dir)) continue;

				// 搜索所有子目录
				var foundFiles = Directory.EnumerateFiles(dir, fileName, SearchOption.AllDirectories)
										  .Select(Path.GetFullPath);

				foreach (var file in foundFiles)
				{
					// 严格匹配版本（如果目标版本不为空）
					if (CheckAssemblyVersion(file, targetAssemblyName.Version))
					{
						return file;
					}
				}
			}
			return null;
		}

		/// <summary>
		/// 检查程序集版本
		/// </summary>
		/// <param name="szAssemblyPath">程序集路径</param>
		/// <param name="pTargetVersion">目标版本</param>
		/// <returns>版本是否匹配</returns>
		private bool CheckAssemblyVersion(string szAssemblyPath, Version pTargetVersion)
		{
			if (pTargetVersion == null) return true;

			try
			{
				var pAssembly = Assembly.LoadFrom(szAssemblyPath);
				return pAssembly.GetName().Version == pTargetVersion;
			}
			catch
			{
				return false;
			}
		}

		private bool Compile(List<string> pSourceFiles, List<string> pReferences, string szOutputPath)
		{
			// 确保输出目录存在
			EnsureDirectoryExists(Path.GetDirectoryName(szOutputPath));

			// 解析原始源文件为语法树
			var originalSyntaxTrees = pSourceFiles
				.Where(szPath => File.Exists(szPath))
				.Select(szPath => CSharpSyntaxTree.ParseText(
					File.ReadAllText(szPath),
					path: szPath
				))
				.ToList();

			// 处理源生成器（核心修改部分）
			List<SyntaxTree> allSyntaxTrees = new List<SyntaxTree>(originalSyntaxTrees);
			if (m_listSourceGenerators.Count > 0)
			{
				// 创建生成器上下文并执行生成器
				var (generatedTrees, generatorDiagnostics) = ExecuteSourceGenerators(originalSyntaxTrees, pReferences);

				// 检查生成器是否产生错误
				if (generatorDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
				{
					foreach (var diag in generatorDiagnostics)
					{
						DisplayDiagnostic(diag);
					}
					return false;
				}

				// 将生成的语法树添加到编译列表
				allSyntaxTrees.AddRange(generatedTrees);
			}

			// 准备引用元数据
			var pMetadataReferences = pReferences
				.Where(szPath => File.Exists(szPath))
				.Select(szPath => MetadataReference.CreateFromFile(szPath))
				.ToList();

			// 配置编译选项
			var pCompilationOptions = new CSharpCompilationOptions(
				OutputKind.DynamicallyLinkedLibrary,
				optimizationLevel: OptimizationLevel.Release,
				allowUnsafe: true
			)
			.WithMetadataImportOptions(MetadataImportOptions.All)
			.WithPlatform(Platform.AnyCpu);

			// 创建编译实例（包含原始代码和生成的代码）
			var pCompilation = CSharpCompilation.Create(
				assemblyName: Path.GetFileNameWithoutExtension(szOutputPath),
				syntaxTrees: allSyntaxTrees,
				references: pMetadataReferences,
				options: pCompilationOptions);

			// 执行编译
			var pResult = pCompilation.Emit(szOutputPath);

			// 处理编译诊断
			if (!pResult.Success)
			{
				foreach (var pDiagnostic in pResult.Diagnostics)
				{
					DisplayDiagnostic(pDiagnostic);
				}
			}

			return pResult.Success;
		}

		/// <summary>
		/// 执行源生成器并返回生成的语法树和诊断信息
		/// </summary>
		private (List<SyntaxTree> generatedTrees, List<Diagnostic> diagnostics) ExecuteSourceGenerators(
			List<SyntaxTree> originalSyntaxTrees,
			List<string> references)
		{
			var generatedTrees = new List<SyntaxTree>();
			var diagnostics = new List<Diagnostic>();

			// 创建临时编译上下文（用于生成器分析）
			var tempCompilation = CSharpCompilation.Create(
				"TempGeneratorCompilation",
				originalSyntaxTrees,
				references.Select(r => MetadataReference.CreateFromFile(r)),
				new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
			);

			//合并源生成器实例集
			List<ISourceGenerator> listSourceGenerators = new List<ISourceGenerator>();
			listSourceGenerators.AddRange(m_listIncrementalGenerators.Select(gen =>
					gen.AsSourceGenerator()));
			listSourceGenerators.AddRange(m_listSourceGenerators);

			// 创建增量生成器驱动
			var generatorDriver = CSharpGeneratorDriver.Create(
				// 核心转化步骤：将 IIncrementalGenerator 包装为 ISourceGenerator
				generators: listSourceGenerators, // 使用 Microsoft.CodeAnalysis 扩展方法
				parseOptions: (CSharpParseOptions)originalSyntaxTrees.FirstOrDefault()?.Options ?? new CSharpParseOptions()
			);// 创建临时编译上下文（用于生成器分析）


			// 执行增量生成流程
			generatorDriver = (CSharpGeneratorDriver)generatorDriver.RunGeneratorsAndUpdateCompilation(
				tempCompilation,
				out var updatedCompilation,
				out var generatorDiags
			);

			// 收集生成的语法树
			foreach (var tree in updatedCompilation.SyntaxTrees)
			{
				// 过滤掉原始语法树，只保留生成的代码
				if (!originalSyntaxTrees.Contains(tree))
				{
					generatedTrees.Add(tree);
				}
			}

			// 收集诊断信息
			diagnostics.AddRange(generatorDiags);

			return (generatedTrees, diagnostics);
		}

		/// <summary>
		/// 显示编译诊断信息
		/// </summary>
		/// <param name="pDiagnostic">诊断信息</param>
		private void DisplayDiagnostic(Diagnostic pDiagnostic)
		{
			var pLocation = pDiagnostic.Location;
			var pLineSpan = pLocation.GetLineSpan();
			string szFilePath = pLocation.SourceTree?.FilePath;
			int nLineNumber = pLineSpan.StartLinePosition.Line + 1;
			int nColumnNumber = pLineSpan.StartLinePosition.Character + 1;

			string szErrorHeader = $"[{pDiagnostic.Id}] {pDiagnostic.Severity.ToString().ToUpper()}";
			string szLocationInfo = $"at {szFilePath}:line {nLineNumber}, column {nColumnNumber}";
			string szErrorDetails = $"{szErrorHeader}\n{szLocationInfo}\nMessage: {pDiagnostic.GetMessage()}";

			if (pLocation.SourceTree != null && pLineSpan.IsValid)
			{
				var pTextLine = pLocation.SourceTree.GetText().Lines[pLineSpan.StartLinePosition.Line];
				string szLineContent = pTextLine.ToString().Trim();
				string szPointerLine = new string(' ', nColumnNumber - 1) + "^";
				szErrorDetails += $"\nContext:\n{szLineContent}\n{szPointerLine}";
			}

			if (pDiagnostic.Severity == DiagnosticSeverity.Error)
			{
				ArchLog.LogError(szErrorDetails);
			}
			else
			{
				ArchLog.LogWarning(szErrorDetails);
			}
		}

		/// <summary>
		/// 执行编译后处理动作
		/// </summary>
		/// <param name="szDllPath">DLL文件路径</param>
		private void ExecutePostCompileActions(string szDllPath)
		{
			foreach (var pAction in m_pPostCompileActions)
			{
				try
				{
					pAction.Invoke(szDllPath);
				}
				catch (Exception pEx)
				{
					ArchLog.LogError($"执行编译后处理时发生错误: {pEx.Message}\n{pEx.StackTrace}");
				}
			}
		}

		/// <summary>
		/// 确保目录存在，不存在则创建
		/// </summary>
		/// <param name="szDirectory">目录路径</param>
		private void EnsureDirectoryExists(string szDirectory)
		{
			if (!string.IsNullOrEmpty(szDirectory) && !Directory.Exists(szDirectory))
			{
				Directory.CreateDirectory(szDirectory);
			}
		}
	}
}
