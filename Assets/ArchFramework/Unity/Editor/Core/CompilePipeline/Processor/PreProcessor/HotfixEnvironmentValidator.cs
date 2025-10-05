using HybridCLR.Editor;
using HybridCLR.Editor.HotUpdate;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Arch.Compilation.Editor
{
	[PreBuildProcessor]
	public class HotfixEnvironmentValidator : IPreBuildProcessor, IPostBuildProcessorGUI
	{
		public string Name => "热更新环境验证器";
		public string Description => "检查热更新打包前的 HybridCLR 环境、配置与元数据状态";

		public void OnGUI(SerializedObject config)
		{
			Builder.EditorModifyDllPath(config);
		}

		public void Process(ArchBuildConfig cfg)
		{
			if (string.IsNullOrEmpty(cfg.buildSetting.MetaDllPath) ||
				string.IsNullOrEmpty(cfg.buildSetting.HotFixDllPath))
				throw new System.Exception("热更新程序集的资源目录为空！");

			if (AssetDatabase.FindAssets("t:Script AOTGenericReferences").Length == 0)
				throw new System.Exception("未执行华佗 Generate/all！");

			HybridCLR.Editor.Commands.LinkGeneratorCommand.GenerateLinkXml();

			if (!Builder.CheckAccessMissingMetadata())
				throw new System.Exception("环境缺失元数据，请执行华佗 Generate/all！");

			if (SettingsUtil.AOTAssemblyNames.Count == 0)
				throw new System.Exception("AOTGenericReferences 配置缺失！");

			if (!Directory.Exists(SettingsUtil.HotUpdateDllsRootOutputDir))
				throw new System.Exception("未配置热更新程序集输出目录！");
		}
	}
}