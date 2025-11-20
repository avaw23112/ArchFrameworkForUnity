#if UNITY_EDITOR

using Arch.Tools;
using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Callbacks;

namespace Arch.Compilation.Editor
{
	public class ArchConfigPostPlayerBuild : IPreprocessBuildWithReport
	{
		public ArchBuildConfig Config => ArchBuildConfig.LoadOrCreate();

		public int callbackOrder => -1000;

		/// <summary>
		/// 在构建后覆盖原ScriptAssembly，让IL2cpp使用自定义编译管线处理后的dll程序集完成To cpp化
		/// </summary>
		/// <param name="obj"></param>
		public void OnPreprocessBuild(BuildReport report)
		{
			ArchLog.LogInfo("构建开始");
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
				return;
			}
			ArchLog.LogInfo("构建结束");
		}
	}
}

#endif