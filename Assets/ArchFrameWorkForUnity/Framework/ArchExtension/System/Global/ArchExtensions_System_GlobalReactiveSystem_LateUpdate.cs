using Arch.Core;


namespace Arch
{
	public abstract class GlobalLateUpdateSystem : GlobalReactiveSystem, IReactiveLateUpdate
	{
		public void LateUpdate()
		{
			foreach (World worldNamed in NamedWorld.Instance.NamedWorlds)
			{
				QueryDescription query = Filter();
				worldNamed.Query(in query, entity =>
				{
					if (GetTrigger(entity))
						Run(entity);
				});
				commandBuffer.Playback(worldNamed);
			}
		}
	}

	public abstract class GlobalLateUpdateSystem<T> : GlobalReactiveSystem<T>, IReactiveLateUpdate where T : struct, IComponent
	{

		public void LateUpdate()
		{
			foreach (World worldNamed in NamedWorld.Instance.NamedWorlds)
			{
				QueryDescription query = Filter();
				world.Query(in query, (Entity e, ref T c) =>
				{
					if (GetTrigger(e))
						Run(e, ref c);
				});
				commandBuffer.Playback(world);
			}
		}
	}

	public abstract class GlobalLateUpdateSystem<T1, T2> : GlobalReactiveSystem<T1, T2>, IReactiveLateUpdate
		where T1 : struct, IComponent where T2 : struct, IComponent
	{
		public void LateUpdate()
		{
			foreach (World worldNamed in NamedWorld.Instance.NamedWorlds)
			{
				QueryDescription query = Filter();
				worldNamed.Query(in query, (Entity e, ref T1 c1, ref T2 c2) =>
				{
					if (GetTrigger(e))
						Run(e, ref c1, ref c2);
				});
				commandBuffer.Playback(worldNamed);
			}
		}
	}

	public abstract class GlobalLateUpdateSystem<T1, T2, T3> : GlobalReactiveSystem<T1, T2, T3>, IReactiveLateUpdate
		where T1 : struct, IComponent where T2 : struct, IComponent where T3 : struct, IComponent
	{

		public void LateUpdate()
		{
			foreach (World worldNamed in NamedWorld.Instance.NamedWorlds)
			{
				QueryDescription query = Filter();
				worldNamed.Query(in query, (Entity e, ref T1 c1, ref T2 c2, ref T3 c3) =>
				{
					if (GetTrigger(e))
						Run(e, ref c1, ref c2, ref c3);
				});
				commandBuffer.Playback(worldNamed);
			}
		}
	}
	// 4 参数版本
	public abstract class GlobalLateUpdateSystem<T1, T2, T3, T4> : GlobalReactiveSystem<T1, T2, T3, T4>, IReactiveLateUpdate
		where T1 : struct, IComponent
		where T2 : struct, IComponent
		where T3 : struct, IComponent
		where T4 : struct, IComponent
	{


		public void LateUpdate()
		{
			foreach (World worldNamed in NamedWorld.Instance.NamedWorlds)
			{
				QueryDescription query = Filter();
				worldNamed.Query(in query, (Entity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4) =>
				{
					if (GetTrigger(e))
						Run(e, ref c1, ref c2, ref c3, ref c4);
				});
				commandBuffer.Playback(worldNamed);
			}
		}
	}

	// 5 参数版本
	public abstract class GlobalLateUpdateSystem<T1, T2, T3, T4, T5> : GlobalReactiveSystem<T1, T2, T3, T4, T5>, IReactiveLateUpdate
		where T1 : struct, IComponent
		where T2 : struct, IComponent
		where T3 : struct, IComponent
		where T4 : struct, IComponent
		where T5 : struct, IComponent
	{
		public void LateUpdate()
		{
			foreach (World worldNamed in NamedWorld.Instance.NamedWorlds)
			{
				QueryDescription query = Filter();
				worldNamed.Query(in query, (Entity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5) =>
				{
					if (GetTrigger(e))
						Run(e, ref c1, ref c2, ref c3, ref c4, ref c5);
				});
				commandBuffer.Playback(worldNamed);
			}
		}
	}

	// 6 参数版本
	public abstract class GlobalLateUpdateSystem<T1, T2, T3, T4, T5, T6> : GlobalReactiveSystem<T1, T2, T3, T4, T5, T6>, IReactiveLateUpdate
		where T1 : struct, IComponent
		where T2 : struct, IComponent
		where T3 : struct, IComponent
		where T4 : struct, IComponent
		where T5 : struct, IComponent
		where T6 : struct, IComponent
	{
		public void LateUpdate()
		{
			foreach (World worldNamed in NamedWorld.Instance.NamedWorlds)
			{
				QueryDescription query = Filter();
				worldNamed.Query(in query, (Entity e, ref T1 c1, ref T2 c2, ref T3 c3, ref T4 c4, ref T5 c5, ref T6 c6) =>
				{
					if (GetTrigger(e))
						Run(e, ref c1, ref c2, ref c3, ref c4, ref c5, ref c6);
				});
				commandBuffer.Playback(worldNamed);
			}
		}
	}

}
