using HybridCLR.Editor;
using System.IO;
using System.Linq;

namespace Arch.Compilation.Editor
{
	[PostBuildProcessor]
	public class HybridCLRdllExporter : IUnitPostBuildProcessor
	{
		public string Name => "华佗热更新程序集导出器";

		public string Description => "自动导出华佗热更新的依赖集";

		public void Process(ArchBuildConfig cfg, string builtDllPath)
		{
			if (cfg == null || string.IsNullOrEmpty(cfg.compilePipeLineSetting.postExportDir)) return;
			if (!File.Exists(builtDllPath)) return;

			string exportRoot = Path.GetFullPath(SettingsUtil.HotUpdateDllsRootOutputDir);
			Directory.CreateDirectory(exportRoot);

			string asmName = Path.GetFileNameWithoutExtension(builtDllPath);
			string newName = $"{asmName}.dll";
			string platform = GetPlatformName();
			string dstPath = Path.Combine(exportRoot, platform, newName);

			// 拷贝DLL
			File.Copy(builtDllPath, dstPath, true);

			// 新增PDB拷贝逻辑
			string pdbSrcPath = Path.ChangeExtension(builtDllPath, ".pdb");
			if (File.Exists(pdbSrcPath))
			{
				string pdbDstPath = Path.ChangeExtension(dstPath, ".pdb");
				File.Copy(pdbSrcPath, pdbDstPath, true);
			}
		}

		public static string GetPlatformName()
		{
#if UNITY_EDITOR
			string platformFolder = SettingsUtil
				.GetHotUpdateDllsOutputDirByTarget(UnityEditor.EditorUserBuildSettings.activeBuildTarget)
				.Split('/', '\\').Last();
#else
    string platformFolder = Application.platform switch
    {
        RuntimePlatform.WindowsPlayer => "StandaloneWindows64",
        RuntimePlatform.OSXPlayer     => "StandaloneOSX",
        RuntimePlatform.IPhonePlayer  => "iOS",
        RuntimePlatform.Android       => "Android",
        RuntimePlatform.WebGLPlayer   => "WebGL",
        RuntimePlatform.LinuxPlayer   => "StandaloneLinux64",
        _ => Application.platform.ToString()
    };
#endif
			return platformFolder;
		}
	}
}