#if UNITY_EDITOR

using System.IO;
using UnityEditor;
using UnityEngine;

namespace Arch.Compilation.Editor
{
	[PostBuildProcessor]
	public class PostBuildExporter : IUnitPostBuildProcessor, IPostBuildProcessorGUI
	{
		public string Name => "DLL 导出器";
		public string Description => "将编译生成的DLL复制到导出目录并重命名。";

		public void Process(ArchBuildConfig cfg, string builtDllPath)
		{
			if (string.IsNullOrEmpty(cfg.compilePipeLineSetting.postExportDir)) return;
			if (!File.Exists(builtDllPath)) return;

			string exportRoot = Path.GetFullPath(cfg.compilePipeLineSetting.postExportDir);
			Directory.CreateDirectory(exportRoot);

			string asmName = Path.GetFileNameWithoutExtension(builtDllPath);
			string suffix = string.IsNullOrEmpty(cfg.compilePipeLineSetting.postExportSuffix) ? "dll" : cfg.compilePipeLineSetting.postExportSuffix;
			string newName = $"{asmName}_{suffix}.dll";

			File.Copy(builtDllPath, Path.Combine(exportRoot, newName), true);
			Debug.Log($"[PostBuild] 导出 DLL: {newName}");
		}

		// 🔹 GUI 实现
		public void OnGUI(SerializedObject so)
		{
			var cfg = so.targetObject as ArchBuildConfig;
			if (cfg == null) return;

			EditorGUILayout.LabelField("DLL 导出设置", EditorStyles.boldLabel);

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PropertyField(so.FindProperty("compilePipeLineSetting.postExportDir"), new GUIContent("导出目录"));
			if (GUILayout.Button("选路径", GUILayout.Width(70)))
			{
				string init = string.IsNullOrEmpty(cfg.compilePipeLineSetting.postExportDir) ? Application.dataPath : cfg.compilePipeLineSetting.postExportDir;
				string selected = EditorUtility.OpenFolderPanel("选择导出路径", init, "");
				if (!string.IsNullOrEmpty(selected))
				{
					if (selected.StartsWith(Application.dataPath))
						selected = "Assets" + selected.Substring(Application.dataPath.Length);
					so.FindProperty("compilePipeLineSetting.postExportDir").stringValue = selected;
				}
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.PropertyField(so.FindProperty("compilePipeLineSetting.postExportSuffix"), new GUIContent("导出文件后缀"));
		}
	}
}

#endif