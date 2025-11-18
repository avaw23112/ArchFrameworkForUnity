#if UNITY_EDITOR

using System.Linq;
using UnityEditor;

namespace Arch.Compilation.Editor
{
	public class PreBuildProcessorSection : ProcessorSection
	{
		public override string SectionName => "编译前处理流程 (Pre-Build Pipeline)";
		private int selectedIndex = -1;

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
			DrawSelectedProcessorGUI<PreBuildProcessorRegistry, IPreBuildProcessor>(cfg, so);
			DrawList("编译前处理列表");
		}

		private void DrawAddProcessorPopup(ArchBuildConfig cfg)
		{
			var all = AttributeTargetRegistry.All<PreBuildProcessorRegistry, IPreBuildProcessor>();
			var allNames = all.Select(p => p.Name).ToArray();

			selectedIndex = EditorGUILayout.Popup("添加处理器", selectedIndex, allNames);
			if (selectedIndex >= 0)
			{
				var name = allNames[selectedIndex];
				if (!cfg.compilePipeLineSetting.preBuildProcessors.Contains(name))
				{
					Undo.RecordObject(cfg, "Add PreBuild Processor");
					cfg.compilePipeLineSetting.preBuildProcessors.Add(name);
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