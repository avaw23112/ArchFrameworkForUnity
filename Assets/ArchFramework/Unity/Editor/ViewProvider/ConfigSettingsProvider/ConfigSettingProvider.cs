#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Arch.Compilation.Editor
{
	public class ConfigSettingsProvider : SettingsProvider
	{
		private SerializedObject _serializedConfig;
		private ArchBuildConfig _config;
		private Vector2 _scroll;

		// ✅ Page 容器支持
		private static readonly List<IConfigPage> _pages = new();

		private static readonly List<IConfigSection> _legacySections = new();
		private int _selectedPageIndex = 0;

		public ConfigSettingsProvider(string path, SettingsScope scope)
			: base(path, scope)
		{
			label = "Arch Build Settings";
		}

		[SettingsProvider]
		public static SettingsProvider CreateProvider()
		{
			var provider = new ConfigSettingsProvider("Project/Arch Build Settings", SettingsScope.Project);
			provider.LoadConfig();
			return provider;
		}

		#region 注册接口

		public static void RegisterPage(IConfigPage page)
		{
			if (page == null || _pages.Contains(page)) return;
			_pages.Add(page);
		}

		/// <summary>
		/// 向后兼容旧的直接 Section 注册
		/// </summary>
		public static void RegisterSection(IConfigSection section)
		{
			if (section == null) return;

			// 若没有 Page，则创建一个默认页
			if (_pages.Count == 0)
			{
				var defaultPage = new ConfigPage("General");
				_pages.Add(defaultPage);
			}

			_pages[0].RegisterSection(section);
			if (!_legacySections.Contains(section))
				_legacySections.Add(section);
		}

		#endregion 注册接口

		private void LoadConfig()
		{
			_config = ArchBuildConfig.LoadOrCreate();
			if (_config != null)
				_serializedConfig = new SerializedObject(_config);
		}

		public override void OnGUI(string searchContext)
		{
			if (_serializedConfig == null)
			{
				EditorGUILayout.HelpBox("未找到 ArchBuildConfig.asset", MessageType.Warning);
				if (GUILayout.Button("创建配置文件"))
				{
					LoadConfig();
				}
				return;
			}

			if (_pages.Count == 0)
			{
				EditorGUILayout.HelpBox("未注册任何 Page 或 Section。", MessageType.Info);
				return;
			}

			EditorGUILayout.BeginHorizontal();

			// ✅ 左侧 Page 列表
			DrawPageList();

			// ✅ 右侧内容区域
			EditorGUILayout.BeginVertical();
			_serializedConfig.Update();

			if (_selectedPageIndex < _pages.Count)
			{
				_scroll = EditorGUILayout.BeginScrollView(_scroll);
				_pages[_selectedPageIndex].OnGUI(_serializedConfig);
				EditorGUILayout.EndScrollView();
			}

			_serializedConfig.ApplyModifiedProperties();
			EditorGUILayout.EndVertical();

			EditorGUILayout.EndHorizontal();
		}

		private void DrawPageList()
		{
			GUILayout.BeginVertical(GUILayout.Width(180));
			EditorGUILayout.LabelField("配置页", EditorStyles.boldLabel);
			EditorGUILayout.Space(5);

			for (int i = 0; i < _pages.Count; i++)
			{
				var page = _pages[i];
				bool selected = i == _selectedPageIndex;

				GUIStyle style = new(EditorStyles.toolbarButton)
				{
					fontStyle = selected ? FontStyle.Bold : FontStyle.Normal,
					alignment = TextAnchor.MiddleLeft
				};

				Color bg = selected ? new Color(0.2f, 0.4f, 0.8f, 0.2f) : Color.clear;
				Rect rect = GUILayoutUtility.GetRect(160, 24, style);
				EditorGUI.DrawRect(rect, bg);

				if (GUI.Button(rect, "  " + page.PageName, style))
				{
					_selectedPageIndex = i;
					GUI.FocusControl(null);
				}
			}

			GUILayout.FlexibleSpace();
			GUILayout.EndVertical();
		}
	}
}

#endif