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
		// �������õļ�������
		private const string OutputPathKey = "Compilation_OutputPath";
		private const string AssemblyPathsKey = "Compilation_AssemblyPaths";
		private const string CodePathsKey = "Compilation_CodePaths";
		private const string SourceGeneratorPathsKey = "Compilation_SourceGeneratorPaths";
		private const string CodePathDllNamesKey = "Compilation_CodePathDllNames";
		private const string ReferenceResolvePathsKey = "Compilation_ReferenceResolvePaths"; // ���������ý���·��

		// ��ǰ�Ự����������
		private string _outputPath;
		private List<string> _assemblyPaths = new List<string>();
		private List<string> _codePaths = new List<string>();
		private List<string> _codePathDllNames = new List<string>();
		private List<string> _sourceGeneratorPaths = new List<string>();
		private List<string> _referenceResolvePaths = new List<string>(); // ���������ý���·���б�

		// ��ʱ�����ֶ�
		private string _newAssemblyPath = "";
		private string _newCodePath = "";
		private string _newCodePathDllName = "";
		private string _newSourceGeneratorPath = "";
		private string _newReferenceResolvePath = ""; // �����������ý���·������

		// ����λ��
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

			// ͳһʹ�ù̶���ȵı�ǩ��ȷ������
			int labelWidth = 120;
			EditorGUIUtility.labelWidth = labelWidth;

			// ���·������
			GUILayout.Label("���·������", EditorStyles.boldLabel);
			EditorGUILayout.BeginHorizontal();
			_outputPath = EditorGUILayout.TextField("���Ŀ¼", _outputPath);
			if (GUILayout.Button("���", GUILayout.Width(60)))
			{
				string selectedPath = EditorUtility.OpenFolderPanel("ѡ�����·��", _outputPath, "");
				if (!string.IsNullOrEmpty(selectedPath))
				{
					_outputPath = selectedPath;
					SaveSettings();
				}
			}
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space(15);

			// ���ó���·������
			GUILayout.Label("���ó���·��", EditorStyles.boldLabel);
			EditorGUILayout.HelpBox("�ѱ����DLL����������ЩDLL������������", MessageType.Info);
			EditorGUILayout.BeginHorizontal();
			_newAssemblyPath = EditorGUILayout.TextField("����·��", _newAssemblyPath);
			if (GUILayout.Button("���", GUILayout.Width(60)))
			{
				AddPath(_assemblyPaths, _newAssemblyPath, true);
				_newAssemblyPath = "";
			}
			if (GUILayout.Button("���", GUILayout.Width(60)))
			{
				string selectedPath = EditorUtility.OpenFilePanel("ѡ�����", "", "dll;exe");
				AddPath(_assemblyPaths, selectedPath, true);
			}
			EditorGUILayout.EndHorizontal();

			// ��ʾ����ӵ����ó���·��
			ShowPathList(_assemblyPaths, OnRemoveAssemblyPath);
			EditorGUILayout.Space(15);

			// ���������ý���·������
			GUILayout.Label("���ý���·��", EditorStyles.boldLabel);
			EditorGUILayout.HelpBox("���ڲ����������Ŀ¼��������Щ·�����������DLL", MessageType.Info);
			EditorGUILayout.BeginHorizontal();
			_newReferenceResolvePath = EditorGUILayout.TextField("����Ŀ¼", _newReferenceResolvePath);
			if (GUILayout.Button("���", GUILayout.Width(60)))
			{
				AddPath(_referenceResolvePaths, _newReferenceResolvePath, false);
				_newReferenceResolvePath = "";
			}
			if (GUILayout.Button("���", GUILayout.Width(60)))
			{
				string selectedPath = EditorUtility.OpenFolderPanel("ѡ�����Ŀ¼", "", "");
				AddPath(_referenceResolvePaths, selectedPath, false);
			}
			EditorGUILayout.EndHorizontal();

			// ��ʾ����ӵ����ý���·��
			ShowPathList(_referenceResolvePaths, OnRemoveReferenceResolvePath);
			EditorGUILayout.Space(15);

			// �����ռ�·������
			GUILayout.Label("�����ռ�·��", EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Ϊÿ������·��ָ�����DLL���ƣ�������չ����", EditorStyles.miniLabel);
			EditorGUILayout.BeginHorizontal();
			_newCodePath = EditorGUILayout.TextField("�����ļ���", _newCodePath);
			_newCodePathDllName = EditorGUILayout.TextField("DLL����", _newCodePathDllName, GUILayout.Width(150));
			if (GUILayout.Button("���", GUILayout.Width(60)))
			{
				AddCodePathWithDllName();
			}
			if (GUILayout.Button("���", GUILayout.Width(60)))
			{
				string selectedPath = EditorUtility.OpenFolderPanel("ѡ������ļ���", "", "");
				if (!string.IsNullOrEmpty(selectedPath))
				{
					_newCodePath = selectedPath;
				}
			}
			EditorGUILayout.EndHorizontal();

			// ��ʾ����ӵĴ���·���Ͷ�Ӧ��DLL����
			ShowCodePathList();
			EditorGUILayout.Space(15);

			// Դ������DLL����
			GUILayout.Label("Դ������DLL", EditorStyles.boldLabel);
			EditorGUILayout.HelpBox("Դ���������ڱ���ʱ�������ɶ������", MessageType.Info);
			EditorGUILayout.BeginHorizontal();
			_newSourceGeneratorPath = EditorGUILayout.TextField("������·��", _newSourceGeneratorPath);
			if (GUILayout.Button("���", GUILayout.Width(60)))
			{
				AddPath(_sourceGeneratorPaths, _newSourceGeneratorPath, true);
				_newSourceGeneratorPath = "";
			}
			if (GUILayout.Button("���", GUILayout.Width(60)))
			{
				string selectedPath = EditorUtility.OpenFilePanel("ѡ��Դ������DLL", "", "dll");
				AddPath(_sourceGeneratorPaths, selectedPath, true);
			}
			EditorGUILayout.EndHorizontal();

			// ��ʾ����ӵ�Դ������
			ShowPathList(_sourceGeneratorPaths, OnRemoveSourceGeneratorPath);
			EditorGUILayout.Space(15);

			// �ײ�������ť
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("��������", GUILayout.Width(100)))
			{
				SaveSettings();
				EditorUtility.DisplayDialog("�ɹ�", "�����ѱ���", "ȷ��");
			}
			if (GUILayout.Button("�����������", GUILayout.Width(100)))
			{
				if (EditorUtility.DisplayDialog("ȷ��", "ȷ��Ҫ�������������", "��", "��"))
				{
					ClearAllSettings();
				}
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space(20);
			EditorGUILayout.EndScrollView();

			// �ָ���
			GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(1));
			EditorGUILayout.Space(10);

			// �ײ�����ı��밴ť��ռ80%���
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUI.backgroundColor = Color.green;
			if (GUILayout.Button("�������д���", GUILayout.Width(Screen.width * 0.8f), GUILayout.Height(40)))
			{
				CompileCode();
			}
			GUI.backgroundColor = Color.white;
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space(10);
		}

		// ��Ӵ���·���Ͷ�Ӧ��DLL����
		private void AddCodePathWithDllName()
		{
			if (string.IsNullOrEmpty(_newCodePath))
			{
				EditorUtility.DisplayDialog("����", "����·������Ϊ��", "ȷ��");
				return;
			}

			if (string.IsNullOrEmpty(_newCodePathDllName))
			{
				EditorUtility.DisplayDialog("����", "DLL���Ʋ���Ϊ��", "ȷ��");
				return;
			}

			if (!Directory.Exists(_newCodePath))
			{
				EditorUtility.DisplayDialog("����", "����·��������", "ȷ��");
				return;
			}

			if (_codePaths.Contains(_newCodePath))
			{
				EditorUtility.DisplayDialog("��ʾ", "�ô���·���Ѵ���", "ȷ��");
				return;
			}

			_codePaths.Add(_newCodePath);
			_codePathDllNames.Add(_newCodePathDllName);
			SaveSettings();

			_newCodePath = "";
			_newCodePathDllName = "";
		}

		// ��ʾ����·���б����Ӧ��DLL����
		private void ShowCodePathList()
		{
			if (_codePaths.Count == 0)
			{
				EditorGUILayout.HelpBox("��δ����κδ���·��", MessageType.Info);
				return;
			}

			foreach (var i in Enumerable.Range(0, _codePaths.Count))
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("����·��:", GUILayout.Width(80));
				EditorGUILayout.TextField(_codePaths[i]);
				EditorGUILayout.LabelField("DLL����:", GUILayout.Width(70));
				EditorGUILayout.TextField(_codePathDllNames[i], GUILayout.Width(150));
				if (GUILayout.Button("ɾ��", GUILayout.Width(60)))
				{
					_codePaths.RemoveAt(i);
					_codePathDllNames.RemoveAt(i);
					SaveSettings();
					break;
				}
				EditorGUILayout.EndHorizontal();
			}
		}

		// ���·�����б������ظ�����Ч·����
		private void AddPath(List<string> pathList, string path, bool isFile)
		{
			if (string.IsNullOrEmpty(path))
			{
				EditorUtility.DisplayDialog("����", "·������Ϊ��", "ȷ��");
				return;
			}

			bool pathExists = isFile ? File.Exists(path) : Directory.Exists(path);
			if (!pathExists)
			{
				EditorUtility.DisplayDialog("����", "·��������", "ȷ��");
				return;
			}

			if (pathList.Contains(path))
			{
				EditorUtility.DisplayDialog("��ʾ", "��·���Ѵ���", "ȷ��");
				return;
			}

			pathList.Add(path);
			SaveSettings();
		}

		// ��ʾ·���б��ṩɾ������
		private void ShowPathList(List<string> pathList, System.Action<int> onRemove)
		{
			if (pathList.Count == 0)
			{
				EditorGUILayout.HelpBox("��δ����κ�·��", MessageType.Info);
				return;
			}

			foreach (var i in Enumerable.Range(0, pathList.Count))
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.TextField("·��:", pathList[i]);
				if (GUILayout.Button("ɾ��", GUILayout.Width(60)))
				{
					onRemove?.Invoke(i);
					break;
				}
				EditorGUILayout.EndHorizontal();
			}
			EditorGUILayout.Space(5);
		}

		// �Ƴ����ó���·��
		private void OnRemoveAssemblyPath(int index)
		{
			_assemblyPaths.RemoveAt(index);
			SaveSettings();
		}

		// �������Ƴ����ý���·��
		private void OnRemoveReferenceResolvePath(int index)
		{
			_referenceResolvePaths.RemoveAt(index);
			SaveSettings();
		}

		// �Ƴ�Դ������·��
		private void OnRemoveSourceGeneratorPath(int index)
		{
			_sourceGeneratorPaths.RemoveAt(index);
			SaveSettings();
		}

		// �������õ�PlayerPrefs
		private void SaveSettings()
		{
			PlayerPrefs.SetString(OutputPathKey, _outputPath);

			// �������ó���·��
			SavePathList(AssemblyPathsKey, _assemblyPaths);

			// �������������ý���·��
			SavePathList(ReferenceResolvePathsKey, _referenceResolvePaths);

			// �������·���Ͷ�Ӧ��DLL����
			SavePathList(CodePathsKey, _codePaths);
			SavePathList(CodePathDllNamesKey, _codePathDllNames);

			// ����Դ������·��
			SavePathList(SourceGeneratorPathsKey, _sourceGeneratorPaths);

			PlayerPrefs.Save();
		}

		// ����·���б�PlayerPrefs
		private void SavePathList(string key, List<string> paths)
		{
			PlayerPrefs.SetInt(key + "_Count", paths.Count);
			for (int i = 0; i < paths.Count; i++)
			{
				PlayerPrefs.SetString($"{key}_{i}", paths[i]);
			}
		}

		// ��PlayerPrefs��������
		private void LoadSettings()
		{
			_outputPath = PlayerPrefs.GetString(OutputPathKey, "");

			// �������ó���·��
			_assemblyPaths = LoadPathList(AssemblyPathsKey);

			// �������������ý���·��
			_referenceResolvePaths = LoadPathList(ReferenceResolvePathsKey);

			// ���ش���·���Ͷ�Ӧ��DLL����
			_codePaths = LoadPathList(CodePathsKey);
			_codePathDllNames = LoadPathList(CodePathDllNamesKey);

			// ȷ�������б���һ��
			if (_codePaths.Count != _codePathDllNames.Count)
			{
				int minCount = Mathf.Min(_codePaths.Count, _codePathDllNames.Count);
				_codePaths = _codePaths.Take(minCount).ToList();
				_codePathDllNames = _codePathDllNames.Take(minCount).ToList();
			}

			// ����Դ������·��
			_sourceGeneratorPaths = LoadPathList(SourceGeneratorPathsKey);
		}

		// ��PlayerPrefs����·���б�
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

		// �����������
		private void ClearAllSettings()
		{
			_outputPath = "";
			_assemblyPaths.Clear();
			_referenceResolvePaths.Clear(); // ������������ý���·��
			_codePaths.Clear();
			_codePathDllNames.Clear();
			_sourceGeneratorPaths.Clear();

			// ���PlayerPrefs�е�����
			PlayerPrefs.DeleteKey(OutputPathKey);
			DeletePathListPrefs(AssemblyPathsKey);
			DeletePathListPrefs(ReferenceResolvePathsKey); // ������ɾ�����ý���·������
			DeletePathListPrefs(CodePathsKey);
			DeletePathListPrefs(CodePathDllNamesKey);
			DeletePathListPrefs(SourceGeneratorPathsKey);

			PlayerPrefs.Save();
		}

		// ɾ��·���б��PlayerPrefs����
		private void DeletePathListPrefs(string key)
		{
			int count = PlayerPrefs.GetInt(key + "_Count", 0);
			PlayerPrefs.DeleteKey(key + "_Count");

			for (int i = 0; i < count; i++)
			{
				PlayerPrefs.DeleteKey($"{key}_{i}");
			}
		}

		// ��������ʵ�� - �Ľ��汾
		private void CompileCode()
		{
			// ����Ҫ������
			if (string.IsNullOrEmpty(_outputPath))
			{
				EditorUtility.DisplayDialog("����", "���������·��", "ȷ��");
				return;
			}

			if (_codePaths.Count == 0)
			{
				EditorUtility.DisplayDialog("����", "���������һ������·��", "ȷ��");
				return;
			}

			if (_assemblyPaths.Count == 0)
			{
				// ѯ���û��Ƿ���û�����ó��򼯵�����¼���
				if (!EditorUtility.DisplayDialog("����", "δ����κ����ó��򼯣��Ƿ�������룿", "��", "��"))
				{
					return;
				}
			}

			if (_referenceResolvePaths.Count == 0)
			{
				// ѯ���û��Ƿ���û�����ý���·��������¼���
				if (!EditorUtility.DisplayDialog("����", "δ����κ����ý���·�������ܵ�����������ʧ�ܣ��Ƿ�������룿", "��", "��"))
				{
					return;
				}
			}

			try
			{
				// ��������ʵ��
				AssemblyCompiler compiler = new AssemblyCompiler(_outputPath);

				// ������ó��򼯣��ѱ����DLL��
				foreach (string assemblyPath in _assemblyPaths)
				{
					try
					{
						Assembly assembly = Assembly.LoadFrom(assemblyPath);
						compiler.AddReferencedAssembly(assembly);
						Debug.Log($"��������ó���: {assembly.FullName}");
					}
					catch (Exception ex)
					{
						EditorUtility.DisplayDialog("����", $"���س��� {assemblyPath} ʧ��: {ex.Message}", "ȷ��");
					}
				}

				// ������ý���·�������ڲ���������DLL��
				foreach (string resolvePath in _referenceResolvePaths)
				{
					compiler.AddSearchDirectory(resolvePath);
				}

				// ���Դ������
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
									ArchLog.LogDebug($"�����Դ������: {type.Name}");
								}
								if (typeof(ISourceGenerator).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
								{
									ISourceGenerator generator = (ISourceGenerator)Activator.CreateInstance(type);
									compiler.AddSourceGenerator(generator);
									ArchLog.LogDebug($"�����Դ������: {type.Name}");
								}
							}
						}
					}
					catch (Exception ex)
					{
						EditorUtility.DisplayDialog("����", $"����Դ������ {generatorPath} ʧ��: {ex.Message}", "ȷ��");
					}
				}

				// ��Ӵ���·��
				for (int i = 0; i < _codePaths.Count; i++)
				{
					compiler.AddCodePath(_codePaths[i], _codePathDllNames[i]);
				}

				// ���Ĭ������Ŀ¼�����ó�������Ŀ¼��
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

				// ִ�б���
				bool success = compiler.CompileAll();

				if (success)
				{
					EditorUtility.DisplayDialog("�ɹ�", "���д���������", "ȷ��");
				}
				else
				{
					EditorUtility.DisplayDialog("ʧ��", "��������г��ִ�����鿴����̨��־", "ȷ��");
				}
			}
			catch (Exception ex)
			{
				EditorUtility.DisplayDialog("����", $"����ʧ��: {ex.Message}\n{ex.StackTrace}", "ȷ��");
			}
		}

		// �ṩ���������������ű���������
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

		// ��������ȡ���ý���·��
		public static List<string> GetReferenceResolvePaths()
		{
			CompilationSettingsEditor window = GetWindow<CompilationSettingsEditor>();
			return new List<string>(window._referenceResolvePaths);
		}
	}
}

#endif
