#if UNITY_EDITOR

using Arch.Compilation.Editor;
using Arch.Resource;
using Arch.Tools;
using Attributes;
using UnityEditor;

/// <summary>
/// 统一启动入口：初始化配置、注册或调用必要的编辑期管线。
/// </summary>
[InitializeOnLoad]
public class EditorStart
{
	static EditorStart()
	{
		BuildEditorEnvironment();
		BuildEditorConfigPage();
	}

	public static void BuildEditorConfigPage()
	{
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

		// Page3: System可视化
		var systemsPage = new ConfigPage("系统可视化");
		systemsPage.RegisterSection(new PureAwakeSection());
		systemsPage.RegisterSection(new ReactiveAwakeSection());
		systemsPage.RegisterSection(new UpdateSection());
		systemsPage.RegisterSection(new LateUpdateSection());
		systemsPage.RegisterSection(new PureDestroySection());
		systemsPage.RegisterSection(new ReactiveDestroySection());
		ConfigSettingsProvider.RegisterPage(systemsPage);

		CsprojPatcher
			.Begin()
			.Target("Code.Protocol")
			.AddSourceFolder(@"..\Common\Protocol")
			.Apply();
		CsprojPatcher
			.Begin()
			.Target("Code.Model")
			.AddSourceFolder(@"Codes\Model")
			.Apply();
		CsprojPatcher
			.Begin()
			.Target("Code.Logic")
			.AddSourceFolder(@"Codes\Logic")
			.Apply();
	}

	public static void BuildEditorEnvironment()
	{
		//设置日志
		ArchLog.SetLogger(new UnityLogger());

		//初始化统一配置（首次自动创建 ScriptableObject 资产）
		ArchBuildConfig.LoadOrCreate();
		ArchRes.SetProvider(new UnityResProvider());
		ArchRes.InitializeAsync();

		//配置程序集，会用到运行时检查
		Assemblys.SetLoader(new UnityAssemblyLoader());
		Assemblys.LoadAssemblys();
		Collector.CollectBaseAttributes();

		//注册所有编译管线构建器
		AttributeTargetRegistry.RegisterAllRegistries();
	}
}

#endif