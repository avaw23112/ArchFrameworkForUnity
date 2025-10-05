#if UNITY_EDITOR

using System.Linq;
using UnityEditor;

namespace Arch.Compilation.Editor
{
	public class GlobalPostProcessorSection : ProcessorSection
	{
		public override string SectionName => "总后处理流程 (Global Post Processors)";
		public string lastName;

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
			DrawSelectedProcessorGUI(cfg, so);
			DrawList("编译后处理列表");
		}

		private void DrawAddProcessorPopup(ArchBuildConfig cfg)
		{
			var all = GlobalPostBuildProcessorRegistry.All.ToList();
			var allNames = all.Select(p => p.Name).ToArray();
			selectedProcessorIndex = EditorGUILayout.Popup("添加处理器", selectedProcessorIndex, allNames);
			if (selectedProcessorIndex >= 0)
			{
				var name = allNames[selectedProcessorIndex];
				if (string.IsNullOrEmpty(lastName))
				{
					lastName = name;
				}
				else if (name == lastName)
				{
					return;
				}
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