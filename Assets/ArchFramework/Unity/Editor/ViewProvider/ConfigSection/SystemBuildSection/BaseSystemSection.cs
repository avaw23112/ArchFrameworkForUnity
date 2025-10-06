#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Arch.Compilation.Editor
{
	/// <summary>
	/// 仅允许排序的 System Section。
	/// 自动从注册器系统列表初始化，不可添加或删除。
	/// </summary>
	public abstract class BaseSystemSection : IConfigSection
	{
		protected ReorderableList reorderableList;
		protected string propertyPath;
		protected string sectionName;
		protected List<string> systemNames;
		protected IEnumerable<System.Type> systems;

		public string SectionName => sectionName;

		protected BaseSystemSection(string sectionName, string propertyPath, IEnumerable<System.Type> systems)
		{
			this.sectionName = sectionName;
			this.propertyPath = propertyPath;
			this.systems = systems;
			this.systemNames = systems.Select(t => t.FullName).ToList();
		}

		public void OnGUI(SerializedObject so)
		{
			var prop = so.FindProperty(propertyPath);
			if (prop == null)
			{
				EditorGUILayout.HelpBox($"未找到字段: {propertyPath}", MessageType.Error);
				return;
			}

			// 确保配置中的系统列表与注册器保持一致
			SyncConfigWithSystems(prop, so);

			// 创建仅允许排序的 ReorderableList
			reorderableList ??= new ReorderableList(so, prop, true, true, false, false);

			reorderableList.drawHeaderCallback = rect =>
				EditorGUI.LabelField(rect, $"{sectionName} 顺序");

			reorderableList.drawElementCallback = (rect, index, active, focused) =>
			{
				if (index < prop.arraySize)
				{
					var element = prop.GetArrayElementAtIndex(index);
					EditorGUI.LabelField(rect, $"{index + 1}. {element.stringValue}");
				}
			};

			reorderableList.onReorderCallback = list =>
			{
				so.ApplyModifiedProperties();
				EditorUtility.SetDirty(so.targetObject);
			};

			reorderableList.DoLayoutList();
		}

		/// <summary>
		/// 确保配置中的字符串列表与系统注册表对齐。
		/// 若有新系统则补充，若配置中有不存在的系统则移除。
		/// </summary>
		private void SyncConfigWithSystems(SerializedProperty prop, SerializedObject so)
		{
			var cfgNames = new HashSet<string>(
				Enumerable.Range(0, prop.arraySize).Select(i => prop.GetArrayElementAtIndex(i).stringValue)
			);

			bool changed = false;

			// 1️⃣ 移除配置中不存在的系统
			for (int i = prop.arraySize - 1; i >= 0; i--)
			{
				var element = prop.GetArrayElementAtIndex(i);
				if (!systemNames.Contains(element.stringValue))
				{
					prop.DeleteArrayElementAtIndex(i);
					changed = true;
				}
			}

			// 2️⃣ 添加缺失的新系统
			foreach (var name in systemNames)
			{
				if (!cfgNames.Contains(name))
				{
					prop.InsertArrayElementAtIndex(prop.arraySize);
					prop.GetArrayElementAtIndex(prop.arraySize - 1).stringValue = name;
					changed = true;
				}
			}

			if (changed)
			{
				so.ApplyModifiedProperties();
				EditorUtility.SetDirty(so.targetObject); // ✅ 初始化时的同步也要标记
			}
		}
	}
}

#endif