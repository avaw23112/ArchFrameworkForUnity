using Arch.Tools;

using System.IO;
using UnityEditor;
using UnityEngine;

namespace Arch.Compilation.Editor
{
	[PreBuildProcessor]
	public class CleanOldHotfixFiles : IPreBuildProcessor, IPostBuildProcessorGUI
	{
		public string Name => "清理旧热更新DLL";
		public string Description => "清空 Meta 与 HotUpdate 资源输出目录";

		public void OnGUI(SerializedObject config)
		{
			Builder.EditorModifyDllPath(config);
		}

		public void Process(ArchBuildConfig cfg)
		{
			Builder.DeleteAllFilesInDirectory(cfg.buildSetting.MetaDllPath);
			Builder.DeleteAllFilesInDirectory(cfg.buildSetting.HotFixDllPath);
		}
	}
}