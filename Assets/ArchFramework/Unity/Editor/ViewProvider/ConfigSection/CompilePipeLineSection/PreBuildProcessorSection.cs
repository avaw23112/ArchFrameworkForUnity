#if UNITY_EDITOR

using System.Linq;
using UnityEditor;

namespace Arch.Compilation.Editor
{
	public class PreBuildProcessorSection : ProcessorSection
	{
		public override string SectionName => "编译前处理流程 (Pre-Build Pipeline)";

		public override void OnGUI(SerializedObject so)
		{
			var cfg = so.targetObject as ArchBuildConfig;
			if (cfg == null) return;
			if (selectedProcessorIndex >= cfg.compilePipeLineSetting.preBuildProcessors.Count)
				selectedProcessorIndex = -1;
			if (reorderableList == null)
				InitList(cfg, "编译前处理流程", cfg.compilePipeLineSetting.preBuildProcessors);

			EditorGUILayout.LabelField("可用编译前处理器", EditorStyles.boldLabel);
			DrawAddProcessorPopup(cfg);

			DrawSelectedProcessorGUI(cfg, so);
			DrawList("编译前处理列表(可拖拽)");
		}

		private void DrawAddProcessorPopup(ArchBuildConfig cfg)
		{
			var all = PreBuildProcessorRegistry.All.ToList();
			var allNames = all.Select(p => p.Name).ToArray();
			selectedProcessorIndex = EditorGUILayout.Popup("添加处理器", selectedProcessorIndex, allNames);
			if (selectedProcessorIndex >= 0)
			{
				var name = allNames[selectedProcessorIndex];
				if (!cfg.compilePipeLineSetting.preBuildProcessors.Contains(name))
				{
					Undo.RecordObject(cfg, "Add PreBuild Processor");
					cfg.compilePipeLineSetting.preBuildProcessors.Add(name);
					EditorUtility.SetDirty(cfg);
				}
			}
		}

		private void DrawSelectedProcessorGUI(ArchBuildConfig cfg, SerializedObject so)
		{
			if (selectedProcessorIndex < 0 || selectedProcessorIndex >= cfg.compilePipeLineSetting.preBuildProcessors.Count)
				return;

			string selectedName = cfg.compilePipeLineSetting.preBuildProcessors[selectedProcessorIndex];
			if (!PreBuildProcessorRegistry.TryGet(selectedName, out var processor))
				return;

			ProcessorOnGUI(so, processor);
		}
	}
}

#endif