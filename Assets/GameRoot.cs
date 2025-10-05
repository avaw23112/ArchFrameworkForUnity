using Arch;
using Arch.Net;
using Arch.Tools;
using Attributes;
using Cysharp.Threading.Tasks;
using Events;
using System;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts
{
	public class GameRoot
	{
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

		private static async UniTask Initialize(Action<float> onProgress, Action<string> onProgressTip)
		{
			// Init logging
			onProgressTip?.Invoke("Initialize systems");
			Arch.Tools.ArchLog.Initialize();
			onProgress?.Invoke(0.1f);

			// Init resource system
			await ArchRes.InitializeAsync();
			onProgress?.Invoke(0.3f);
			onProgressTip?.Invoke("Loading resources");

			// Load hot assemblies
			await Assemblys.LoadAssemblys();
			onProgress?.Invoke(0.4f);

			// Register events
			EventBus.RegisterEvents();

			// Collect and register attribute systems
			Attributes.Collector.CollectBaseAttributesParallel();
			Attributes.Attributes.RegisterAttributeSystems();

			// Register components and serializers
			ComponentRegistryExtensions.RegisterAllComponents();
			ComponentSerializer.RegisterAllSerializers();

			// Net init (set local ClientId and start session)
			try
			{
				// Read local ClientId via env ARCH_CLIENT_ID; default 1
				int clientId = 1;
				var envId = Environment.GetEnvironmentVariable("ARCH_CLIENT_ID");
				if (!string.IsNullOrEmpty(envId) && int.TryParse(envId, out var parsed)) clientId = parsed;
				OwnershipService.MyClientId = clientId;

				// Pick endpoint: prefer NetworkConfig.DefaultEndpoint; fallback loopback
				var cfg = Arch.Net.NetworkSettings.Config;
				var endpoint = (cfg != null && !string.IsNullOrEmpty(cfg.DefaultEndpoint))
					? cfg.DefaultEndpoint
					: "loopback://local";

				// Seed NetworkRuntime to trigger NetworkAwakeSystem -> NetworkSingleton.EnsureInitialized
				Unique.Component<NetworkRuntime>.Set(new NetworkRuntime { Endpoint = endpoint });
				ArchLog.LogInfo($"[NetInit] ClientId={clientId} Endpoint={endpoint}");
			}
			catch (Exception ex)
			{
				ArchLog.LogWarning($"[NetInit] Init failed: {ex.Message}");
			}

			// Register [System] systems
			ArchSystems.RegisterArchSystems();
			onProgress?.Invoke(0.8f);
			onProgressTip?.Invoke("Register systems");

			// Start systems
			ArchSystems.Instance.Start();
			ArchSystems.Instance.SubcribeEntityStart();
			ArchSystems.Instance.SubcribeEntityDestroy();
			ArchSystems.ApplyToPlayerLoop();

#if UNITY_EDITOR
			EditorApplication.playModeStateChanged += state =>
			{
				if (state == PlayModeStateChange.ExitingPlayMode)
				{
					ArchSystems.ResetPlayerLoop();
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

		private static void Loading(Action<float> onProgress, Action<string> onProgressTip)
		{
		}
	}
}