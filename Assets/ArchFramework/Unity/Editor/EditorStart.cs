#if UNITY_EDITOR

using Arch.Compilation.Editor;
using Arch.Tools;
using UnityEditor;

/// <summary>
/// 统一启动入口：初始化配置、注册或调用必要的编辑期管线。
/// </summary>
[InitializeOnLoad]
public class EditorStart
{
	static EditorStart()
	{
		ArchLog.SetLogger(new UnityLogger());
		// 1) 初始化统一配置（首次自动创建 ScriptableObject 资产）
		ArchBuildConfig.LoadOrCreate();

		// 2) 注册可扩展配置页（这里注册默认节，可自由扩展）
		var hotReloadPage = new ConfigPage("热重载设置");
		hotReloadPage.RegisterSection(new HotReloadSection());
		ConfigSettingsProvider.RegisterPage(hotReloadPage);

		// Page1：基础构建
		var buildPage = new ConfigPage("构建设置");
		buildPage.RegisterSection(new AssemblyModeSection());
		ConfigSettingsProvider.RegisterPage(buildPage);

		// Page2：编译后处理
		var postPage = new ConfigPage("编译管线");
		postPage.RegisterSection(new BuildPipelineVisualizerSection());
		postPage.RegisterSection(new PreBuildProcessorSection());
		postPage.RegisterSection(new PostBuildProcessorSection());
		postPage.RegisterSection(new GlobalPostProcessorSection());
		ConfigSettingsProvider.RegisterPage(postPage);

		CsprojPatcher
			.Begin()
			.Target("Protocol")
			.AddSourceFolder(@"..\Common\Protocol")
			.Apply();
	}
}

#endif