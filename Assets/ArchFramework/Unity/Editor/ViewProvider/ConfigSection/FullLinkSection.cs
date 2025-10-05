#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace Arch.Compilation.Editor
{
	public class FullLinkSection : IConfigSection
	{
		public string SectionName => "全联编 (FullLink Assembly)";

		public void OnGUI(SerializedObject so)
		{
			var fullLink = so.FindProperty("buildSetting.fullLink");
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.PropertyField(fullLink.FindPropertyRelative("assemblyName"), new GUIContent("程序集名"));
			EditorGUILayout.PropertyField(fullLink.FindPropertyRelative("outputDir"), new GUIContent("输出目录"));
			EditorGUILayout.PropertyField(fullLink.FindPropertyRelative("useEngineModules"), new GUIContent("使用引擎模块"));
			EditorGUILayout.PropertyField(fullLink.FindPropertyRelative("editorAssembly"), new GUIContent("Editor Assembly"));

			DrawList(fullLink.FindPropertyRelative("sourceDirs"), "源码目录");
			DrawList(fullLink.FindPropertyRelative("additionalDefines"), "宏定义");
			DrawList(fullLink.FindPropertyRelative("additionalReferences"), "额外引用 DLL");
			EditorGUILayout.EndVertical();
		}

		private void DrawList(SerializedProperty list, string label)
		{
			EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
			EditorGUI.indentLevel++;
			for (int i = 0; i < list.arraySize; i++)
			{
				EditorGUILayout.BeginHorizontal();
				list.GetArrayElementAtIndex(i).stringValue =
					EditorGUILayout.TextField(list.GetArrayElementAtIndex(i).stringValue);

				if (GUILayout.Button("选路径", GUILayout.Width(60)))
				{
					var path = EditorUtility.OpenFolderPanel(label, "Assets", "");
					if (!string.IsNullOrEmpty(path))
						list.GetArrayElementAtIndex(i).stringValue = path;
				}

				if (GUILayout.Button("-", GUILayout.Width(25)))
				{
					list.DeleteArrayElementAtIndex(i);
					break;
				}
				EditorGUILayout.EndHorizontal();
			}

			if (GUILayout.Button("+ 添加", GUILayout.Width(100)))
				list.InsertArrayElementAtIndex(list.arraySize);
			EditorGUI.indentLevel--;
		}
	}
}

#endif