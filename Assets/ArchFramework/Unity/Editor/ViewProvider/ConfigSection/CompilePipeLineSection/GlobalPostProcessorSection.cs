#if UNITY_EDITOR

using System;
using System.Linq;
using UnityEditor;

namespace Arch.Compilation.Editor
{
	public class GlobalPostProcessorSection : ProcessorSection
	{
		public override string SectionName => "总后处理流程 (Global Post Processors)";
		private int selectedIndex = -1;

		public override void OnGUI(SerializedObject so)
		{
			var cfg = so.targetObject as ArchBuildConfig;
			if (cfg == null) return;

			if (selectedProcessorIndex >= cfg.compilePipeLineSetting.globalPostProcessors.Count)
				selectedProcessorIndex = -1;

			if (reorderableList == null)
				InitList(cfg, "总后处理流程", cfg.compilePipeLineSetting.globalPostProcessors);
			EditorGUILayout.LabelField("可用全局后处理器", EditorStyles.boldLabel);

			DrawAddProcessorPopup(cfg);
			DrawSelectedProcessorGUI<GlobalPostBuildProcessorRegistry, IGlobalPostProcessor>(cfg, so);
			DrawList("编译后处理列表");
		}

		private void DrawAddProcessorPopup(ArchBuildConfig cfg)
		{
			var all = AttributeTargetRegistry.All<GlobalPostBuildProcessorRegistry, IGlobalPostProcessor>();
			var allNames = all.Select(p => p.Name).ToArray();

			selectedIndex = EditorGUILayout.Popup("添加处理器", selectedIndex, allNames);
			if (selectedIndex >= 0)
			{
				var name = allNames[selectedIndex];
				if (!cfg.compilePipeLineSetting.globalPostProcessors.Contains(name))
				{
					Undo.RecordObject(cfg, "Add GlobalPostProcessor");
					cfg.compilePipeLineSetting.globalPostProcessors.Add(name);
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