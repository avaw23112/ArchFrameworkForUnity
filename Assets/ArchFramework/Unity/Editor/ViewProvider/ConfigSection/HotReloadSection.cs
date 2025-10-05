#if UNITY_EDITOR

using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Arch.Compilation.Editor
{
	public class HotReloadSection : IConfigSection
	{
		private int selectedIndex = 0;
		private string[] isolatedNames;
		private ReorderableList reorderableList;
		private bool reorderInit = false;

		public string SectionName => "热重载设置 (Hot Reload)";

		public void OnGUI(SerializedObject so)
		{
			var cfg = so.targetObject as ArchBuildConfig;
			if (cfg == null) return;

			// 🔁 同步清理已删除的 Isolated 条目
			SyncRemovedIsolatedEntries(cfg);

			// ✅ 初始化可拖拽列表
			if (!reorderInit)
			{
				InitReorderableList(cfg, so);
				reorderInit = true;
			}

			EditorGUILayout.Space(5);
			EditorGUILayout.LabelField("从独立程序集 (IsolatedSection) 选择热重载目标", EditorStyles.boldLabel);

			if (cfg.buildSetting.isolated == null || cfg.buildSetting.isolated.Count == 0)
			{
				EditorGUILayout.HelpBox("未配置任何独立程序集，请先在 IsolatedSection 中添加。", MessageType.Info);
				return;
			}

			isolatedNames = cfg.buildSetting.isolated.Select(i => i.assemblyName).ToArray();
			if (selectedIndex >= isolatedNames.Length) selectedIndex = 0;

			selectedIndex = EditorGUILayout.Popup("选择程序集", selectedIndex, isolatedNames);

			// ✅ 添加与删除按钮
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("添加到热重载列表", GUILayout.Height(22)))
			{
				string selected = isolatedNames[selectedIndex];
				if (!cfg.buildSetting.hotReloadAssemblies.Contains(selected))
				{
					Undo.RecordObject(cfg, "Add HotReload Assembly");
					cfg.buildSetting.hotReloadAssemblies.Add(selected);
					EditorUtility.SetDirty(cfg);
					so.Update();
				}
			}

			if (GUILayout.Button("删除选中项", GUILayout.Height(22)))
			{
				DeleteSelectedItem(cfg, so);
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space(10);
			EditorGUILayout.LabelField("热重载程序集列表 (可拖拽排序)", EditorStyles.boldLabel);

			reorderableList.DoLayoutList();
		}

		private void InitReorderableList(ArchBuildConfig cfg, SerializedObject so)
		{
			reorderableList = new ReorderableList(so, so.FindProperty("buildSetting.hotReloadAssemblies"), true, true, false, false);

			reorderableList.drawHeaderCallback = rect =>
			{
				EditorGUI.LabelField(rect, "热重载程序集顺序");
			};

			reorderableList.drawElementCallback = (rect, index, active, focused) =>
			{
				var element = reorderableList.serializedProperty.GetArrayElementAtIndex(index);
				rect.y += 2;
				EditorGUI.LabelField(rect, $"{index + 1}. {element.stringValue}");
			};

			reorderableList.onReorderCallback = list =>
			{
				so.ApplyModifiedProperties();
				EditorUtility.SetDirty(cfg);
			};

			reorderableList.onSelectCallback = list =>
			{
				// 在选中项时刷新 Inspector
				so.ApplyModifiedProperties();
			};
		}

		/// <summary>
		/// 删除 ReorderableList 中选中的项
		/// </summary>
		private void DeleteSelectedItem(ArchBuildConfig cfg, SerializedObject so)
		{
			if (reorderableList.index < 0 || reorderableList.index >= cfg.buildSetting.hotReloadAssemblies.Count)
			{
				EditorUtility.DisplayDialog("删除失败", "请先在列表中选中要删除的项。", "OK");
				return;
			}

			string removed = cfg.buildSetting.hotReloadAssemblies[reorderableList.index];
			Undo.RecordObject(cfg, "Remove HotReload Assembly");
			cfg.buildSetting.hotReloadAssemblies.RemoveAt(reorderableList.index);
			EditorUtility.SetDirty(cfg);

			// 立即刷新面板状态
			reorderableList.index = -1;
			so.Update();

			Debug.Log($"[HotReloadSection] 已删除热重载项: {removed}");
		}

		/// <summary>
		/// 检测并同步清除不存在的 Isolated 条目
		/// </summary>
		private void SyncRemovedIsolatedEntries(ArchBuildConfig cfg)
		{
			if (cfg == null) return;

			// 当前有效的独立程序集名称
			var validNames = cfg.buildSetting.isolated?
				.Select(i => i.assemblyName)
				.Where(s => !string.IsNullOrEmpty(s))
				.ToList() ?? new();

			if (cfg.buildSetting.hotReloadAssemblies == null) return;

			int before = cfg.buildSetting.hotReloadAssemblies.Count;
			cfg.buildSetting.hotReloadAssemblies.RemoveAll(name => !validNames.Contains(name));
			if (cfg.buildSetting.hotReloadAssemblies.Count != before)
			{
				EditorUtility.SetDirty(cfg);
				if (reorderableList != null)
					reorderableList.serializedProperty.serializedObject.Update();
			}
		}
	}
}

#endif