#if UNITY_EDITOR

using System;
using System.Linq;
using UnityEditor;

namespace Arch.Compilation.Editor
{
	public class PostBuildProcessorSection : ProcessorSection
	{
		public override string SectionName => "编译后处理流程 (Post Build Pipeline)";
		private int selectedIndex = -1;

		public override void OnGUI(SerializedObject so)
		{
			var cfg = so.targetObject as ArchBuildConfig;
			if (cfg == null) return;

			if (selectedProcessorIndex >= cfg.compilePipeLineSetting.postProcessors.Count)
				selectedProcessorIndex = -1;

			if (reorderableList == null)
				InitList(cfg, "单编译后处理流程", cfg.compilePipeLineSetting.postProcessors);
			EditorGUILayout.LabelField("可用单位后处理器", EditorStyles.boldLabel);

			DrawAddProcessorPopup(cfg);
			DrawSelectedProcessorGUI<UnitPostBuildProcessorRegistry, IUnitPostBuildProcessor>(cfg, so);
			DrawList("单位编译后处理列表");
		}

		private void DrawAddProcessorPopup(ArchBuildConfig cfg)
		{
			var all = AttributeTargetRegistry.All<UnitPostBuildProcessorRegistry, IUnitPostBuildProcessor>();
			var allNames = all.Select(p => p.Name).ToArray();

			selectedIndex = EditorGUILayout.Popup("添加处理器", selectedIndex, allNames);
			if (selectedIndex >= 0)
			{
				var name = allNames[selectedIndex];
				if (!cfg.compilePipeLineSetting.postProcessors.Contains(name))
				{
					Undo.RecordObject(cfg, "Add UnitPostBuild Processor");
					cfg.compilePipeLineSetting.postProcessors.Add(name);
					EditorUtility.SetDirty(cfg);
				}
				else
				{
					selectedProcessorIndex = IndexOfReorederableList(name);
				}
			}
		}
	}
}

#endif