using Arch.Tools;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Arch
{
	public static class CustomCompiler
	{
		static public bool Compile(string outputPath, List<string> sourceFiles, List<string> references)
		{
			// 规范化输出路径
			outputPath = Path.GetFullPath(outputPath);

			// 规范化源码路径
			var normalizedSourceFiles = sourceFiles.Select(Path.GetFullPath).ToList();
			var syntaxTrees = normalizedSourceFiles.Select(path =>
			{
				if (!File.Exists(path))
				{
					Console.WriteLine($"错误：源码文件不存在 - {path}");
					return null;
				}
				return CSharpSyntaxTree.ParseText(File.ReadAllText(path), path: path);
			}).Where(tree => tree != null);

			// 规范化引用路径并过滤无效文件
			var normalizedReferences = references.Select(Path.GetFullPath).ToList();
			var metadataReferences = normalizedReferences
				.Where(path => File.Exists(path))
				.Select(path => MetadataReference.CreateFromFile(path))
				.ToList();

			// 后续编译逻辑不变...
			var compilationOptions = new CSharpCompilationOptions(
				OutputKind.DynamicallyLinkedLibrary,
				optimizationLevel: OptimizationLevel.Release,
				allowUnsafe: true
			)
			.WithMetadataImportOptions(MetadataImportOptions.All)
			.WithPlatform(Platform.AnyCpu);

			var compilation = CSharpCompilation.Create(
				assemblyName: Path.GetFileNameWithoutExtension(outputPath),
				syntaxTrees: syntaxTrees,
				references: metadataReferences,
				options: compilationOptions);

			var result = compilation.Emit(outputPath);

			if (!result.Success)
			{
				foreach (var diagnostic in result.Diagnostics)
				{
					var location = diagnostic.Location;
					var lineSpan = location.GetLineSpan();
					var filePath = location.SourceTree?.FilePath;
					var lineNumber = lineSpan.StartLinePosition.Line + 1;
					var columnNumber = lineSpan.StartLinePosition.Character + 1;

					// 构建错误信息主体
					string errorHeader = $"<color=red>[{diagnostic.Id}] {diagnostic.Severity.ToString().ToUpper()}</color>";
					string locationInfo = $"at <color=cyan>{filePath}</color>:line {lineNumber}, column {columnNumber}";
					string errorDetails = $"{errorHeader}\n{locationInfo}\nMessage: {diagnostic.GetMessage()}";

					// 构建上下文信息
					if (location.SourceTree != null && lineSpan.IsValid)
					{
						var textLine = location.SourceTree.GetText().Lines[lineSpan.StartLinePosition.Line];
						string lineContent = textLine.ToString().Trim();
						string pointerLine = new string(' ', columnNumber - 1) + "^";
						errorDetails += $"\nContext:\n{lineContent}\n{pointerLine}";
					}

					// 单次输出完整信息
					ArchLog.Error(errorDetails);
				}
			}

			return result.Success;
		}

	}
}