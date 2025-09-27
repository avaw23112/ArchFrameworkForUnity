#if UNITY_EDITOR
using Arch.Tools;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Arch.Compilation
{
	public class CompilationSettingsEditor : EditorWindow
	{
		// 保存设置的键名常量
		private const string OutputPathKey = "Compilation_OutputPath";
		private const string AssemblyPathsKey = "Compilation_AssemblyPaths";
		private const string CodePathsKey = "Compilation_CodePaths";
		private const string SourceGeneratorPathsKey = "Compilation_SourceGeneratorPaths";
		private const string CodePathDllNamesKey = "Compilation_CodePathDllNames";
		private const string ReferenceResolvePathsKey = "Compilation_ReferenceResolvePaths"; // 新增：引用解析路径

		// 当前会话的设置数据
		private string _outputPath;
		private List<string> _assemblyPaths = new List<string>();
		private List<string> _codePaths = new List<string>();
		private List<string> _codePathDllNames = new List<string>();
		private List<string> _sourceGeneratorPaths = new List<string>();
		private List<string> _referenceResolvePaths = new List<string>(); // 新增：引用解析路径列表

		// 临时输入字段
		private string _newAssemblyPath = "";
		private string _newCodePath = "";
		private string _newCodePathDllName = "";
		private string _newSourceGeneratorPath = "";
		private string _newReferenceResolvePath = ""; // 新增：新引用解析路径输入

		// 滚动位置
		private Vector2 _scrollPosition;

		[MenuItem("Tools/Compilation Settings")]
		public static void ShowWindow()
		{
			var window = GetWindow<CompilationSettingsEditor>("Compilation Settings");
			window.minSize = new Vector2(900, 700);
			window.Show();
		}

		private void OnEnable()
		{
			LoadSettings();
		}

		private void OnGUI()
		{
			_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
			EditorGUILayout.Space(10);

			// 统一使用固定宽度的标签，确保对齐
			int labelWidth = 120;
			EditorGUIUtility.labelWidth = labelWidth;

			// 输出路径设置
			GUILayout.Label("输出路径设置", EditorStyles.boldLabel);
			EditorGUILayout.BeginHorizontal();
			_outputPath = EditorGUILayout.TextField("输出目录", _outputPath);
			if (GUILayout.Button("浏览", GUILayout.Width(60)))
			{
				string selectedPath = EditorUtility.OpenFolderPanel("选择输出路径", _outputPath, "");
				if (!string.IsNullOrEmpty(selectedPath))
				{
					_outputPath = selectedPath;
					SaveSettings();
				}
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space(15);

			// 引用程序集路径设置
			GUILayout.Label("引用程序集路径", EditorStyles.boldLabel);
			EditorGUILayout.HelpBox("已编译的DLL，将基于这些DLL查找其依赖项", MessageType.Info);
			EditorGUILayout.BeginHorizontal();
			_newAssemblyPath = EditorGUILayout.TextField("程序集路径", _newAssemblyPath);
			if (GUILayout.Button("添加", GUILayout.Width(60)))
			{
				AddPath(_assemblyPaths, _newAssemblyPath, true);
				_newAssemblyPath = "";
			}
			if (GUILayout.Button("浏览", GUILayout.Width(60)))
			{
				string selectedPath = EditorUtility.OpenFilePanel("选择程序集", "", "dll;exe");
				AddPath(_assemblyPaths, selectedPath, true);
			}
			EditorGUILayout.EndHorizontal();

			// 显示已添加的引用程序集路径
			ShowPathList(_assemblyPaths, OnRemoveAssemblyPath);
			EditorGUILayout.Space(15);

			// 新增：引用解析路径设置
			GUILayout.Label("引用解析路径", EditorStyles.boldLabel);
			EditorGUILayout.HelpBox("用于查找依赖项的目录，将从这些路径搜索所需的DLL", MessageType.Info);
			EditorGUILayout.BeginHorizontal();
			_newReferenceResolvePath = EditorGUILayout.TextField("解析目录", _newReferenceResolvePath);
			if (GUILayout.Button("添加", GUILayout.Width(60)))
			{
				AddPath(_referenceResolvePaths, _newReferenceResolvePath, false);
				_newReferenceResolvePath = "";
			}
			if (GUILayout.Button("浏览", GUILayout.Width(60)))
			{
				string selectedPath = EditorUtility.OpenFolderPanel("选择解析目录", "", "");
				AddPath(_referenceResolvePaths, selectedPath, false);
			}
			EditorGUILayout.EndHorizontal();

			// 显示已添加的引用解析路径
			ShowPathList(_referenceResolvePaths, OnRemoveReferenceResolvePath);
			EditorGUILayout.Space(15);

			// 代码收集路径设置
			GUILayout.Label("代码收集路径", EditorStyles.boldLabel);
			EditorGUILayout.LabelField("为每个代码路径指定输出DLL名称（不含扩展名）", EditorStyles.miniLabel);
			EditorGUILayout.BeginHorizontal();
			_newCodePath = EditorGUILayout.TextField("代码文件夹", _newCodePath);
			_newCodePathDllName = EditorGUILayout.TextField("DLL名称", _newCodePathDllName, GUILayout.Width(150));
			if (GUILayout.Button("添加", GUILayout.Width(60)))
			{
				AddCodePathWithDllName();
			}
			if (GUILayout.Button("浏览", GUILayout.Width(60)))
			{
				string selectedPath = EditorUtility.OpenFolderPanel("选择代码文件夹", "", "");
				if (!string.IsNullOrEmpty(selectedPath))
				{
					_newCodePath = selectedPath;
				}
			}
			EditorGUILayout.EndHorizontal();

			// 显示已添加的代码路径和对应的DLL名称
			ShowCodePathList();
			EditorGUILayout.Space(15);

			// 源生成器DLL设置
			GUILayout.Label("源生成器DLL", EditorStyles.boldLabel);
			EditorGUILayout.HelpBox("源生成器将在编译时用于生成额外代码", MessageType.Info);
			EditorGUILayout.BeginHorizontal();
			_newSourceGeneratorPath = EditorGUILayout.TextField("生成器路径", _newSourceGeneratorPath);
			if (GUILayout.Button("添加", GUILayout.Width(60)))
			{
				AddPath(_sourceGeneratorPaths, _newSourceGeneratorPath, true);
				_newSourceGeneratorPath = "";
			}
			if (GUILayout.Button("浏览", GUILayout.Width(60)))
			{
				string selectedPath = EditorUtility.OpenFilePanel("选择源生成器DLL", "", "dll");
				AddPath(_sourceGeneratorPaths, selectedPath, true);
			}
			EditorGUILayout.EndHorizontal();

			// 显示已添加的源生成器
			ShowPathList(_sourceGeneratorPaths, OnRemoveSourceGeneratorPath);
			EditorGUILayout.Space(15);

			// 底部操作按钮
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("保存设置", GUILayout.Width(100)))
			{
				SaveSettings();
				EditorUtility.DisplayDialog("成功", "设置已保存", "确定");
			}
			if (GUILayout.Button("清除所有设置", GUILayout.Width(100)))
			{
				if (EditorUtility.DisplayDialog("确认", "确定要清除所有设置吗？", "是", "否"))
				{
					ClearAllSettings();
				}
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space(20);
			EditorGUILayout.EndScrollView();

			// 分隔线
			GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
			EditorGUILayout.Space(10);

			// 底部中央的编译按钮，占80%宽度
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUI.backgroundColor = Color.green;
			if (GUILayout.Button("编译所有代码", GUILayout.Width(Screen.width * 0.8f), GUILayout.Height(40)))
			{
				CompileCode();
			}
			GUI.backgroundColor = Color.white;
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space(10);
		}

		// 添加代码路径和对应的DLL名称
		private void AddCodePathWithDllName()
		{
			if (string.IsNullOrEmpty(_newCodePath))
			{
				EditorUtility.DisplayDialog("错误", "代码路径不能为空", "确定");
				return;
			}

			if (string.IsNullOrEmpty(_newCodePathDllName))
			{
				EditorUtility.DisplayDialog("错误", "DLL名称不能为空", "确定");
				return;
			}

			if (!Directory.Exists(_newCodePath))
			{
				EditorUtility.DisplayDialog("错误", "代码路径不存在", "确定");
				return;
			}

			if (_codePaths.Contains(_newCodePath))
			{
				EditorUtility.DisplayDialog("提示", "该代码路径已存在", "确定");
				return;
			}

			_codePaths.Add(_newCodePath);
			_codePathDllNames.Add(_newCodePathDllName);
			SaveSettings();

			_newCodePath = "";
			_newCodePathDllName = "";
		}

		// 显示代码路径列表及其对应的DLL名称
		private void ShowCodePathList()
		{
			if (_codePaths.Count == 0)
			{
				EditorGUILayout.HelpBox("尚未添加任何代码路径", MessageType.Info);
				return;
			}

			foreach (var i in Enumerable.Range(0, _codePaths.Count))
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("代码路径:", GUILayout.Width(80));
				EditorGUILayout.TextField(_codePaths[i]);
				EditorGUILayout.LabelField("DLL名称:", GUILayout.Width(70));
				EditorGUILayout.TextField(_codePathDllNames[i], GUILayout.Width(150));
				if (GUILayout.Button("删除", GUILayout.Width(60)))
				{
					_codePaths.RemoveAt(i);
					_codePathDllNames.RemoveAt(i);
					SaveSettings();
					break;
				}
				EditorGUILayout.EndHorizontal();
			}
		}

		// 添加路径到列表（避免重复和无效路径）
		private void AddPath(List<string> pathList, string path, bool isFile)
		{
			if (string.IsNullOrEmpty(path))
			{
				EditorUtility.DisplayDialog("错误", "路径不能为空", "确定");
				return;
			}

			bool pathExists = isFile ? File.Exists(path) : Directory.Exists(path);
			if (!pathExists)
			{
				EditorUtility.DisplayDialog("错误", "路径不存在", "确定");
				return;
			}

			if (pathList.Contains(path))
			{
				EditorUtility.DisplayDialog("提示", "该路径已存在", "确定");
				return;
			}

			pathList.Add(path);
			SaveSettings();
		}

		// 显示路径列表并提供删除功能
		private void ShowPathList(List<string> pathList, System.Action<int> onRemove)
		{
			if (pathList.Count == 0)
			{
				EditorGUILayout.HelpBox("尚未添加任何路径", MessageType.Info);
				return;
			}

			foreach (var i in Enumerable.Range(0, pathList.Count))
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.TextField("路径:", pathList[i]);
				if (GUILayout.Button("删除", GUILayout.Width(60)))
				{
					onRemove?.Invoke(i);
					break;
				}
				EditorGUILayout.EndHorizontal();
			}
			EditorGUILayout.Space(5);
		}

		// 移除引用程序集路径
		private void OnRemoveAssemblyPath(int index)
		{
			_assemblyPaths.RemoveAt(index);
			SaveSettings();
		}

		// 新增：移除引用解析路径
		private void OnRemoveReferenceResolvePath(int index)
		{
			_referenceResolvePaths.RemoveAt(index);
			SaveSettings();
		}

		// 移除源生成器路径
		private void OnRemoveSourceGeneratorPath(int index)
		{
			_sourceGeneratorPaths.RemoveAt(index);
			SaveSettings();
		}

		// 保存设置到PlayerPrefs
		private void SaveSettings()
		{
			PlayerPrefs.SetString(OutputPathKey, _outputPath);

			// 保存引用程序集路径
			SavePathList(AssemblyPathsKey, _assemblyPaths);

			// 新增：保存引用解析路径
			SavePathList(ReferenceResolvePathsKey, _referenceResolvePaths);

			// 保存代码路径和对应的DLL名称
			SavePathList(CodePathsKey, _codePaths);
			SavePathList(CodePathDllNamesKey, _codePathDllNames);

			// 保存源生成器路径
			SavePathList(SourceGeneratorPathsKey, _sourceGeneratorPaths);

			PlayerPrefs.Save();
		}

		// 保存路径列表到PlayerPrefs
		private void SavePathList(string key, List<string> paths)
		{
			PlayerPrefs.SetInt(key + "_Count", paths.Count);
			for (int i = 0; i < paths.Count; i++)
			{
				PlayerPrefs.SetString($"{key}_{i}", paths[i]);
			}
		}

		// 从PlayerPrefs加载设置
		private void LoadSettings()
		{
			_outputPath = PlayerPrefs.GetString(OutputPathKey, "");

			// 加载引用程序集路径
			_assemblyPaths = LoadPathList(AssemblyPathsKey);

			// 新增：加载引用解析路径
			_referenceResolvePaths = LoadPathList(ReferenceResolvePathsKey);

			// 加载代码路径和对应的DLL名称
			_codePaths = LoadPathList(CodePathsKey);
			_codePathDllNames = LoadPathList(CodePathDllNamesKey);

			// 确保两个列表长度一致
			if (_codePaths.Count != _codePathDllNames.Count)
			{
				int minCount = Mathf.Min(_codePaths.Count, _codePathDllNames.Count);
				_codePaths = _codePaths.Take(minCount).ToList();
				_codePathDllNames = _codePathDllNames.Take(minCount).ToList();
			}

			// 加载源生成器路径
			_sourceGeneratorPaths = LoadPathList(SourceGeneratorPathsKey);
		}

		// 从PlayerPrefs加载路径列表
		private List<string> LoadPathList(string key)
		{
			List<string> paths = new List<string>();
			int count = PlayerPrefs.GetInt(key + "_Count", 0);

			for (int i = 0; i < count; i++)
			{
				string path = PlayerPrefs.GetString($"{key}_{i}", "");
				if (!string.IsNullOrEmpty(path))
				{
					paths.Add(path);
				}
			}

			return paths;
		}

		// 清除所有设置
		private void ClearAllSettings()
		{
			_outputPath = "";
			_assemblyPaths.Clear();
			_referenceResolvePaths.Clear(); // 新增：清除引用解析路径
			_codePaths.Clear();
			_codePathDllNames.Clear();
			_sourceGeneratorPaths.Clear();

			// 清除PlayerPrefs中的设置
			PlayerPrefs.DeleteKey(OutputPathKey);
			DeletePathListPrefs(AssemblyPathsKey);
			DeletePathListPrefs(ReferenceResolvePathsKey); // 新增：删除引用解析路径设置
			DeletePathListPrefs(CodePathsKey);
			DeletePathListPrefs(CodePathDllNamesKey);
			DeletePathListPrefs(SourceGeneratorPathsKey);

			PlayerPrefs.Save();
		}

		// 删除路径列表的PlayerPrefs数据
		private void DeletePathListPrefs(string key)
		{
			int count = PlayerPrefs.GetInt(key + "_Count", 0);
			PlayerPrefs.DeleteKey(key + "_Count");

			for (int i = 0; i < count; i++)
			{
				PlayerPrefs.DeleteKey($"{key}_{i}");
			}
		}

		// 编译代码的实现 - 改进版本
		private void CompileCode()
		{
			// 检查必要的设置
			if (string.IsNullOrEmpty(_outputPath))
			{
				EditorUtility.DisplayDialog("错误", "请设置输出路径", "确定");
				return;
			}

			if (_codePaths.Count == 0)
			{
				EditorUtility.DisplayDialog("错误", "请添加至少一个代码路径", "确定");
				return;
			}

			if (_assemblyPaths.Count == 0)
			{
				// 询问用户是否在没有引用程序集的情况下继续
				if (!EditorUtility.DisplayDialog("警告", "未添加任何引用程序集，是否继续编译？", "是", "否"))
				{
					return;
				}
			}

			if (_referenceResolvePaths.Count == 0)
			{
				// 询问用户是否在没有引用解析路径的情况下继续
				if (!EditorUtility.DisplayDialog("警告", "未添加任何引用解析路径，可能导致依赖查找失败，是否继续编译？", "是", "否"))
				{
					return;
				}
			}

			try
			{
				// 创建编译实例
				AssemblyCompiler compiler = new AssemblyCompiler(_outputPath);

				// 添加引用程序集（已编译的DLL）
				foreach (string assemblyPath in _assemblyPaths)
				{
					try
					{
						Assembly assembly = Assembly.LoadFrom(assemblyPath);
						compiler.AddReferencedAssembly(assembly);
						Debug.Log($"已添加引用程序集: {assembly.FullName}");
					}
					catch (Exception ex)
					{
						EditorUtility.DisplayDialog("警告", $"加载程序集 {assemblyPath} 失败: {ex.Message}", "确定");
					}
				}

				// 添加引用解析路径（用于查找依赖的DLL）
				foreach (string resolvePath in _referenceResolvePaths)
				{
					compiler.AddSearchDirectory(resolvePath);
				}

				// 添加源生成器
				foreach (string generatorPath in _sourceGeneratorPaths)
				{
					try
					{
						if (File.Exists(generatorPath))
						{
							Assembly generatorAssembly = Assembly.LoadFrom(generatorPath);
							foreach (Type type in generatorAssembly.GetTypes())
							{
								if (typeof(IIncrementalGenerator).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
								{
									IIncrementalGenerator generator = (IIncrementalGenerator)Activator.CreateInstance(type);
									compiler.AddSourceGenerator(generator);
									ArchLog.LogDebug($"已添加源生成器: {type.Name}");
								}
								if (typeof(ISourceGenerator).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
								{
									ISourceGenerator generator = (ISourceGenerator)Activator.CreateInstance(type);
									compiler.AddSourceGenerator(generator);
									ArchLog.LogDebug($"已添加源生成器: {type.Name}");
								}
							}
						}
					}
					catch (Exception ex)
					{
						EditorUtility.DisplayDialog("警告", $"加载源生成器 {generatorPath} 失败: {ex.Message}", "确定");
					}
				}

				// 添加代码路径
				for (int i = 0; i < _codePaths.Count; i++)
				{
					compiler.AddCodePath(_codePaths[i], _codePathDllNames[i]);
				}

				// 添加默认搜索目录（引用程序集所在目录）
				HashSet<string> searchDirs = new HashSet<string>();
				foreach (string assemblyPath in _assemblyPaths)
				{
					string dir = Path.GetDirectoryName(assemblyPath);
					if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
					{
						searchDirs.Add(dir);
					}
				}

				foreach (string dir in searchDirs)
				{
					compiler.AddSearchDirectory(dir);
				}

				// 执行编译
				bool success = compiler.CompileAll();

				if (success)
				{
					EditorUtility.DisplayDialog("成功", "所有代码编译完成", "确定");
				}
				else
				{
					EditorUtility.DisplayDialog("失败", "编译过程中出现错误，请查看控制台日志", "确定");
				}
			}
			catch (Exception ex)
			{
				EditorUtility.DisplayDialog("错误", $"编译失败: {ex.Message}\n{ex.StackTrace}", "确定");
			}
		}

		// 提供公共方法让其他脚本访问设置
		public static string GetOutputPath()
		{
			return PlayerPrefs.GetString(OutputPathKey, "");
		}

		public static List<string> GetAssemblyPaths()
		{
			CompilationSettingsEditor window = GetWindow<CompilationSettingsEditor>();
			return new List<string>(window._assemblyPaths);
		}

		public static List<string> GetCodePaths()
		{
			CompilationSettingsEditor window = GetWindow<CompilationSettingsEditor>();
			return new List<string>(window._codePaths);
		}

		public static List<string> GetSourceGeneratorPaths()
		{
			CompilationSettingsEditor window = GetWindow<CompilationSettingsEditor>();
			return new List<string>(window._sourceGeneratorPaths);
		}

		// 新增：获取引用解析路径
		public static List<string> GetReferenceResolvePaths()
		{
			CompilationSettingsEditor window = GetWindow<CompilationSettingsEditor>();
			return new List<string>(window._referenceResolvePaths);
		}
	}
}

#endif
