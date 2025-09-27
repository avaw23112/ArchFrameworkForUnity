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
	/// ���򼯱����������ڱ���C#��������DLL
	/// </summary>
	public class AssemblyCompiler
	{
		// �洢��Ҫʹ�õ�Դ������ʵ��
		private readonly List<IIncrementalGenerator> m_listIncrementalGenerators = new List<IIncrementalGenerator>();     // �洢��Ҫʹ�õ�Դ������ʵ��
		private readonly List<ISourceGenerator> m_listSourceGenerators = new List<ISourceGenerator>();
		// �ѱ����Unity�����ó���
		private readonly List<Assembly> m_pReferencedAssemblies = new List<Assembly>();

		// ���ڲ���DLL��Ŀ¼����
		private readonly List<string> m_pSearchDirectories = new List<string>()
		{
			Path.GetDirectoryName(typeof(object).Assembly.Location),
		};

		// ����·�������DLL���Ƶ�ӳ��
		private readonly Dictionary<string, string> m_pCodePaths = new Dictionary<string, string>();

		// �����Ŀ¼
		private string m_szOutputRootPath;

		// ���������
		private readonly List<Action<string>> m_pPostCompileActions = new List<Action<string>>();

		// ����ȥ���Ѵ���ĳ��򼯣�����ѭ�����ã�
		private readonly HashSet<string> _processedAssemblies = new HashSet<string>();

		// �ų����ļ�ģʽ
		private readonly HashSet<string> m_pExcludePatterns = new HashSet<string>
		{
			"*.meta",
			"Editor/",
			"Test/"
		};

		/// <summary>
		/// ��ʼ�����򼯱�����
		/// </summary>
		/// <param name="szOutputRootPath">�����Ŀ¼·��</param>
		public AssemblyCompiler(string szOutputRootPath)
		{
			SetOutRootPath(szOutputRootPath);

		}



		/// <summary>
		/// ��ʼ�����򼯱�����
		/// </summary>
		public AssemblyCompiler()
		{

		}
		/// <summary>
		/// ����ʵ�������ռ��߼������������ó������ݹ������������
		/// </summary>
		private List<string> CollectAllReferences()
		{
			_processedAssemblies.Clear(); // ����ϴδ����¼
			HashSet<string> referencePaths = new HashSet<string>();

			// 1. �����������õ�ֱ��·��
			foreach (var assembly in m_pReferencedAssemblies)
			{
				try
				{
					string assemblyPath = assembly.Location;
					if (!string.IsNullOrEmpty(assemblyPath) && File.Exists(assemblyPath))
					{
						referencePaths.Add(Path.GetFullPath(assemblyPath));
						// 2. �ݹ���Ҹ����õ���������
						CollectReferencedAssemblies(assembly, referencePaths);
					}
				}
				catch (Exception ex)
				{
					ArchLog.LogWarning($"������������ [{assembly.FullName}] ʱ����: {ex.Message}");
				}
			}

			return referencePaths.ToList();
		}
		/// <summary>
		/// �ݹ���ҳ��򼯵���������
		/// </summary>
		private void CollectReferencedAssemblies(Assembly assembly, HashSet<string> referencePaths)
		{
			string assemblyFullName = assembly.FullName;
			if (_processedAssemblies.Contains(assemblyFullName))
				return; // �Ѵ����������ѭ������

			_processedAssemblies.Add(assemblyFullName);

			// �����ó��򼯵���������
			foreach (var referencedName in assembly.GetReferencedAssemblies())
			{
				// 3. �����ý���·���в���ʵ�ʵ�DLL
				string dllPath = FindAssemblyPath(referencedName);
				if (!string.IsNullOrEmpty(dllPath) && File.Exists(dllPath))
				{
					if (referencePaths.Add(dllPath)) // �������·��
					{
						try
						{
							// �����ҵ���DLL�������ݹ����������
							Assembly referencedAssembly = Assembly.LoadFrom(dllPath);
							CollectReferencedAssemblies(referencedAssembly, referencePaths);
						}
						catch (Exception ex)
						{
							ArchLog.LogWarning($"��������DLL [{dllPath}] ʧ�ܣ���������������: {ex.Message}");
						}
					}
				}
			}
		}

		/// <summary>
		/// �������·��
		/// </summary>
		/// <param name="szOutputRootPath"></param>
		public void SetOutRootPath(string szOutputRootPath)
		{
			m_szOutputRootPath = Path.GetFullPath(szOutputRootPath);
			EnsureDirectoryExists(m_szOutputRootPath);
		}

		/// <summary>
		/// ���Դ������������������
		/// </summary>
		/// <param name="generator">Դ������ʵ������ʵ��ISourceGenerator�ӿڣ�</param>
		public void AddSourceGenerator(IIncrementalGenerator generator)
		{
			if (generator != null && !m_listIncrementalGenerators.Contains(generator))
			{
				m_listIncrementalGenerators.Add(generator);
			}
		}

		/// <summary>
		/// ���Դ������������������
		/// </summary>
		/// <param name="generator">Դ������ʵ������ʵ��ISourceGenerator�ӿڣ�</param>
		public void AddSourceGenerator(ISourceGenerator generator)
		{
			if (generator != null && !m_listSourceGenerators.Contains(generator))
			{
				m_listSourceGenerators.Add(generator);
			}
		}

		/// <summary>
		/// �������Դ������
		/// </summary>
		public void AddSourceGenerators(IEnumerable<IIncrementalGenerator> generators)
		{
			foreach (var generator in generators)
			{
				AddSourceGenerator(generator);
			}
		}

		/// <summary>
		/// ����ѱ����Unity�����ó���
		/// </summary>
		/// <param name="pAssembly">���򼯶���</param>
		public void AddReferencedAssembly(Assembly pAssembly)
		{
			if (pAssembly == null) return;
			if (!m_pReferencedAssemblies.Contains(pAssembly))
			{
				m_pReferencedAssemblies.Add(pAssembly);
			}
		}

		/// <summary>
		/// ���DLL����Ŀ¼
		/// </summary>
		/// <param name="szDirectory">Ŀ¼·��</param>
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
		/// ��Ӵ���·����ÿ��·��������Ϊһ��������DLL
		/// </summary>
		/// <param name="szCodePath">����Ŀ¼·��</param>
		/// <param name="szOutputDllName">���DLL���ƣ�������չ����</param>
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
		/// ��ӱ��������
		/// </summary>
		/// <param name="pAction">������������ΪDLL�ļ�·��</param>
		public void AddPostCompileAction(Action<string> pAction)
		{
			if (pAction != null)
			{
				m_pPostCompileActions.Add(pAction);
			}
		}

		/// <summary>
		/// ���������Ŀ¼
		/// </summary>
		/// <param name="szOutputPath">���Ŀ¼·��</param>
		public void SetOutputRootPath(string szOutputPath)
		{
			if (!string.IsNullOrEmpty(szOutputPath))
			{
				m_szOutputRootPath = Path.GetFullPath(szOutputPath);
				EnsureDirectoryExists(m_szOutputRootPath);
			}
		}

		/// <summary>
		/// ����������ӵĴ���·��
		/// </summary>
		/// <returns>�Ƿ�ȫ������ɹ�</returns>
		public bool CompileAll()
		{
			if (string.IsNullOrEmpty(m_szOutputRootPath))
			{
				ArchLog.LogError("���빤�ߵ����·��û������");
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
					ArchLog.LogError($"����·�� {szCodeDir} ʧ��");
				}
			}

			return bAllSuccess;
		}

		/// <summary>
		/// ���뵥������·��
		/// </summary>
		/// <param name="szCodeDir">����Ŀ¼</param>
		/// <param name="szOutputPath">���DLL·��</param>
		/// <returns>�Ƿ����ɹ�</returns>
		private bool CompileSinglePath(string szCodeDir, string szOutputPath)
		{
			try
			{
				// �ռ�Դ���ļ�
				List<string> pSourceFiles = CollectSourceFiles(szCodeDir);
				if (pSourceFiles.Count == 0)
				{
					ArchLog.LogWarning($"��·�� {szCodeDir} ��δ�ҵ��κ�C#Դ���ļ�");
					return false;
				}

				// �ռ�����
				List<string> pReferences = CollectAllReferences();
				if (pReferences.Count == 0)
				{
					if (!EditorUtility.DisplayDialog("����", "δ�ҵ��κ������ļ����Ƿ�������룿", "��", "��"))
					{
						return false;
					}
				}

				// ִ�б���
				bool bSuccess = Compile(pSourceFiles, pReferences, szOutputPath);

				// ִ�б������
				if (bSuccess && File.Exists(szOutputPath))
				{
					ExecutePostCompileActions(szOutputPath);
				}

				return bSuccess;
			}
			catch (Exception pEx)
			{
				ArchLog.LogError($"����ʧ��: {pEx.Message}\n{pEx.StackTrace}");
				return false;
			}
		}

		/// <summary>
		/// �ռ�ָ��Ŀ¼�µ�����Դ���ļ�
		/// </summary>
		/// <param name="szCodeDir">����Ŀ¼</param>
		/// <returns>Դ���ļ�·���б�</returns>
		private List<string> CollectSourceFiles(string szCodeDir)
		{
			List<string> pSourceFiles = new List<string>();

			if (!Directory.Exists(szCodeDir))
			{
				ArchLog.LogError($"����Ŀ¼������: {szCodeDir}");
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
		/// ����ļ��Ƿ���Ҫ�ų�
		/// </summary>
		/// <param name="szFilePath">�ļ�·��</param>
		/// <param name="szBaseDir">��׼Ŀ¼</param>
		/// <returns>�Ƿ���Ҫ�ų�</returns>
		private bool IsExcluded(string szFilePath, string szBaseDir)
		{
			string szRelativePath = Path.GetRelativePath(szBaseDir, szFilePath);
			szRelativePath = szRelativePath.Replace('/', Path.DirectorySeparatorChar)
										   .Replace('\\', Path.DirectorySeparatorChar);

			return m_pExcludePatterns.Any(szPattern => MatchesPattern(szRelativePath, szPattern));
		}

		/// <summary>
		/// ���·���Ƿ�ƥ��ģʽ
		/// </summary>
		/// <param name="szPath">·��</param>
		/// <param name="szPattern">ģʽ</param>
		/// <returns>�Ƿ�ƥ��</returns>
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
		/// ��ǿ�棺�����ý���·���в���ƥ���DLL���ϸ�ƥ�����ƺͰ汾��
		/// </summary>
		private string FindAssemblyPath(AssemblyName targetAssemblyName)
		{
			string fileName = $"{targetAssemblyName.Name}.dll";
			// ���������û���ӵĽ���·����������Ĭ��·��
			foreach (var dir in m_pSearchDirectories)
			{
				if (!Directory.Exists(dir)) continue;

				// ����������Ŀ¼
				var foundFiles = Directory.EnumerateFiles(dir, fileName, SearchOption.AllDirectories)
										  .Select(Path.GetFullPath);

				foreach (var file in foundFiles)
				{
					// �ϸ�ƥ��汾�����Ŀ��汾��Ϊ�գ�
					if (CheckAssemblyVersion(file, targetAssemblyName.Version))
					{
						return file;
					}
				}
			}
			return null;
		}

		/// <summary>
		/// �����򼯰汾
		/// </summary>
		/// <param name="szAssemblyPath">����·��</param>
		/// <param name="pTargetVersion">Ŀ��汾</param>
		/// <returns>�汾�Ƿ�ƥ��</returns>
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
			// ȷ�����Ŀ¼����
			EnsureDirectoryExists(Path.GetDirectoryName(szOutputPath));

			// ����ԭʼԴ�ļ�Ϊ�﷨��
			var originalSyntaxTrees = pSourceFiles
				.Where(szPath => File.Exists(szPath))
				.Select(szPath => CSharpSyntaxTree.ParseText(
					File.ReadAllText(szPath),
					path: szPath
				))
				.ToList();

			// ����Դ�������������޸Ĳ��֣�
			List<SyntaxTree> allSyntaxTrees = new List<SyntaxTree>(originalSyntaxTrees);
			if (m_listSourceGenerators.Count > 0)
			{
				// ���������������Ĳ�ִ��������
				var (generatedTrees, generatorDiagnostics) = ExecuteSourceGenerators(originalSyntaxTrees, pReferences);

				// ����������Ƿ��������
				if (generatorDiagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
				{
					foreach (var diag in generatorDiagnostics)
					{
						DisplayDiagnostic(diag);
					}
					return false;
				}

				// �����ɵ��﷨����ӵ������б�
				allSyntaxTrees.AddRange(generatedTrees);
			}

			// ׼������Ԫ����
			var pMetadataReferences = pReferences
				.Where(szPath => File.Exists(szPath))
				.Select(szPath => MetadataReference.CreateFromFile(szPath))
				.ToList();

			// ���ñ���ѡ��
			var pCompilationOptions = new CSharpCompilationOptions(
				OutputKind.DynamicallyLinkedLibrary,
				optimizationLevel: OptimizationLevel.Release,
				allowUnsafe: true
			)
			.WithMetadataImportOptions(MetadataImportOptions.All)
			.WithPlatform(Platform.AnyCpu);

			// ��������ʵ��������ԭʼ��������ɵĴ��룩
			var pCompilation = CSharpCompilation.Create(
				assemblyName: Path.GetFileNameWithoutExtension(szOutputPath),
				syntaxTrees: allSyntaxTrees,
				references: pMetadataReferences,
				options: pCompilationOptions);

			// ִ�б���
			var pResult = pCompilation.Emit(szOutputPath);

			// ����������
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
		/// ִ��Դ���������������ɵ��﷨���������Ϣ
		/// </summary>
		private (List<SyntaxTree> generatedTrees, List<Diagnostic> diagnostics) ExecuteSourceGenerators(
			List<SyntaxTree> originalSyntaxTrees,
			List<string> references)
		{
			var generatedTrees = new List<SyntaxTree>();
			var diagnostics = new List<Diagnostic>();

			// ������ʱ���������ģ�����������������
			var tempCompilation = CSharpCompilation.Create(
				"TempGeneratorCompilation",
				originalSyntaxTrees,
				references.Select(r => MetadataReference.CreateFromFile(r)),
				new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
			);

			//�ϲ�Դ������ʵ����
			List<ISourceGenerator> listSourceGenerators = new List<ISourceGenerator>();
			listSourceGenerators.AddRange(m_listIncrementalGenerators.Select(gen =>
					gen.AsSourceGenerator()));
			listSourceGenerators.AddRange(m_listSourceGenerators);

			// ������������������
			var generatorDriver = CSharpGeneratorDriver.Create(
				// ����ת�����裺�� IIncrementalGenerator ��װΪ ISourceGenerator
				generators: listSourceGenerators, // ʹ�� Microsoft.CodeAnalysis ��չ����
				parseOptions: (CSharpParseOptions)originalSyntaxTrees.FirstOrDefault()?.Options ?? new CSharpParseOptions()
			);// ������ʱ���������ģ�����������������


			// ִ��������������
			generatorDriver = (CSharpGeneratorDriver)generatorDriver.RunGeneratorsAndUpdateCompilation(
				tempCompilation,
				out var updatedCompilation,
				out var generatorDiags
			);

			// �ռ����ɵ��﷨��
			foreach (var tree in updatedCompilation.SyntaxTrees)
			{
				// ���˵�ԭʼ�﷨����ֻ�������ɵĴ���
				if (!originalSyntaxTrees.Contains(tree))
				{
					generatedTrees.Add(tree);
				}
			}

			// �ռ������Ϣ
			diagnostics.AddRange(generatorDiags);

			return (generatedTrees, diagnostics);
		}

		/// <summary>
		/// ��ʾ���������Ϣ
		/// </summary>
		/// <param name="pDiagnostic">�����Ϣ</param>
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
		/// ִ�б��������
		/// </summary>
		/// <param name="szDllPath">DLL�ļ�·��</param>
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
					ArchLog.LogError($"ִ�б������ʱ��������: {pEx.Message}\n{pEx.StackTrace}");
				}
			}
		}

		/// <summary>
		/// ȷ��Ŀ¼���ڣ��������򴴽�
		/// </summary>
		/// <param name="szDirectory">Ŀ¼·��</param>
		private void EnsureDirectoryExists(string szDirectory)
		{
			if (!string.IsNullOrEmpty(szDirectory) && !Directory.Exists(szDirectory))
			{
				Directory.CreateDirectory(szDirectory);
			}
		}
	}
}
