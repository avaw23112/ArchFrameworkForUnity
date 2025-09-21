using Arch;
using Attributes;
using Cysharp.Threading.Tasks;
using Events;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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
		private static async void OnGameStart()
		{
			Action<float> OnProgreess = null;
			Action<string> OnprogressTip = null;

			Loading(OnProgreess, OnprogressTip);
			await Initialize(OnProgreess, OnprogressTip);

			//发送完毕事件
			EventBus.Publish<GameStartEvent>(new GameStartEvent());
		}

		private static async Task Initialize(Action<float> OnProgreess, Action<string> OnprogressTip)
		{
			//初始化日志
			OnprogressTip?.Invoke("初始化系统");
			Arch.Tools.ArchLog.Initialize();
			OnProgreess?.Invoke(0.1f);

			//初始化资源管理系统
			await ArchRes.InitializeAsync();
			OnProgreess?.Invoke(0.3f);

			OnprogressTip?.Invoke("加载资源中");
			//加载热更新程序集
			await Assemblys.LoadAssemblys();
			OnProgreess?.Invoke(0.4f);
			//注册事件总线
			EventBus.RegisterEvents();
			//调度特性处理系统
			Attributes.Attributes.RegisterAttributeSystems();

			//初始化网络系统

			//初始化渲染系统

			//注册所有被标注[System]的系统
			ArchSystems.RegisterArchSystems();
			OnProgreess?.Invoke(0.8f);
			OnprogressTip?.Invoke("注册系统");

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
			OnProgreess?.Invoke(1f);
			OnprogressTip?.Invoke("加载完成");
		}

		private static void Loading(Action<float> OnProgreess, Action<string> OnprogressTip)
		{

		}
	}
}