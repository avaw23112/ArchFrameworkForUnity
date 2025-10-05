#if UNITY_EDITOR

using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Arch.Compilation.Editor
{
	public class AssemblyModeSection : IConfigSection
	{
		private IsolatedSection isolatedSection = new();
		private FullLinkSection fullLinkSection = new();
		private ReorderableList reorderableList;
		private bool reorderInit = false;
		private BuildSetting.AssemblyBuildMode lastMode;
		private bool initialized = false;

		public string SectionName => "程序集模式选择 (Assembly Build Mode)";

		public void OnGUI(SerializedObject so)
		{
			var cfg = so.targetObject as ArchBuildConfig;
			if (cfg == null) return;

			// 检查删除同步
			SyncRemovedIsolatedEntries(cfg);

			if (!initialized)
			{
				lastMode = cfg.buildSetting.buildMode;
				initialized = true;
			}

			EditorGUILayout.Space(5);
			EditorGUILayout.LabelField("程序集构建模式", EditorStyles.boldLabel);

			var modeProp = so.FindProperty("buildSetting.buildMode");
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(modeProp, new GUIContent("编译模式"));
			if (EditorGUI.EndChangeCheck())
			{
				so.ApplyModifiedProperties();
				EditorUtility.SetDirty(cfg);
				lastMode = cfg.buildSetting.buildMode;
			}

			so.ApplyModifiedProperties();

			EditorGUILayout.Space(10);
			EditorGUILayout.BeginVertical("box");

			if (cfg.buildSetting.buildMode == BuildSetting.AssemblyBuildMode.Isolated)
			{
				DrawIsolatedSection(so, cfg);
			}
			else
			{
				fullLinkSection.OnGUI(so);
			}

			EditorGUILayout.EndVertical();
		}

		private void DrawIsolatedSection(SerializedObject so, ArchBuildConfig cfg)
		{
			EditorGUILayout.LabelField("独立编译配置 (Isolated Build)", EditorStyles.boldLabel);
			isolatedSection.OnGUI(so);

			EditorGUILayout.Space(10);
			EditorGUILayout.LabelField("程序集顺序 (可拖拽调整)", EditorStyles.boldLabel);

			if (!reorderInit)
			{
				InitReorderableList(so, cfg);
				reorderInit = true;
			}
			var isoProp = so.FindProperty("buildSetting.isolated");
			if (reorderableList == null || reorderableList.serializedProperty.arraySize != isoProp.arraySize)
			{
				InitReorderableList(so, cfg);
			}

			reorderableList.DoLayoutList();
		}

		private void InitReorderableList(SerializedObject so, ArchBuildConfig cfg)
		{
			reorderableList = new ReorderableList(so, so.FindProperty("buildSetting.isolated"), true, true, false, false);
			reorderableList.drawHeaderCallback = rect =>
			{
				EditorGUI.LabelField(rect, "Isolated Assemblies 顺序");
			};

			reorderableList.drawElementCallback = (rect, index, active, focused) =>
			{
				var list = so.FindProperty("buildSetting.isolated");
				if (index >= list.arraySize) return;
				var element = list.GetArrayElementAtIndex(index);
				rect.y += 2;
				string asmName = element.FindPropertyRelative("assemblyName").stringValue;
				EditorGUI.LabelField(rect, $"{index + 1}. {asmName}");
			};

			reorderableList.onReorderCallback = list =>
			{
				so.ApplyModifiedProperties();
				EditorUtility.SetDirty(cfg);
			};
		}

		/// <summary>
		/// 检测并同步删除的独立条目。
		/// </summary>
		private void SyncRemovedIsolatedEntries(ArchBuildConfig cfg)
		{
			if (cfg == null) return;

			// 收集当前存在的 isolated 名称
			var validNames = cfg.buildSetting.isolated?.Select(i => i.assemblyName)?.Where(s => !string.IsNullOrEmpty(s)).ToList() ?? new();

			// 清除 hotReloadAssemblies 中已被删掉的条目
			if (cfg.buildSetting.hotReloadAssemblies != null)
			{
				int beforeCount = cfg.buildSetting.hotReloadAssemblies.Count;
				cfg.buildSetting.hotReloadAssemblies.RemoveAll(name => !validNames.Contains(name));
				if (cfg.buildSetting.hotReloadAssemblies.Count != beforeCount)
					EditorUtility.SetDirty(cfg);
			}

			// 检查 reorderableList 中的有效性
			if (reorderableList != null && reorderableList.serializedProperty != null)
			{
				var sp = reorderableList.serializedProperty;
				for (int i = sp.arraySize - 1; i >= 0; i--)
				{
					var element = sp.GetArrayElementAtIndex(i);
					var asmName = element.FindPropertyRelative("assemblyName").stringValue;
					if (!validNames.Contains(asmName))
					{
						sp.DeleteArrayElementAtIndex(i);
						EditorUtility.SetDirty(cfg);
					}
				}
			}
		}
	}
}

#endif