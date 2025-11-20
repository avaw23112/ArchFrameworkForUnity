using Arch.Core;
using Arch.Tools;
using Assets.src.AOT.ECS.SystemScheduler;
using Schedulers;
using System;
using System.Collections.Generic;

namespace Arch
{
	public static class ArchSystems
	{
		// -------------------- 系统集合 --------------------

		private static readonly List<IPureAwake> m_pAwakes = new();
		private static readonly List<IUpdate> m_updates = new();
		private static readonly List<IPureDestroy> m_pDestroys = new();

		private static readonly List<IReactiveAwake> m_rAwakes = new();
		private static readonly List<ILateUpdate> m_lateUpdates = new();
		private static readonly List<IReactiveDestroy> m_rDestroys = new();

		private static JobScheduler jobScheduler;
		private static ISystemScheduler scheduler;

		// -------------------- 初始化 --------------------
		public static void RegisterSystemInternal()
		{
			ResetAll();
			NamedWorld.ClearEvents();

			// 加载并行任务配置
			jobScheduler = new JobScheduler(new JobScheduler.Config
			{
				ThreadPrefixName = "Arch.Scheduler",
				ThreadCount = 0,
				MaxExpectedConcurrentJobs = 64,
				StrictAllocationMode = false,
			});
			World.SharedJobScheduler = jobScheduler;

			// 扫描系统
			if (!Attributes.Attributes.TryGetDecrectType(typeof(SystemAttribute), out var dicSystems)
				|| dicSystems.Count == 0)
			{
				ArchLog.LogInfo("系统中暂无可注册的 System。");
				return;
			}

			var pureList = new List<ISystem>();
			var reactiveList = new List<IReactiveSystem>();

			foreach (var sysType in dicSystems.Keys)
			{
				if (sysType.IsAbstract || sysType.IsInterface) continue;
				var sys = Activator.CreateInstance(sysType);
				if (sys is ISystem ps)
				{
					pureList.Add(ps);
				}
				if (sys is IReactiveSystem rs)
				{
					var worldAttr = sysType.GetCustomAttributes(typeof(WorldAttribute), true);
					string worldName = worldAttr.Length > 0 ? ((WorldAttribute)worldAttr[0]).worldName : "Default";
					if (string.IsNullOrEmpty(worldName))
						throw new Exception($"ReactiveSystem 未指定 World: {sysType}");
					rs.BuildIn(NamedWorld.GetNamed(worldName));
					reactiveList.Add(rs);
				}
			}

			foreach (var s in pureList)
				AddSystem(s);
			foreach (var s in reactiveList)
				AddSystem(s);

			// 改为门面接口调用：
			SystemSorter.SortSystems(m_pAwakes);
			SystemSorter.SortSystems(m_rAwakes);
			SystemSorter.SortSystems(m_updates);
			SystemSorter.SortSystems(m_lateUpdates);
			SystemSorter.SortSystems(m_rDestroys);
			SystemSorter.SortSystems(m_pDestroys);

			//在这里根据可视化提供的数据进行排序
			pureList.Clear();
			reactiveList.Clear();
		}

		public static void RegisterArchSystems(ISystemScheduler externalScheduler = null)
		{
			RegisterSystemInternal();
			scheduler = externalScheduler ?? new DefaultSystemScheduler();
			scheduler.Start(Update, LateUpdate);
			ArchLog.LogInfo("ArchSystems 初始化完成。");
		}

		public static void ReloadArchSystem()
		{
			if (scheduler == null)
			{
				ArchLog.LogError("ArchSystems 未存在任务调度器！");
				return;
			}
			RegisterSystemInternal();
			ArchLog.LogInfo("ArchSystems 初始化完成。");
		}

		private static void ResetAll()
		{
			m_pAwakes.Clear(); m_lateUpdates.Clear();
			m_updates.Clear(); m_pDestroys.Clear();
			m_rAwakes.Clear(); m_rDestroys.Clear();
		}

		// -------------------- 添加系统 --------------------
		private static void AddSystem(ISystem sys)
		{
			if (sys is IPureAwake a) m_pAwakes.Add(a);
			if (sys is IUpdate u) m_updates.Add(u);
			if (sys is IPureLateUpdate l) m_lateUpdates.Add(l);
			if (sys is IPureDestroy d) m_pDestroys.Add(d);
			ArchLog.LogInfo($"[System] {sys.GetType().Name} has been regist");
		}

		private static void AddSystem(IReactiveSystem sys)
		{
			if (sys is IReactiveAwake a) m_rAwakes.Add(a);
			if (sys is IUpdate u) m_updates.Add(u);
			if (sys is IPureLateUpdate l) m_lateUpdates.Add(l);
			if (sys is IReactiveDestroy d) m_rDestroys.Add(d);
			ArchLog.LogInfo($"[System] {sys.GetType().Name} has been regist");
		}

		// -------------------- 生命周期 --------------------

		public static void Start()
		{
			foreach (var s in m_pAwakes) s.Awake();
		}

		public static void Update()
		{
			try
			{
				foreach (var s in m_updates)
					s.Update();
			}
			catch (Exception e)
			{
				ArchLog.LogError(e);
			}
		}

		public static void LateUpdate()
		{
			try
			{
				foreach (var s in m_lateUpdates)
					s.LateUpdate();
			}
			catch (Exception e)
			{
				ArchLog.LogError(e);
			}
		}

		public static void Destroy()
		{
			Unique.World.TearDown();
			foreach (var s in m_pDestroys)
				s.Destroy();
			jobScheduler?.Dispose();
			scheduler?.Stop();
		}

		public static void SubcribeEntityAwake()
		{
			foreach (var s in m_rAwakes) s.SubcribeEntityAwake();
		}

		public static void SubcribeEntityDestroy()
		{
			foreach (var s in m_rDestroys) s.SubcribeEntityDestroy();
		}
	}
}