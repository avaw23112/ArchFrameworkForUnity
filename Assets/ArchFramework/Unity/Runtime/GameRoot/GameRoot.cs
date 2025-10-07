using Arch.DI;
using Arch.Net;
using Arch.Resource;
using Arch.Tools;
using Assets.src.AOT.ECS.SystemScheduler;
using Cysharp.Threading.Tasks;
using Events;
using UnityEditor;
using UnityEngine;

namespace Arch.Runtime
{
	public class GameRoot
	{
		public class Setting
		{
			public static string SettingPath = "Assets/Data/Setting";
			public static string ResourseNameMapSettingPath = $"{SettingPath}/ResourceNameMap.asset";
			public static string ArchSettingPath = $"{SettingPath}/ArchBuildConfig.asset";

			public static string MainRuntime = "Assembly-CSharp";
			public static string MainEditor = "Assembly-CSharp-Editor";
			public static string AOT = "ArchFramework.Runtime";
			public static string Logic = "Code.Logic";
			public static string Protocol = "Code.Protocol";
			public static string Model = "Code.Model";
			public static string FullLink = "Code.FullLink";
		}

		// Entry point: initialize after the first scene is loaded
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static async void OnGameStart()
		{
			await Initialize();
			EventBus.Publish<GameStartEvent>(new GameStartEvent());
		}

		private static async UniTask Initialize()
		{
			//DIÈÝÆ÷×¢²á
			ArchKernel.Init(new UnityBootstrapModule());
			ILoadProgress loadProgress = ArchKernel.Resolve<ILoadProgress>();
			// Init logging
			ArchLog.SetLogger(ArchKernel.Resolve<IArchLogger>());
			loadProgress.Set(0.1f, "Initialize systems");

			// Init resource system
			ArchRes.SetProvider(ArchKernel.Resolve<IResProvider>());
			await ArchRes.InitializeAsync();
			loadProgress.Set(0.3f, "Loading resources");

			// Load assemblies
			Assemblys.SetLoader(ArchKernel.Resolve<IAssemblyLoader>());
			await Assemblys.LoadAssemblysAsync();

			loadProgress.Report(0.4f);
			// Register events
			EventBus.RegisterEvents();

			// Register components and serializers
			ComponentRegistryExtensions.RegisterAllComponents();
			ComponentSerializer.RegisterAllSerializers();

			// Collect and register attribute systems
			Attributes.Attributes.RemoveMapping();
			Attributes.Collector.CollectBaseAttributes();
			Attributes.Attributes.RegisterAttributeSystems();

			// Net init (set local ClientId and start session)

			// Register [System] systems
			loadProgress.Set(0.8f, "Register systems");
			SystemSorter.SetSorter(ArchKernel.Resolve<ISystemSorter>());
			ArchSystems.RegisterArchSystems(ArchKernel.Resolve<ISystemScheduler>());

			// Start systems
			ArchSystems.Start();
			ArchSystems.SubcribeEntityAwake();
			ArchSystems.SubcribeEntityDestroy();

#if UNITY_EDITOR
			EditorApplication.playModeStateChanged += state =>
			{
				if (state == PlayModeStateChange.ExitingPlayMode)
				{
					ArchSystems.Destroy();
				}
			};
#else
            Application.quitting += () =>
            {
                ArchSystems.Destroy();
            };
#endif
			loadProgress.Set(1f, "Startup complete");
		}
	}
}