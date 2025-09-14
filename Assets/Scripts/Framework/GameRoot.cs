using Arch;
using Cysharp.Threading.Tasks;
using Events;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts
{
	public class GameRoot
	{
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static async void OnGameStart()
		{
			Tools.Logger.Initialize();
			await UniTask.Yield();

			EventBus.RegisterEvents();
			await UniTask.Yield();

			Attributes.Attributes.RegisterAttributeSystems();
			await UniTask.Yield();

			ArchSystems.RegisterEntitasSystems();
			ArchSystems.ApplyToPlayerLoop();

#if UNITY_EDITOR
			EditorApplication.playModeStateChanged +=
				(state) =>
				{
					if (state == PlayModeStateChange.ExitingPlayMode)
						ArchSystems.ResetPlayerLoop();
				};
#else
						Application.quitting += () =>
						{
							ArchSystems.ResetPlayerLoop();
						};
#endif

			await UniTask.Yield();

			EventBus.Publish(new GameStarted());
		}

	}
}