using Arch;
using Assets.Scripts.Framework.Tools;
using Cysharp.Threading.Tasks;
using Events;
using UnityEngine;

namespace Assets.Scripts
{
	public class GameRoot : MonoSingleton<GameRoot>
	{
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static async void OnGameStart()
		{
			EventBus.RegisterEvents();
			await UniTask.Yield();

			Attributes.Attributes.RegisterAttributeSystems();
			await UniTask.Yield();

			ArchSystems.RegisterEntitasSystems();
			GameRoot.Instance.Init();
			await UniTask.Yield();

			EventBus.PublishAsync(new GameStarted());
		}

		protected override void OnStart()
		{
			ArchSystems.Instance.Start();
			ArchSystems.Instance.SubcribeEntityStart();
			ArchSystems.Instance.SubcribeEntityDestroy();
		}

		private void Update()
		{
			ArchSystems.Instance.Update();
		}

		private void LateUpdate()
		{
			ArchSystems.Instance.LateUpdate();
		}
		private void OnDestroy()
		{
			ArchSystems.Instance.Destroy();
		}
	}
}