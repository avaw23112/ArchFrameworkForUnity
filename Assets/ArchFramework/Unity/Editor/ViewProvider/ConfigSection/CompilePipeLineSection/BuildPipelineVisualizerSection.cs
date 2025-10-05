#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Arch.Compilation.Editor
{
	/// <summary>
	/// 横向全宽“编译管线可视化”
	/// 三大区块等宽：前处理器 → 编译后处理器 → 总后处理器
	/// </summary>
	public class BuildPipelineVisualizerSection : IConfigSection
	{
		public string SectionName => "编译管线可视化";

		private ArchBuildConfig _cfg;
		private SerializedObject _so;

		// 删除槽状态
		private Rect _deleteZone;

		private bool _isHoveringDeleteZone = false;

		// 拖拽状态
		private bool _isDragging = false;

		private int _dragIndex = -1;
		private int _dragStage = -1;
		private Vector2 _dragOffset;
		private float _dragStartX;
		private float _dragCurX;
		private int _hoverTargetIndex = -1; // 当前拖动时悬停的可替换目标

		// 紧凑布局参数
		private const float kStageHeight = 80f;

		private const float kBlockGap = 4f;
		private const float kBlockHeight = 50f;
		private const float kTitleHeight = 18f;
		private const float kStageGap = 18f;
		private const float kStagePadding = 8f;

		public void OnGUI(SerializedObject so)
		{
			_so = so;
			_so.Update(); // 先刷新
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

			DrawStage("编译前处理器", pipe.preBuildProcessors, yBase, 0 * (stageWidth + kStageGap), stageWidth, new Color(0.36f, 0.75f, 0.9f), 0);
			DrawStage("编译中后处理器", pipe.postProcessors, yBase, 1 * (stageWidth + kStageGap), stageWidth, new Color(0.45f, 0.45f, 0.9f), 1);
			DrawStage("总后处理器", pipe.globalPostProcessors, yBase, 2 * (stageWidth + kStageGap), stageWidth, new Color(0.68f, 0.45f, 0.85f), 2);

			// 绘制删除槽区域
			totalWidth = EditorGUILayout.GetControlRect(false, 0).width;
			float deleteZoneHeight = 40f;
			_deleteZone = GUILayoutUtility.GetRect(totalWidth, deleteZoneHeight, GUILayout.ExpandWidth(true));

			Color deleteColor = _isHoveringDeleteZone
				? new Color(0.8f, 0.1f, 0.1f, 0.9f)  // 拖入时更深红
				: new Color(0.5f, 0.05f, 0.05f, 0.6f);

			EditorGUI.DrawRect(_deleteZone, deleteColor);
			GUIStyle delStyle = new GUIStyle(EditorStyles.boldLabel)
			{
				alignment = TextAnchor.MiddleCenter,
				normal = { textColor = Color.white }
			};
			GUI.Label(_deleteZone, _isHoveringDeleteZone ? "释放以删除此处理器" : "拖拽到此处删除", delStyle);

			HandleMouseEvents();
		}

		private void DrawStage(string title, List<string> processors, float yBase, float offsetX, float stageWidth, Color color, int stageId)
		{
			Rect stageRect = new Rect(offsetX, yBase + 10f, stageWidth, kStageHeight);
			EditorGUI.DrawRect(stageRect, new Color(color.r, color.g, color.b, 0.12f));

			if (processors == null || processors.Count == 0)
			{
				GUI.Label(stageRect, "（空）", new GUIStyle(EditorStyles.centeredGreyMiniLabel) { alignment = TextAnchor.MiddleCenter });
				return;
			}

			float x = offsetX + kStagePadding;
			float y = yBase + kTitleHeight + 8f;
			float segmentWidth = (stageWidth - (processors.Count + 1) * kBlockGap - kStagePadding * 2) / processors.Count;

			for (int i = 0; i < processors.Count; i++)
			{
				string name = processors[i];
				Rect rect = new Rect(x, y, segmentWidth, kBlockHeight);
				bool isThisDragged = _isDragging && _dragStage == stageId && _dragIndex == i;
				bool isTarget = _isDragging && _dragStage == stageId && _hoverTargetIndex == i && !_isDragging.Equals(isThisDragged);

				// 背景：轻渐变 + 投影
				if (!isThisDragged)
				{
					EditorGUI.DrawRect(rect, new Color(color.r, color.g, color.b, 0.45f));
					Handles.BeginGUI();
					Handles.color = new Color(0, 0, 0, 0.25f);
					Handles.DrawAAPolyLine(
						0.4f,
						new Vector3(rect.x, rect.yMax, 0),
						new Vector3(rect.xMax, rect.yMax, 0)
					);
					Handles.EndGUI();
				}

				if (isTarget)
				{
					// 高亮目标（可替换项）
					Handles.color = new Color(1f, 0.84f, 0f, 0.9f); // 金色边框
					Handles.BeginGUI();
					Handles.color = new Color(1f, 0.84f, 0f, 0.9f); // 金色
					Handles.DrawAAPolyLine(
						4f,
						new Vector3(rect.x, rect.y, 0),
						new Vector3(rect.xMax, rect.y, 0),
						new Vector3(rect.xMax, rect.yMax, 0),
						new Vector3(rect.x, rect.yMax, 0),
						new Vector3(rect.x, rect.y, 0)
					);
					Handles.EndGUI();
				}

				// 拖拽中的块
				if (isThisDragged)
				{
					rect.x = _dragCurX - _dragOffset.x;
					rect.y -= 3;
					EditorGUI.DrawRect(rect, new Color(1f, 1f, 1f, 0.2f));
					Handles.color = Color.white;
					Handles.BeginGUI();
					Handles.color = Color.white;
					Handles.DrawAAPolyLine(
						3f,
						new Vector3(rect.x, rect.y, 0),
						new Vector3(rect.xMax, rect.y, 0)
					);
					Handles.EndGUI();
				}

				// 标签
				GUIStyle labelStyle = new GUIStyle(EditorStyles.whiteLabel)
				{
					alignment = TextAnchor.MiddleCenter,
					fontStyle = FontStyle.Bold,
					wordWrap = true
				};
				GUI.Label(rect, $"{i + 1}\n{name}", labelStyle);

				// 鼠标悬停提示
				if (rect.Contains(Event.current.mousePosition))
				{
					string tip = GetDescription(title, name);
					Vector2 size = GUI.skin.box.CalcSize(new GUIContent(tip));
					Rect tipRect = new Rect(Event.current.mousePosition.x + 14, Event.current.mousePosition.y + 14,
						Mathf.Min(size.x + 24, 320), Mathf.Min(size.y + 16, 200));
					EditorGUI.DrawRect(tipRect, new Color(0, 0, 0, 0.85f));
					GUI.Label(tipRect, tip, new GUIStyle(EditorStyles.whiteMiniLabel)
					{
						wordWrap = true,
						alignment = TextAnchor.UpperLeft,
						padding = new RectOffset(6, 6, 4, 4)
					});
					if (EditorWindow.focusedWindow != null)
						EditorWindow.focusedWindow.Repaint();
				}

				// 鼠标点击检测
				if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
				{
					_isDragging = true;
					_dragStage = stageId;
					_dragIndex = i;
					_dragStartX = Event.current.mousePosition.x;
					_dragCurX = _dragStartX;
					_dragOffset.x = Event.current.mousePosition.x - rect.x;
					_hoverTargetIndex = -1;
					GUI.FocusControl(null);
					Event.current.Use();
				}

				// 拖动时检查是否悬停目标
				if (_isDragging && _dragStage == stageId && !_dragIndex.Equals(i))
				{
					Rect hoverRect = rect;
					if (hoverRect.Contains(Event.current.mousePosition))
					{
						_hoverTargetIndex = i;
						if (EditorWindow.focusedWindow != null)
							EditorWindow.focusedWindow.Repaint();
					}
				}

				x += segmentWidth + kBlockGap;
			}
		}

		private void HandleMouseEvents()
		{
			if (!_isDragging) return;

			Event e = Event.current;
			if (e.type == EventType.MouseDrag)
			{
				_dragCurX = e.mousePosition.x;

				// ✅ 检测是否拖入删除区
				_isHoveringDeleteZone = _deleteZone.Contains(e.mousePosition);

				e.Use();
				if (EditorWindow.focusedWindow != null)
					EditorWindow.focusedWindow.Repaint();
			}

			if (e.type == EventType.MouseUp)
			{
				if (_isHoveringDeleteZone)
				{
					DeleteDraggedProcessor();  // ✅ 拖拽释放时删除
				}
				else
				{
					ReorderProcessors();       // 否则执行重排
				}

				_isDragging = false;
				_dragIndex = -1;
				_dragStage = -1;
				_hoverTargetIndex = -1;
				_isHoveringDeleteZone = false;
				e.Use();
			}
		}

		private void DeleteDraggedProcessor()
		{
			if (_cfg == null || _so == null) return;

			var prop = GetStageListProp(_dragStage);
			if (prop == null) return;
			if (_dragIndex < 0 || _dragIndex >= prop.arraySize) return;

			// 记录撤销
			Undo.RecordObject(_so.targetObject, "Delete Processor");

			// 删元素（Unity 序列化 API）
			prop.DeleteArrayElementAtIndex(_dragIndex);

			_so.ApplyModifiedProperties();     // ✅ 立刻写回
			EditorUtility.SetDirty(_cfg);
			AssetDatabase.SaveAssets();

			// 防止同帧其它 Section 用旧快照覆盖
			GUIUtility.ExitGUI();
		}

		private void ReorderProcessors()
		{
			if (_cfg == null || _so == null) return;

			var prop = GetStageListProp(_dragStage);
			if (prop == null) return;
			if (_dragIndex < 0 || _dragIndex >= prop.arraySize) return;

			float offset = _dragCurX - _dragStartX;
			int move = Mathf.RoundToInt(offset / 120f);
			if (move == 0) return;

			int newIndex = Mathf.Clamp(_dragIndex + move, 0, prop.arraySize - 1);
			if (newIndex == _dragIndex) return;

			Undo.RecordObject(_so.targetObject, "Reorder Processor");
			prop.MoveArrayElement(_dragIndex, newIndex);   // ✅ 官方 API 移动

			_so.ApplyModifiedProperties();                 // ✅ 立刻写回
			EditorUtility.SetDirty(_cfg);
			AssetDatabase.SaveAssets();
			GUIUtility.ExitGUI();
		}

		private SerializedProperty GetStageListProp(int stage)
		{
			if (_so == null) return null;
			switch (stage)
			{
				case 0: return _so.FindProperty("compilePipeLineSetting.preBuildProcessors");
				case 1: return _so.FindProperty("compilePipeLineSetting.postProcessors");
				case 2: return _so.FindProperty("compilePipeLineSetting.globalPostProcessors");
				default: return null;
			}
		}

		private string GetDescription(string sectionTitle, string processorName)
		{
			switch (sectionTitle)
			{
				case "编译前处理器":
					if (PreBuildProcessorRegistry.TryGet(processorName, out var pre))
						return $"{pre.Name}\n{pre.Description}";
					break;

				case "编译中后处理器":
					if (UnitPostBuildProcessorRegistry.TryGet(processorName, out var post))
						return $"{post.Name}\n{post.Description}";
					break;

				case "总后处理器":
					if (GlobalPostBuildProcessorRegistry.TryGet(processorName, out var global))
						return $"{global.Name}\n{global.Description}";
					break;
			}
			return $"{processorName}\n（无描述）";
		}
	}
}

#endif