using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Arch.Compilation.Editor
{
	[PostBuildProcessor]
	public class PostBuildFody : IUnitPostBuildProcessor, IPostBuildProcessorGUI
	{
		public string Name => "代码静态编织处理器";

		public string Description => "Test";

		private ReorderableList _weaverList;

		public void Process(ArchBuildConfig cfg, string builtDllPath)
		{
			var assemblyResolver = FodyProcessorHelper.CreateAssembly();
			FodyProcessorHelper.ProcessAssembly(cfg, builtDllPath, assemblyResolver);
		}

		public void OnGUI(SerializedObject config)
		{
			// 获取对应的列表属性
			SerializedProperty weaverPathsProp = config.FindProperty("compilePipeLineSetting.weaverPaths");
			var cfg = config.targetObject as ArchBuildConfig;

			if (weaverPathsProp == null)
			{
				EditorGUILayout.HelpBox("未找到 weaverPaths 属性", MessageType.Warning);
				return;
			}

			// 初始化可重排列表（确保只初始化一次）
			if (_weaverList == null)
			{
				_weaverList = new ReorderableList(
					elements: cfg.compilePipeLineSetting.weaverPaths,
					elementType: typeof(string),
					draggable: true,
					displayHeader: true,
					displayAddButton: true,
					displayRemoveButton: true
				);

				// 设置列表标题
				_weaverList.headerHeight = 20;
				_weaverList.drawHeaderCallback = (Rect rect) =>
				{
					EditorGUI.LabelField(rect, "Weaver 路径列表");
				};

				// 绘制列表元素
				_weaverList.elementHeight = 24;
				_weaverList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
				{
					rect.yMin += 2;
					rect.yMax -= 2;
					rect.width -= 4; // 留一点边距

					// 获取当前元素的属性
					SerializedProperty elementProp = weaverPathsProp.GetArrayElementAtIndex(index);
					EditorGUI.PropertyField(rect, elementProp, GUIContent.none);
				};

				// 自定义添加按钮逻辑（核心修改：存储完整路径）
				_weaverList.onAddCallback = (list) =>
				{
					string initPath = Application.dataPath; // 初始路径（项目 Assets 目录）
					string selectedPath = EditorUtility.OpenFilePanel(
						"选择Weaver DLL文件",  // 窗口标题
						initPath,              // 初始路径
						"dll"                  // 文件类型过滤（只显示.dll文件）
					);

					if (!string.IsNullOrEmpty(selectedPath))
					{
						// 直接存储完整路径（而非文件夹名）
						weaverPathsProp.arraySize++;
						int newIndex = weaverPathsProp.arraySize - 1;
						SerializedProperty newElement = weaverPathsProp.GetArrayElementAtIndex(newIndex);
						newElement.stringValue = selectedPath; // 存储完整路径

						config.ApplyModifiedProperties();
					}
				};
			}

			// 绘制列表
			_weaverList.DoLayoutList();

			// 应用属性修改（确保手动编辑后的值被保存）
			if (GUI.changed)
			{
				config.ApplyModifiedProperties();
			}
		}
	}
}