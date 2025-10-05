using Arch.Tools;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Arch.Compilation
{
	public class AssemblyCompiler
	{
		private readonly List<IIncrementalGenerator> _incrementalGenerators = new List<IIncrementalGenerator>();
		private readonly List<ISourceGenerator> _sourceGenerators = new List<ISourceGenerator>();
		private readonly List<Assembly> _referencedAssemblies = new List<Assembly>();

		private readonly List<string> _searchDirectories = new List<string>
		{
			Path.GetDirectoryName(typeof(object).Assembly.Location)
		};

		private readonly Dictionary<string, string> _codePaths = new Dictionary<string, string>();
		private readonly List<Action<string>> _postCompileActions = new List<Action<string>>();
		private readonly HashSet<string> _processedAssemblies = new HashSet<string>();

		private readonly HashSet<string> _excludePatterns = new HashSet<string>
		{
			"*.meta",
			"Editor/",
			"Test/"
		};

		private string _outputRootPath;

		public AssemblyCompiler(string outputRootPath)
		{
			SetOutputRootPath(outputRootPath);
		}

		public void SetOutputRootPath(string outputRootPath)
		{
			if (!string.IsNullOrEmpty(outputRootPath))
			{
				_outputRootPath = Path.GetFullPath(outputRootPath);
				EnsureDirectoryExists(_outputRootPath);
			}
		}

		public void AddSourceGenerator(IIncrementalGenerator generator)
		{
			if (generator != null && !_incrementalGenerators.Contains(generator))
			{
				_incrementalGenerators.Add(generator);
			}
		}

		public void AddSourceGenerator(ISourceGenerator generator)
		{
			if (generator != null && !_sourceGenerators.Contains(generator))
			{
				_sourceGenerators.Add(generator);
			}
		}

		public void AddReferencedAssembly(Assembly assembly)
		{
			if (assembly != null && !_referencedAssemblies.Contains(assembly))
			{
				_referencedAssemblies.Add(assembly);
			}
		}

		public void AddSearchDirectory(string directory)
		{
			if (string.IsNullOrEmpty(directory)) return;

			string fullPath = Path.GetFullPath(directory);
			if (!_searchDirectories.Contains(fullPath) && Directory.Exists(fullPath))
			{
				_searchDirectories.Add(fullPath);
			}
		}

		public void AddCodePath(string codePath, string outputDllName)
		{
			if (string.IsNullOrEmpty(codePath) || string.IsNullOrEmpty(outputDllName)) return;

			string fullPath = Path.GetFullPath(codePath);
			if (Directory.Exists(fullPath) && !_codePaths.ContainsKey(fullPath))
			{
				_codePaths[fullPath] = outputDllName;
			}
		}

		public void AddPostCompileAction(Action<string> action)
		{
			if (action != null)
			{
				_postCompileActions.Add(action);
			}
		}

		public bool CompileAll()
		{
			if (string.IsNullOrEmpty(_outputRootPath))
			{
				ArchLog.LogError("编译输出路径未设置");
				return false;
			}

			bool allSuccess = true;

			foreach (var codePath in _codePaths)
			{
				string codeDir = codePath.Key;
				string dllName = codePath.Value;
				string outputPath = Path.Combine(_outputRootPath, $"{dllName}.dll");

				bool success = CompileSinglePath(codeDir, outputPath);
				if (!success)
				{
					allSuccess = false;
					ArchLog.LogError($"代码路径 {codeDir} 编译失败");
				}
			}

			return allSuccess;
		}

		private bool CompileSinglePath(string codeDir, string outputPath)
		{
			try
			{
				List<string> sourceFiles = CollectSourceFiles(codeDir);
				if (sourceFiles.Count == 0)
				{
					ArchLog.LogWarning($"代码目录 {codeDir} 中未找到任何C#源文件");
					return false;
				}

				List<string> references = CollectAllReferences();

				bool success = Compile(sourceFiles, references, outputPath);

				if (success && File.Exists(outputPath))
				{
					ExecutePostCompileActions(outputPath);
				}

				return success;
			}
			catch (Exception ex)
			{
				ArchLog.LogError($"编译失败: {ex.Message}\n{ex.StackTrace}");
				return false;
			}
		}

		private List<string> CollectSourceFiles(string codeDir)
		{
			List<string> sourceFiles = new List<string>();

			if (!Directory.Exists(codeDir))
			{
				ArchLog.LogError($"代码目录不存在: {codeDir}");
				return sourceFiles;
			}

			foreach (var file in Directory.EnumerateFiles(codeDir, "*.cs", SearchOption.AllDirectories))
			{
				if (!IsExcluded(file, codeDir))
				{
					sourceFiles.Add(Path.GetFullPath(file));
				}
			}

			return sourceFiles;
		}

		private bool IsExcluded(string filePath, string baseDir)
		{
			string relativePath = Path.GetRelativePath(baseDir, filePath);
			relativePath = relativePath.Replace('/', Path.DirectorySeparatorChar)
									  .Replace('\\', Path.DirectorySeparatorChar);

			return _excludePatterns.Any(pattern => MatchesPattern(relativePath, pattern));
		}

		private bool MatchesPattern(string path, string pattern)
		{
			// 简单的模式匹配实现
			if (pattern.Contains("*"))
			{
				string[] parts = pattern.Split('*');
				if (parts.Length == 2 && parts[0] == "" && parts[1] != "")
				{
					return path.EndsWith(parts[1], StringComparison.OrdinalIgnoreCase);
				}
			}
			return path.Contains(pattern, StringComparison.OrdinalIgnoreCase);
		}

		private List<string> CollectAllReferences()
		{
			_processedAssemblies.Clear();
			HashSet<string> referencePaths = new HashSet<string>();

			foreach (var assembly in _referencedAssemblies)
			{
				try
				{
					string assemblyPath = assembly.Location;
					if (!string.IsNullOrEmpty(assemblyPath) && File.Exists(assemblyPath))
					{
						referencePaths.Add(Path.GetFullPath(assemblyPath));
						CollectReferencedAssemblies(assembly, referencePaths);
					}
				}
				catch (Exception ex)
				{
					ArchLog.LogWarning($"处理程序集 [{assembly.FullName}] 时出错: {ex.Message}");
				}
			}

			return referencePaths.ToList();
		}

		private void CollectReferencedAssemblies(Assembly assembly, HashSet<string> referencePaths)
		{
			string assemblyFullName = assembly.FullName;
			if (_processedAssemblies.Contains(assemblyFullName))
				return;

			_processedAssemblies.Add(assemblyFullName);

			foreach (var referencedName in assembly.GetReferencedAssemblies())
			{
				string dllPath = FindAssemblyPath(referencedName);
				if (!string.IsNullOrEmpty(dllPath) && File.Exists(dllPath))
				{
					if (referencePaths.Add(dllPath))
					{
						try
						{
							Assembly referencedAssembly = Assembly.LoadFrom(dllPath);
							CollectReferencedAssemblies(referencedAssembly, referencePaths);
						}
						catch (Exception ex)
						{
							ArchLog.LogWarning($"加载DLL [{dllPath}] 失败，可能影响编译: {ex.Message}");
						}
					}
				}
			}
		}

		private string FindAssemblyPath(AssemblyName assemblyName)
		{
			string fileName = $"{assemblyName.Name}.dll";

			// 首先在搜索目录中查找
			foreach (var dir in _searchDirectories)
			{
				if (!Directory.Exists(dir)) continue;

				string filePath = Path.Combine(dir, fileName);
				if (File.Exists(filePath) && CheckAssemblyVersion(filePath, assemblyName.Version))
				{
					return filePath;
				}
			}

			// 搜索子目录
			foreach (var dir in _searchDirectories)
			{
				if (!Directory.Exists(dir)) continue;

				var foundFiles = Directory.EnumerateFiles(dir, fileName, SearchOption.AllDirectories)
										  .Select(Path.GetFullPath);

				foreach (var file in foundFiles)
				{
					if (CheckAssemblyVersion(file, assemblyName.Version))
					{
						return file;
					}
				}
			}
			return null;
		}

		private bool CheckAssemblyVersion(string assemblyPath, Version targetVersion)
		{
			if (targetVersion == null) return true;

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

		public bool Compile(List<string> sourceFiles, List<string> references, string outputPath)
		{
			EnsureDirectoryExists(Path.GetDirectoryName(outputPath));

			var originalSyntaxTrees = sourceFiles
				.Where(path => File.Exists(path))
				.Select(path => CSharpSyntaxTree.ParseText(
					File.ReadAllText(path),
					path: path
				))
				.ToList();

			List<SyntaxTree> allSyntaxTrees = new List<SyntaxTree>(originalSyntaxTrees);

			if (_sourceGenerators.Count > 0 || _incrementalGenerators.Count > 0)
			{
				var (generatedTrees, generatorDiagnostics) = ExecuteSourceGenerators(originalSyntaxTrees, references);

				if (generatorDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
				{
					foreach (var diag in generatorDiagnostics)
					{
						DisplayDiagnostic(diag);
					}
					return false;
				}

				allSyntaxTrees.AddRange(generatedTrees);
			}

			var metadataReferences = references
			.Where(path => File.Exists(path))
			.Select(path => MetadataReference.CreateFromFile(
				path,
				new MetadataReferenceProperties(
					MetadataImageKind.Assembly,
					embedInteropTypes: false // 嵌入完整元数据
					))).ToList();

			var compilationOptions = new CSharpCompilationOptions(
				OutputKind.DynamicallyLinkedLibrary,
				optimizationLevel: OptimizationLevel.Release,
				allowUnsafe: true
			)
			.WithMetadataImportOptions(MetadataImportOptions.All)
			.WithPlatform(Platform.AnyCpu)
			.WithSpecificDiagnosticOptions(
			new Dictionary<string, ReportDiagnostic>
			{
				// 关键修改点2：禁用修剪警告
				{ "IL2026", ReportDiagnostic.Suppress },
				{ "IL2075", ReportDiagnostic.Suppress }
			});

			var compilation = CSharpCompilation.Create(
				assemblyName: Path.GetFileNameWithoutExtension(outputPath),
				syntaxTrees: allSyntaxTrees,
				references: metadataReferences,
				options: compilationOptions);

			var result = compilation.Emit(outputPath);

			if (!result.Success)
			{
				foreach (var diagnostic in result.Diagnostics)
				{
					DisplayDiagnostic(diagnostic);
				}
			}

			return result.Success;
		}

		private (List<SyntaxTree> generatedTrees, List<Diagnostic> diagnostics) ExecuteSourceGenerators(
			List<SyntaxTree> originalSyntaxTrees,
			List<string> references)
		{
			var generatedTrees = new List<SyntaxTree>();
			var diagnostics = new List<Diagnostic>();

			var tempCompilation = CSharpCompilation.Create(
				"TempGeneratorCompilation",
				originalSyntaxTrees,
				references.Select(r => MetadataReference.CreateFromFile(r)),
				new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
			);

			List<ISourceGenerator> allGenerators = new List<ISourceGenerator>();
			allGenerators.AddRange(_sourceGenerators);
			allGenerators.AddRange(_incrementalGenerators.Select(gen => gen.AsSourceGenerator()));

			var generatorDriver = CSharpGeneratorDriver.Create(
				generators: allGenerators,
				parseOptions: (CSharpParseOptions)originalSyntaxTrees.FirstOrDefault()?.Options ?? new CSharpParseOptions()
			);

			generatorDriver = (CSharpGeneratorDriver)generatorDriver.RunGeneratorsAndUpdateCompilation(
				tempCompilation,
				out var updatedCompilation,
				out var generatorDiags
			);

			foreach (var tree in updatedCompilation.SyntaxTrees)
			{
				if (!originalSyntaxTrees.Contains(tree))
				{
					generatedTrees.Add(tree);
				}
			}

			diagnostics.AddRange(generatorDiags);

			return (generatedTrees, diagnostics);
		}

		private void DisplayDiagnostic(Diagnostic diagnostic)
		{
			var location = diagnostic.Location;
			var lineSpan = location.GetLineSpan();
			string filePath = location.SourceTree?.FilePath;
			int lineNumber = lineSpan.StartLinePosition.Line + 1;
			int columnNumber = lineSpan.StartLinePosition.Character + 1;

			string errorHeader = $"[{diagnostic.Id}] {diagnostic.Severity.ToString().ToUpper()}";
			string locationInfo = $"at {filePath}:line {lineNumber}, column {columnNumber}";
			string errorDetails = $"{errorHeader}\n{locationInfo}\nMessage: {diagnostic.GetMessage()}";

			if (location.SourceTree != null && lineSpan.IsValid)
			{
				var textLine = location.SourceTree.GetText().Lines[lineSpan.StartLinePosition.Line];
				string lineContent = textLine.ToString().Trim();
				string pointerLine = new string(' ', columnNumber - 1) + "^";
				errorDetails += $"\nContext:\n{lineContent}\n{pointerLine}";
			}

			if (diagnostic.Severity == DiagnosticSeverity.Error)
			{
				ArchLog.LogError(errorDetails);
			}
			else
			{
				ArchLog.LogWarning(errorDetails);
			}
		}

		private void ExecutePostCompileActions(string dllPath)
		{
			foreach (var action in _postCompileActions)
			{
				try
				{
					action.Invoke(dllPath);
				}
				catch (Exception ex)
				{
					ArchLog.LogError($"执行编译后操作时发生错误: {ex.Message}\n{ex.StackTrace}");
				}
			}
		}

		private void EnsureDirectoryExists(string directory)
		{
			if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}
		}
	}
}