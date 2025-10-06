using Arch.Net;
using Arch.Resource;
using Arch.Tools;
using Cysharp.Threading.Tasks;
using Events;
using System;
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

			public static string AOT = "ArchFramework.Runtime";
			public static string Logic = "Code.Logic";
			public static string Protocol = "Code.Protocol";
			public static string Model = "Code.Model";
		}

		// Entry point: initialize after the first scene is loaded
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static async void OnGameStart()
		{
			Action<float> onProgress = null;
			Action<string> onProgressTip = null;

			Loading(onProgress, onProgressTip);
			await Initialize(onProgress, onProgressTip);

			// Broadcast game start
			EventBus.Publish<GameStartEvent>(new GameStartEvent());
		}

		private static void Loading(Action<float> onProgress, Action<string> onProgressTip)
		{
		}

		private static async UniTask Initialize(Action<float> onProgress, Action<string> onProgressTip)
		{
			// Init logging
			onProgressTip?.Invoke("Initialize systems");
			ArchLog.SetLogger(new UnityLogger());
			onProgress?.Invoke(0.1f);

			// Init resource system
			ArchRes.SetProvider(new UnityResProvider());
			await ArchRes.InitializeAsync();
			onProgress?.Invoke(0.3f);
			onProgressTip?.Invoke("Loading resources");

			// Load assemblies
			Assemblys.SetLoader(new UnityAssemblyLoader());
			Assemblys.LoadAssemblys();

			onProgress?.Invoke(0.4f);
			// Register events
			EventBus.RegisterEvents();

			// Register components and serializers
			ComponentRegistryExtensions.RegisterAllComponents();
			ComponentSerializer.RegisterAllSerializers();

			// Collect and register attribute systems
			Attributes.Collector.CollectBaseAttributesParallel();
			Attributes.Attributes.RegisterAttributeSystems();

			// Net init (set local ClientId and start session)

			// Register [System] systems
			onProgress?.Invoke(0.8f);
			onProgressTip?.Invoke("Register systems");

			ArchSystems.RegisterArchSystems(new UnityPlayerLoopScheduler());

			// Start systems
			ArchSystems.Instance.Start();
			ArchSystems.Instance.SubcribeEntityAwake();
			ArchSystems.Instance.SubcribeEntityDestroy();

#if UNITY_EDITOR
			EditorApplication.playModeStateChanged += state =>
			{
				if (state == PlayModeStateChange.ExitingPlayMode)
				{
					ArchSystems.Instance.Destroy();
				}
			};
#else
            Application.quitting += () =>
            {
                ArchSystems.Instance.Destroy();
            };
#endif
			onProgress?.Invoke(1f);
			onProgressTip?.Invoke("Startup complete");
		}
	}
}