#if UNITY_EDITOR

using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Arch.Compilation.Editor
{
	public class PostBuildProcessorSection : ProcessorSection
	{
		public override string SectionName => "编译后处理流程 (Post Build Pipeline)";

		public override void OnGUI(SerializedObject so)
		{
			var cfg = so.targetObject as ArchBuildConfig;
			if (cfg == null) return;
			if (selectedProcessorIndex >= cfg.compilePipeLineSetting.postProcessors.Count)
				selectedProcessorIndex = -1;
			InitList(cfg, "单编译后处理流程", cfg.compilePipeLineSetting.postProcessors);
			EditorGUILayout.Space(10);

			DrawAddProcessorPopup(cfg);
			DrawSelectedProcessorGUI(cfg, so);
			DrawList("单位编译后处理列表");
		}

		private void DrawAddProcessorPopup(ArchBuildConfig cfg)
		{
			var all = UnitPostBuildProcessorRegistry.All.ToList();
			var allNames = all.Select(p => p.Name).ToArray();
			selectedProcessorIndex = EditorGUILayout.Popup("添加处理器", selectedProcessorIndex, allNames);
			if (selectedProcessorIndex >= 0)
			{
				var name = allNames[selectedProcessorIndex];
				if (!cfg.compilePipeLineSetting.postProcessors.Contains(name))
				{
					Undo.RecordObject(cfg, "Add PreBuild Processor");
					cfg.compilePipeLineSetting.postProcessors.Add(name);
					EditorUtility.SetDirty(cfg);
				}
			}
		}

		private void DrawSelectedProcessorGUI(ArchBuildConfig cfg, SerializedObject so)
		{
			if (selectedProcessorIndex < 0 || selectedProcessorIndex >= cfg.compilePipeLineSetting.postProcessors.Count)
				return;

			string selectedName = cfg.compilePipeLineSetting.postProcessors[selectedProcessorIndex];
			if (!UnitPostBuildProcessorRegistry.TryGet(selectedName, out var processor))
				return;

			ProcessorOnGUI(so, processor);
		}
	}
}

#endif