#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Arch.Compilation.Editor
{
	public interface IConfigPage
	{
		string PageName { get; }
		List<IConfigSection> Sections { get; }

		void OnGUI(SerializedObject so);

		void RegisterSection(IConfigSection section);
	}

	public class ConfigPage : IConfigPage
	{
		public string PageName { get; private set; }
		public List<IConfigSection> Sections { get; } = new();

		public ConfigPage(string pageName)
		{
			PageName = pageName;
		}

		public void RegisterSection(IConfigSection section)
		{
			if (section == null || Sections.Contains(section)) return;
			Sections.Add(section);
		}

		public void OnGUI(SerializedObject so)
		{
			foreach (var section in Sections)
			{
				EditorGUILayout.Space(8);
				EditorGUILayout.LabelField(section.SectionName, EditorStyles.boldLabel);
				section.OnGUI(so);
				EditorGUILayout.Space(10);
				EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
			}
		}
	}
}

#endif