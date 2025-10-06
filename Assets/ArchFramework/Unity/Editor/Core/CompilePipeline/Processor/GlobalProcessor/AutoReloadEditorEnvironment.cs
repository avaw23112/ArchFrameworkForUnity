using Arch.Tools;
using System;
using UnityEditor;

namespace Arch.Compilation.Editor
{
	[GlobalPostBuildProcessor]
	internal class AutoReloadEditorEnvironment : IGlobalPostProcessor
	{
		public string Name => "自动重建编辑器环境";

		public string Description => "";
		private static int _reloadCount = 0;
		private const int MaxReloadCountBeforeFullReset = 10;

		public void Process(ArchBuildConfig cfg)
		{
			_reloadCount++;
			if (_reloadCount >= MaxReloadCountBeforeFullReset)
			{
				ArchLog.LogWarning($"[AutoReload] 已进行 {_reloadCount} 次自动重载，触发 Editor Script Reload。");
				_reloadCount = 0;
				EditorUtility.RequestScriptReload();
				return;
			}

			ConfigSettingsProvider.ClearAllPages();
			EditorStart.BuildEditorEnvironment();
			EditorStart.BuildEditorConfigPage();
			SettingsService.RepaintAllSettingsWindow();
		}
	}
}