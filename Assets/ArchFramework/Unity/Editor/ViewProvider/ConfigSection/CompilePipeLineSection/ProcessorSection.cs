using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Arch.Compilation.Editor
{
	public abstract class ProcessorSection : IConfigSection
	{
		public abstract string SectionName { get; }
		protected ReorderableList reorderableList;
		protected int selectedProcessorIndex = 0;
		protected bool isPostProcessorListExpanded = false;

		protected void InitList(ArchBuildConfig cfg, string processorOrder, List<string> postProcessors)
		{
			reorderableList = new ReorderableList(postProcessors, typeof(string), true, true, true, true);
			reorderableList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, processorOrder);
			reorderableList.drawElementCallback = (rect, index, active, focused) =>
			{
				if (index < 0 || index >= postProcessors.Count) return;
				var name = postProcessors[index];
				rect.y += 2;
				EditorGUI.LabelField(rect, $"{index + 1}. {name}");
			};
			reorderableList.onRemoveCallback = list =>
			{
				if (list.index >= 0 && list.index < postProcessors.Count)
				{
					postProcessors.RemoveAt(list.index);
					selectedProcessorIndex = -1; // ✅ 重置选中索引
					EditorUtility.SetDirty(cfg);
				}
			};
			reorderableList.onReorderCallback = list =>
			{
				selectedProcessorIndex = Mathf.Clamp(selectedProcessorIndex, 0, postProcessors.Count - 1);
				EditorUtility.SetDirty(cfg);
			};
			reorderableList.onSelectCallback = list => selectedProcessorIndex = list.index;
		}

		protected void DrawList(string listLabel)
		{
			EditorGUILayout.Space(10);
			isPostProcessorListExpanded = EditorGUILayout.Foldout(isPostProcessorListExpanded, listLabel, true);
			EditorGUILayout.Space(2);
			if (isPostProcessorListExpanded)
			{
				reorderableList.DoLayoutList();
			}
		}

		protected void ProcessorOnGUI(SerializedObject so, IProcessor processor)
		{
			if (processor is IPostBuildProcessorGUI guiProcessor)
			{
				EditorGUILayout.Space(10);
				EditorGUILayout.LabelField($"⚙ {processor.Name} 设置", EditorStyles.boldLabel);
				EditorGUILayout.BeginVertical("box");
				guiProcessor.OnGUI(so);
				EditorGUILayout.EndVertical();
			}
			else
			{
				EditorGUILayout.HelpBox("该处理器没有自定义配置界面。", MessageType.Info);
			}
		}

		public abstract void OnGUI(SerializedObject so);
	}
}