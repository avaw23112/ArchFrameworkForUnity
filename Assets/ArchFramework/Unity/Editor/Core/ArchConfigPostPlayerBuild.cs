using Arch.Tools;
using UnityEditor.Callbacks;

namespace Arch.Compilation.Editor
{
	public static class ArchConfigPostPlayerBuild
	{
		public static ArchBuildConfig Config => ArchBuildConfig.LoadOrCreate();

		/// <summary>
		/// 在构建后覆盖原ScriptAssembly，让IL2cpp使用自定义编译管线处理后的dll程序集完成To cpp化
		/// </summary>
		/// <param name="obj"></param>
		[PostProcessBuild]
		public static void PostBuild(object obj)
		{
			//从当前配置中获取是什么编译模式
			var buildMode = Config.buildSetting.buildMode;

			//启用编译
			bool ok;
			if (buildMode == BuildSetting.AssemblyBuildMode.Isolated)
			{
				ok = AssemblyBuilderPipeline.BuildIsolated(Config);
			}
			else
			{
				ok = AssemblyBuilderPipeline.BuildFullLink(Config);
			}
			if (ok == false)
			{
				ArchLog.LogError("构建失败");
			}
		}
	}
}