#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Arch.Compilation.Editor
{
	/// <summary>
	/// 现代化紧凑版：编译管线可视化（纯展示，不可拖拽、不可删除、无展开）
	/// </summary>
	public class BuildPipelineVisualizerSection : IConfigSection
	{
		public string SectionName => "编译管线可视化";

		private ArchBuildConfig _cfg;
		private SerializedObject _so;

		// 样式参数
		private const float kStageHeight = 72f;

		private const float kBlockGap = 3f;
		private const float kBlockHeight = 42f;
		private const float kStageGap = 12f;
		private const float kStagePadding = 6f;

		public void OnGUI(SerializedObject so)
		{
			EditorGUILayout.Space(10);
			_so = so;
			_so.Update();
			if (_cfg == null)
				_cfg = so.targetObject as ArchBuildConfig;

			if (_cfg == null || _cfg.compilePipeLineSetting == null)
			{
				EditorGUILayout.HelpBox("未找到 ArchBuildConfig 或 compilePipeLineSetting。", MessageType.Warning);
				return;
			}

			var pipe = _cfg.compilePipeLineSetting;
			float totalWidth = EditorGUILayout.GetControlRect(false, 0).width;
			float stageWidth = (totalWidth - kStageGap * 2) / 3f;
			float yBase = GUILayoutUtility.GetRect(totalWidth, kStageHeight + 24f).y;

			// 整体底层背景
			EditorGUI.DrawRect(new Rect(0, yBase - 8, totalWidth, kStageHeight + 56f), new Color(0.1f, 0.1f, 0.1f, 0.05f));

			DrawStage("编译前处理器", pipe.preBuildProcessors, yBase, 0 * (stageWidth + kStageGap), stageWidth, new Color(0.36f, 0.75f, 0.9f));
			DrawStage("编译中后处理器", pipe.postProcessors, yBase, 1 * (stageWidth + kStageGap), stageWidth, new Color(0.45f, 0.45f, 0.9f));
			DrawStage("总后处理器", pipe.globalPostProcessors, yBase, 2 * (stageWidth + kStageGap), stageWidth, new Color(0.68f, 0.45f, 0.85f));
		}

		private void DrawStage(string title, List<string> processors, float yBase, float offsetX, float stageWidth, Color color)
		{
			Rect stageRect = new Rect(offsetX, yBase, stageWidth, kStageHeight);
			EditorGUI.DrawRect(stageRect, new Color(color.r, color.g, color.b, 0.10f));

			// 阴影底线
			Handles.BeginGUI();
			Handles.color = new Color(0, 0, 0, 0.15f);
			Handles.DrawAAPolyLine(
				2f,
				new Vector3(stageRect.x, stageRect.yMax, 0),
				new Vector3(stageRect.xMax, stageRect.yMax, 0)
			);
			Handles.EndGUI();

			// 标题
			GUI.Label(new Rect(stageRect.x, stageRect.y - 16, stageRect.width, 16),
				title, new GUIStyle(EditorStyles.boldLabel)
				{
					alignment = TextAnchor.MiddleCenter,
					fontSize = 12
				});

			// 内容为空
			if (processors == null || processors.Count == 0)
			{
				GUI.Label(stageRect, "（无处理器）", new GUIStyle(EditorStyles.centeredGreyMiniLabel)
				{
					alignment = TextAnchor.MiddleCenter
				});
				return;
			}

			// 块布局
			float x = offsetX + kStagePadding;
			float y = yBase + 20f;
			float segmentWidth = (stageWidth - (processors.Count + 1) * kBlockGap - kStagePadding * 2) / processors.Count;

			for (int i = 0; i < processors.Count; i++)
			{
				string name = processors[i];
				Rect rect = new Rect(x, y, segmentWidth, kBlockHeight);

				// 颜色渐变背景 + 轻阴影
				Color baseCol = new Color(color.r, color.g, color.b, 0.55f);
				EditorGUI.DrawRect(rect, baseCol);
				DrawShadow(rect, 2f, new Color(0, 0, 0, 0.15f));

				// 悬停高亮
				if (rect.Contains(Event.current.mousePosition))
				{
					EditorGUI.DrawRect(rect, new Color(1f, 1f, 1f, 0.08f));
					if (Event.current.type == EventType.Repaint)
						EditorWindow.focusedWindow?.Repaint();
				}

				// 边框
				Handles.BeginGUI();
				Handles.color = new Color(1f, 1f, 1f, 0.06f);
				Handles.DrawSolidRectangleWithOutline(rect, Color.clear, new Color(1f, 1f, 1f, 0.08f));
				Handles.EndGUI();

				// 标签
				GUIStyle labelStyle = new GUIStyle(EditorStyles.whiteLabel)
				{
					alignment = TextAnchor.MiddleCenter,
					fontStyle = FontStyle.Bold,
					fontSize = 11,
					wordWrap = true
				};
				GUI.Label(rect, $"{i + 1}\n{name}", labelStyle);

				// 悬停提示
				if (rect.Contains(Event.current.mousePosition))
				{
					string tip = GetDescription(title, name);
					Vector2 size = GUI.skin.box.CalcSize(new GUIContent(tip));
					Rect tipRect = new Rect(Event.current.mousePosition.x + 12, Event.current.mousePosition.y + 12,
						Mathf.Min(size.x + 24, 320), Mathf.Min(size.y + 12, 160));
					EditorGUI.DrawRect(tipRect, new Color(0, 0, 0, 0.85f));
					GUI.Label(tipRect, tip, new GUIStyle(EditorStyles.whiteMiniLabel)
					{
						wordWrap = true,
						alignment = TextAnchor.UpperLeft,
						padding = new RectOffset(6, 6, 4, 4)
					});
				}

				x += segmentWidth + kBlockGap;
			}
		}

		private void DrawShadow(Rect rect, float size, Color color)
		{
			for (int i = 1; i <= size; i++)
			{
				EditorGUI.DrawRect(new Rect(rect.x - i, rect.y - i, rect.width + i * 2, rect.height + i * 2),
					new Color(color.r, color.g, color.b, color.a / (i * 2)));
			}
		}

		private string GetDescription(string sectionTitle, string processorName)
		{
			switch (sectionTitle)
			{
				case "编译前处理器":
					if (AttributeTargetRegistry.TryGet<PreBuildProcessorRegistry, IPreBuildProcessor>(processorName, out var pre))
						return $"{pre.Name}\n{pre.Description}";
					break;

				case "编译中后处理器":
					if (AttributeTargetRegistry.TryGet<UnitPostBuildProcessorRegistry, IUnitPostBuildProcessor>(processorName, out var post))
						return $"{post.Name}\n{post.Description}";
					break;

				case "总后处理器":
					if (AttributeTargetRegistry.TryGet<GlobalPostBuildProcessorRegistry, IGlobalPostProcessor>(processorName, out var global))
						return $"{global.Name}\n{global.Description}";
					break;
			}
			return $"{processorName}\n（无描述）";
		}
	}
}

#endif