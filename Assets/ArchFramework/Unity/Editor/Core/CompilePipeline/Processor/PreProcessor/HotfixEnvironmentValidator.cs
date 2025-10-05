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
		public string Name => "�ȸ��»�����֤��";
		public string Description => "����ȸ��´��ǰ�� HybridCLR ������������Ԫ����״̬";

		public void OnGUI(SerializedObject config)
		{
			Builder.EditorModifyDllPath(config);
		}

		public void Process(ArchBuildConfig cfg)
		{
			if (string.IsNullOrEmpty(cfg.buildSetting.MetaDllPath) ||
				string.IsNullOrEmpty(cfg.buildSetting.HotFixDllPath))
				throw new System.Exception("�ȸ��³��򼯵���ԴĿ¼Ϊ�գ�");

			if (AssetDatabase.FindAssets("t:Script AOTGenericReferences").Length == 0)
				throw new System.Exception("δִ�л�٢ Generate/all��");

			HybridCLR.Editor.Commands.LinkGeneratorCommand.GenerateLinkXml();

			if (!Builder.CheckAccessMissingMetadata())
				throw new System.Exception("����ȱʧԪ���ݣ���ִ�л�٢ Generate/all��");

			if (SettingsUtil.AOTAssemblyNames.Count == 0)
				throw new System.Exception("AOTGenericReferences ����ȱʧ��");

			if (!Directory.Exists(SettingsUtil.HotUpdateDllsRootOutputDir))
				throw new System.Exception("δ�����ȸ��³������Ŀ¼��");
		}
	}
}