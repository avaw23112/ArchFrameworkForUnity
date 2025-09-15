using Arch.Core;
using Schedulers;
using System;
using System.Collections.Generic;
using Tools;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace Arch
{
	public class ArchSystems : Singleton<ArchSystems>
	{
		protected readonly List<IAwake> m_listAwakeSystems = new List<IAwake>();
		protected readonly List<IUpdate> m_listUpdateSystems = new List<IUpdate>();
		protected readonly List<ILateUpdate> m_listLateUpdateSystems = new List<ILateUpdate>();
		protected readonly List<IDestroy> m_listDestroySystems = new List<IDestroy>();

		protected readonly List<IReactiveAwake> m_listReactiveAwakeSystems = new List<IReactiveAwake>();
		protected readonly List<IReactiveUpdate> m_listReactiveUpdateSystems = new List<IReactiveUpdate>();
		protected readonly List<IReactiveLateUpdate> m_listReactiveLateUpdateSystems = new List<IReactiveLateUpdate>();
		protected readonly List<IReactiveDestroy> m_listReactiveDestroySystems = new List<IReactiveDestroy>();

		protected JobScheduler jobScheduler;

		public static void RegisterEntitasSystems()
		{
			//获取所有标记了SystemAttribute的类
			Dictionary<Type, List<object>> dicSystems;
			Attributes.Attributes.TryGetDecrectType(typeof(SystemAttribute), out dicSystems);
			List<ISystem> listPureSystems = new List<ISystem>();
			List<IReactiveSystem> listReactiveSystems = new List<IReactiveSystem>();

			//创建默认世界
			NamedWorld.CreateNamed("Default");
			//设置并行线程配置
			Instance.jobScheduler = new JobScheduler(new JobScheduler.Config
			{
				ThreadPrefixName = "Arch.Extensions",
				ThreadCount = 0,
				MaxExpectedConcurrentJobs = 64,
				StrictAllocationMode = false,
			});
			World.SharedJobScheduler = Instance.jobScheduler;

			if (dicSystems == null || dicSystems.Count == 0)
			{
				Logger.Debug("系统中暂无System可创建");
				return;
			}

			foreach (var systemType in dicSystems.Keys)
			{
				if (systemType.IsAbstract || systemType.IsInterface)
				{
					continue;
				}
				var system = Activator.CreateInstance(systemType);
				if (system is ISystem pureSystem)
				{
					listPureSystems.Add(pureSystem);
				}
				else if (system is IReactiveSystem reactiveSystem)
				{
					//创建获取World属性，或者使用默认World
					object[] worldObjs = systemType.GetCustomAttributes(typeof(WorldAttribute), true);
					string worldName = worldObjs.Length > 0 ? (worldObjs[0] as WorldAttribute).worldName : "Default";
					if (string.IsNullOrEmpty(worldName))
					{
						throw new Exception($"系统中存在未指定World的ReactiveSystem: {systemType}");
					}
					World world = NamedWorld.GetNamed(worldName);
					//创建ReactiveSystem的载入点
					reactiveSystem.BuildIn(world);
					listReactiveSystems.Add(reactiveSystem);
				}
				else
				{
					Logger.Error($"系统中存在非系统类型: {systemType}");
					throw new Exception($"系统中存在非系统类型: {systemType}");
				}
			}

			Sorter.SortSystems(listPureSystems);
			Sorter.SortSystems(listReactiveSystems);

			foreach (var system in listPureSystems)
			{
				AddSystem(system);
			}
			foreach (var system in listReactiveSystems)
			{
				AddSystem(system);
			}

			listPureSystems.Clear();
			listReactiveSystems.Clear();
		}

		static PlayerLoopSystem playerOriginLoop;

		public static void ResetPlayerLoop()
		{
			PlayerLoop.SetPlayerLoop(playerOriginLoop);
		}

		public static void ApplyToPlayerLoop()
		{
			// 确保使用有效默认循环
			var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
			playerOriginLoop = PlayerLoop.GetCurrentPlayerLoop();


			// 创建精确的系统节点
			var updateSystem = new PlayerLoopSystem()
			{
				type = typeof(ArchSystems),
				updateDelegate = Instance.Update,
			};

			var lateUpdateSystem = new PlayerLoopSystem()
			{
				type = typeof(ArchSystems),
				updateDelegate = Instance.LateUpdate,
			};
			// 修改后的插入逻辑
			PlayerLoopSystem[] currentSystems = playerLoop.subSystemList;

			// 第一次插入：Update 阶段后
			for (int i = 0; i < currentSystems.Length; i++)
			{
				if (currentSystems[i].type == typeof(Update))
				{
					currentSystems = currentSystems.InsertAfter(i, updateSystem);
					break; // 找到第一个后退出
				}
			}

			// 第二次插入：PreLateUpdate 阶段后 
			for (int i = 0; i < currentSystems.Length; i++)
			{
				if (currentSystems[i].type == typeof(PreLateUpdate))
				{
					currentSystems = currentSystems.InsertAfter(i, lateUpdateSystem);
					break;
				}
			}

			playerLoop.subSystemList = currentSystems;

			// 设置并验证
			PlayerLoop.SetPlayerLoop(playerLoop);
		}


		private static void AddSystem(ISystem system)
		{
			if (system is IAwake StartSystem)
				Instance.m_listAwakeSystems.Add(StartSystem);
			if (system is IUpdate UpdateSystem)
				Instance.m_listUpdateSystems.Add(UpdateSystem);
			if (system is ILateUpdate LateUpdateSystem)
				Instance.m_listLateUpdateSystems.Add(LateUpdateSystem);
			if (system is IDestroy DestroySystem)
				Instance.m_listDestroySystems.Add(DestroySystem);
		}
		private static void AddSystem(IReactiveSystem system)
		{
			if (system is IReactiveAwake ReactiveAwakeSystem)
				Instance.m_listReactiveAwakeSystems.Add(ReactiveAwakeSystem);
			if (system is IReactiveUpdate ReactiveUpdateSystem)
				Instance.m_listReactiveUpdateSystems.Add(ReactiveUpdateSystem);
			if (system is IReactiveLateUpdate ReactiveLateUpdateSystem)
				Instance.m_listReactiveLateUpdateSystems.Add(ReactiveLateUpdateSystem);
			if (system is IReactiveDestroy ReactiveDestroySystem)
				Instance.m_listReactiveDestroySystems.Add(ReactiveDestroySystem);
		}

		#region 调用区

		public void Start()
		{
			try
			{
				for (var i = 0; i < m_listAwakeSystems.Count; i++)
				{
					m_listAwakeSystems[i].Awake();
				}
			}
			catch (Exception e)
			{
				Logger.Error($"Start System: {e.Message}");
				throw;
			}
		}
		public void SubcribeEntityStart()
		{
			try
			{
				for (var i = 0; i < m_listReactiveAwakeSystems.Count; i++)
				{
					m_listReactiveAwakeSystems[i].SubcribeEntityAwake();
				}
			}
			catch (Exception e)
			{
				Logger.Error($"Start System: {e.Message}");
				throw;
			}
		}

		public void Update()
		{
			try
			{
				for (var i = 0; i < m_listUpdateSystems.Count; i++)
				{
					m_listUpdateSystems[i].Update();
				}
				for (var i = 0; i < m_listReactiveUpdateSystems.Count; i++)
				{
					m_listReactiveUpdateSystems[i].Update();
				}
			}
			catch (Exception e)
			{
				Logger.Error($"Update System: {e.Message}");
				throw;
			}
		}

		public void LateUpdate()
		{
			try
			{
				for (var i = 0; i < m_listUpdateSystems.Count; i++)
				{
					m_listLateUpdateSystems[i].LateUpdate();
				}
				for (var i = 0; i < m_listReactiveUpdateSystems.Count; i++)
				{
					m_listReactiveLateUpdateSystems[i].LateUpdate();
				}
			}
			catch (Exception e)
			{
				Logger.Error($"LateUpdate System: {e.Message}");
				throw;
			}
		}

		public void Destroy()
		{
			try
			{
				for (var i = 0; i < m_listDestroySystems.Count; i++)
				{
					m_listDestroySystems[i].Destroy();
				}
			}
			catch (Exception e)
			{
				Logger.Error($"Destroy System: {e.Message}");
				throw;
			}
			finally
			{
				jobScheduler.Dispose();
			}
		}

		public void SubcribeEntityDestroy()
		{
			try
			{
				for (var i = 0; i < m_listReactiveDestroySystems.Count; i++)
				{
					m_listReactiveDestroySystems[i].SubcribeEntityDestroy();
				}
			}
			catch (Exception e)
			{
				Logger.Error($"Destroy System: {e.Message}");
				throw;
			}
		}

		#endregion 调用区
	}
}