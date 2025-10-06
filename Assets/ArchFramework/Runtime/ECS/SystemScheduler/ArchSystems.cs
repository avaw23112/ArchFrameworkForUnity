using Arch.Core;
using Arch.Tools;
using Assets.src.AOT.ECS.SystemScheduler;
using Schedulers;
using System;
using System.Collections.Generic;

namespace Arch
{
	public class ArchSystems : Singleton<ArchSystems>
	{
		// -------------------- 系统集合 --------------------
		private readonly List<IAwake> m_awakes = new();

		private readonly List<IUpdate> m_updates = new();
		private readonly List<ILateUpdate> m_lateUpdates = new();
		private readonly List<IDestroy> m_destroys = new();

		private readonly List<IReactiveAwake> m_rAwakes = new();
		private readonly List<IReactiveUpdate> m_rUpdates = new();
		private readonly List<IReactiveLateUpdate> m_rLateUpdates = new();
		private readonly List<IReactiveDestroy> m_rDestroys = new();

		private JobScheduler jobScheduler;
		private ISystemScheduler scheduler;

		// -------------------- 初始化 --------------------
		public static void RegisterSystemInternal()
		{
			var inst = Instance;
			inst.ResetAll();
			NamedWorld.ClearEvents();

			// 加载并行任务配置
			inst.jobScheduler = new JobScheduler(new JobScheduler.Config
			{
				ThreadPrefixName = "Arch.Scheduler",
				ThreadCount = 0,
				MaxExpectedConcurrentJobs = 64,
				StrictAllocationMode = false,
			});
			World.SharedJobScheduler = inst.jobScheduler;

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

			Sorter.SortSystems(pureList);
			Sorter.SortSystems(reactiveList);

			foreach (var s in pureList)
				AddSystem(s);
			foreach (var s in reactiveList)
				AddSystem(s);

			pureList.Clear();
			reactiveList.Clear();
		}

		public static void RegisterArchSystems(ISystemScheduler externalScheduler = null)
		{
			RegisterSystemInternal();
			Instance.scheduler = externalScheduler ?? new DefaultSystemScheduler();
			Instance.scheduler.Start(Instance.Update, Instance.LateUpdate);
			ArchLog.LogInfo("ArchSystems 初始化完成。");
		}

		public static void ReloadArchSystem()
		{
			if (Instance.scheduler == null)
			{
				ArchLog.LogError("ArchSystems 未存在任务调度器！");
				return;
			}
			RegisterSystemInternal();
			ArchLog.LogInfo("ArchSystems 初始化完成。");
		}

		private void ResetAll()
		{
			m_awakes.Clear(); m_updates.Clear(); m_lateUpdates.Clear(); m_destroys.Clear();
			m_rAwakes.Clear(); m_rUpdates.Clear(); m_rLateUpdates.Clear(); m_rDestroys.Clear();
		}

		// -------------------- 添加系统 --------------------

		private static void AddSystem(ISystem sys)
		{
			var i = Instance;
			if (sys is IAwake a) i.m_awakes.Add(a);
			if (sys is IUpdate u) i.m_updates.Add(u);
			if (sys is ILateUpdate l) i.m_lateUpdates.Add(l);
			if (sys is IDestroy d) i.m_destroys.Add(d);
		}

		private static void AddSystem(IReactiveSystem sys)
		{
			var i = Instance;
			if (sys is IReactiveAwake a) i.m_rAwakes.Add(a);
			if (sys is IReactiveUpdate u) i.m_rUpdates.Add(u);
			if (sys is IReactiveLateUpdate l) i.m_rLateUpdates.Add(l);
			if (sys is IReactiveDestroy d) i.m_rDestroys.Add(d);
		}

		// -------------------- 生命周期 --------------------

		public void Start()
		{
			foreach (var s in m_awakes) s.Awake();
		}

		public void Update()
		{
			foreach (var s in m_updates) s.Update();
			foreach (var s in m_rUpdates) s.Update();
		}

		public void LateUpdate()
		{
			foreach (var s in m_rLateUpdates) s.LateUpdate();
			foreach (var s in m_lateUpdates) s.LateUpdate();
		}

		public void Destroy()
		{
			foreach (var s in m_destroys) s.Destroy();
			Unique.World.TearDown();
			jobScheduler?.Dispose();
			scheduler?.Stop();
		}

		public void SubcribeEntityAwake()
		{
			foreach (var s in m_rAwakes) s.SubcribeEntityAwake();
		}

		public void SubcribeEntityDestroy()
		{
			foreach (var s in m_rDestroys) s.SubcribeEntityDestroy();
		}
	}
}