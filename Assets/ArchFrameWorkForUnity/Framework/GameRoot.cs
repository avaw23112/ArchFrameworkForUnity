using Arch;
using Events;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts
{
    public class GameRoot
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnGameStart()
        {
            Arch.Tools.ArchLog.Initialize();
            EventBus.RegisterEvents();
            Attributes.Attributes.RegisterAttributeSystems();
            ArchSystems.RegisterEntitasSystems();
            ArchSystems.Instance.Start();
            ArchSystems.Instance.SubcribeEntityStart();
            ArchSystems.Instance.SubcribeEntityDestroy();
            ArchSystems.ApplyToPlayerLoop();

#if UNITY_EDITOR
            EditorApplication.playModeStateChanged +=
                (state) =>
                {
                    if (state == PlayModeStateChange.ExitingPlayMode)
                    {
                        ArchSystems.Instance.Destroy();
                        ArchSystems.ResetPlayerLoop();
                    }
                };
#else
			Application.quitting += () =>
			{
				ArchSystems.Instance.Destroy();
				ArchSystems.ResetPlayerLoop();
			};
#endif

            EventBus.Publish(new GameStarted());
        }
    }
}