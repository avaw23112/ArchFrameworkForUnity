using Arch.Tools;
using HybridCLR.Editor;
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Arch.Compilation.Editor
{
	[GlobalPostBuildProcessor]
	internal class AutoCopyMetaDllProcessor : IGlobalPostProcessor, IPostBuildProcessorGUI
	{
		public string Name => "补充元数据dll导出器";
		public string Description => "用于自动拷贝补充元数据";

		public void OnGUI(SerializedObject config)
		{
		}

		public void Process(ArchBuildConfig cfg)
		{
			string metaDir = cfg.buildSetting.MetaDllPath;

			string basePath = Path.Combine(Application.dataPath, "..",
				SettingsUtil.AssembliesPostIl2CppStripDir,
				EditorUserBuildSettings.activeBuildTarget.ToString());

			foreach (string szMetaDllName in SettingsUtil.AOTAssemblyNames)
			{
				string path = Path.Combine(basePath, $"{szMetaDllName}.dll");
				if (File.Exists(path))
					Builder.CopyCompiledDll(path, metaDir, ".bytes");
			}
			AssetDatabase.Refresh();
		}
	}
}