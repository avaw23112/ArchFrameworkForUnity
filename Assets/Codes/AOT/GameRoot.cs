using Arch;
using Attributes;
using Cysharp.Threading.Tasks;
using Events;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts
{
	//初始化完毕事件
	public struct GameStartEvent
	{

	}

	public class GameRoot
	{
		//切忌改变初始化顺序
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private async static void OnGameStart()
		{
			//初始化日志
			Arch.Tools.ArchLog.Initialize();

			//初始化资源管理系统
			await ArchRes.InitializeAsync();
			GameStartSetting gameStartSetting = Resources.Load<GameStartSetting>("GameStartSetting");
			//插入初始化界面，回调更新进度
			Resources.UnloadAsset(gameStartSetting);
			//检查资源更新
			if (gameStartSetting.isRemoteUpdate)
			{
				bool isCanUpdate = await ArchRes.CheckForUpdatesAsync();
				if (isCanUpdate)
				{
					await ArchRes.DownloadUpdatesAsync();
				}
			}

			//加载热更新程序集
			await Assemblys.LoadAssemblys();
			//注册事件总线
			EventBus.RegisterEvents();
			//调度特性处理系统
			Attributes.Attributes.RegisterAttributeSystems();

			//初始化网络系统

			//初始化渲染系统

			//注册所有被标注[System]的系统
			ArchSystems.RegisterArchSystems();

			//启动系统工作流
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
			};
#endif

			//发送完毕事件
			EventBus.Publish<GameStartEvent>(new GameStartEvent());
		}
	}
}