#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace Arch.Compilation.Editor
{
	public class IsolatedSection : IConfigSection
	{
		public string SectionName => "独立编译 (Isolated Assemblies)";

		public void OnGUI(SerializedObject so)
		{
			var list = so.FindProperty("buildSetting.isolated");
			for (int i = 0; i < list.arraySize; i++)
			{
				var element = list.GetArrayElementAtIndex(i);
				EditorGUILayout.BeginVertical("box");

				// 程序集名
				EditorGUILayout.PropertyField(element.FindPropertyRelative("assemblyName"), new GUIContent("程序集名"));

				// 输出目录 + 选路径按钮
				EditorGUILayout.BeginHorizontal();
				var outputProp = element.FindPropertyRelative("outputDir");
				EditorGUILayout.PropertyField(outputProp, new GUIContent("输出目录"));
				if (GUILayout.Button("选路径", GUILayout.Width(70)))
				{
					string initPath = string.IsNullOrEmpty(outputProp.stringValue)
						? Application.dataPath
						: outputProp.stringValue;

					string path = EditorUtility.OpenFolderPanel("选择输出目录", initPath, "");
					if (!string.IsNullOrEmpty(path))
					{
						// 尝试转换为相对路径（例如 Assets/HotfixOutput）
						if (path.StartsWith(Application.dataPath))
							path = "Assets" + path.Substring(Application.dataPath.Length);
						outputProp.stringValue = path;
						so.ApplyModifiedProperties();
					}
				}
				EditorGUILayout.EndHorizontal();

				EditorGUILayout.PropertyField(element.FindPropertyRelative("useEngineModules"), new GUIContent("使用引擎模块"));
				EditorGUILayout.PropertyField(element.FindPropertyRelative("editorAssembly"), new GUIContent("Editor Assembly"));

				DrawList(element.FindPropertyRelative("sourceDirs"), so, "源码目录");
				DrawList(element.FindPropertyRelative("additionalDefines"), so, "宏定义");
				DrawList(element.FindPropertyRelative("additionalReferences"), so, "额外引用 DLL");

				if (GUILayout.Button("删除该条目", GUILayout.Width(120)))
				{
					list.DeleteArrayElementAtIndex(i);
					so.ApplyModifiedProperties();
					EditorUtility.SetDirty(so.targetObject);
					break;
				}
				EditorGUILayout.EndVertical();
				EditorGUILayout.Space(5);
			}

			if (GUILayout.Button("+ 添加新的 Isolated 项", GUILayout.Width(220)))
			{
				list.InsertArrayElementAtIndex(list.arraySize);
				var newElem = list.GetArrayElementAtIndex(list.arraySize - 1);
				newElem.FindPropertyRelative("assemblyName").stringValue = "NewAssembly";
				newElem.FindPropertyRelative("outputDir").stringValue = "HotfixOutput";
				so.ApplyModifiedProperties();
				EditorUtility.SetDirty(so.targetObject);
			}
		}

		private void DrawList(SerializedProperty list, SerializedObject so, string label)
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
					{
						if (path.StartsWith(Application.dataPath))
							path = "Assets" + path.Substring(Application.dataPath.Length);
						list.GetArrayElementAtIndex(i).stringValue = path;
						so.ApplyModifiedProperties();
						EditorUtility.SetDirty(so.targetObject);
					}
				}

				if (GUILayout.Button("-", GUILayout.Width(25)))
				{
					list.DeleteArrayElementAtIndex(i);
					so.ApplyModifiedProperties();
					EditorUtility.SetDirty(so.targetObject);
					break;
				}
				EditorGUILayout.EndHorizontal();
			}

			if (GUILayout.Button("+ 添加", GUILayout.Width(100)))
			{
				so.ApplyModifiedProperties();
				list.InsertArrayElementAtIndex(list.arraySize);
				EditorUtility.SetDirty(so.targetObject);
			}

			EditorGUI.indentLevel--;
		}
	}
}

#endif