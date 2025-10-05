#if UNITY_EDITOR

using System.Linq;
using UnityEditor;

namespace Arch.Compilation.Editor
{
	public class GlobalPostProcessorSection : ProcessorSection
	{
		public override string SectionName => "总后处理流程 (Global Post Processors)";

		public override void OnGUI(SerializedObject so)
		{
			var cfg = so.targetObject as ArchBuildConfig;
			if (cfg == null) return;

			//if (reorderableList == null)
			//	InitList(cfg, "总后处理流程", cfg.compilePipeLineSetting.globalPostProcessors);

			EditorGUILayout.LabelField("可用全局后处理器", EditorStyles.boldLabel);

			DrawAddProcessorPopup(cfg);
			DrawSelectedProcessorGUI(cfg, so);
			//DrawList("编译后处理列表");
		}

		private void DrawAddProcessorPopup(ArchBuildConfig cfg)
		{
			var all = PreBuildProcessorRegistry.All.ToList();
			var allNames = all.Select(p => p.Name).ToArray();
			int addIdx = EditorGUILayout.Popup("添加处理器", -1, allNames);
			if (addIdx >= 0)
			{
				var name = allNames[addIdx];
				if (!cfg.compilePipeLineSetting.globalPostProcessors.Contains(name))
				{
					Undo.RecordObject(cfg, "Add GlobalPostProcessor");
					cfg.compilePipeLineSetting.globalPostProcessors.Add(name);
					EditorUtility.SetDirty(cfg);
				}
			}
		}

		private void DrawSelectedProcessorGUI(ArchBuildConfig cfg, SerializedObject so)
		{
			if (selectedProcessorIndex < 0 || selectedProcessorIndex >= cfg.compilePipeLineSetting.globalPostProcessors.Count)
				return;

			string selectedName = cfg.compilePipeLineSetting.globalPostProcessors[selectedProcessorIndex];
			if (!GlobalPostBuildProcessorRegistry.TryGet(selectedName, out var processor))
				return;

			ProcessorOnGUI(so, processor);
		}
	}
}

#endif