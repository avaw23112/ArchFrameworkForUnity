using Arch;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.Tools.Pool;

[System]
public class StateMechineAwakeSystem : AwakeSystem<StateMachineComponent>
{
	protected override void Run(Entity entity, ref StateMachineComponent component_T1)
	{
		component_T1.queueStateCommend = QueuePool<StateChangeEvent>.Get();
	}
}

[System]
public class StateMechineDestroySystem : DestroySystem<StateMachineComponent>
{
	protected override void Run(Entity entity, ref StateMachineComponent component_T1)
	{
		QueuePool<StateChangeEvent>.Release(component_T1.queueStateCommend);
	}
}

[System]
public class StateMachineLateUpdateSystem : LateUpdateSystem<StateMachineComponent>
{
	protected override void Run(Entity entity, ref StateMachineComponent component_T1)
	{
		if (entity.Has<PlayerTag>())
		{
			PlayerStatePriorityTreeComponent component = SingletonComponent.GetOrAdd<PlayerStatePriorityTreeComponent>();
			HierarchicalPriorityTree hierarchicalPriorityTree = component.playerStateProrityTree;

			while (component_T1.queueStateCommend.Count > 0)
			{
				long lNextStateKey = component_T1.queueStateCommend.Dequeue().nNextStateKey;
				long currentStateKey = component_T1.currentState.StateKey;
				if (hierarchicalPriorityTree.IsSwitchAllowed(currentStateKey, lNextStateKey))
				{
					component_T1.currentState.StateKey = lNextStateKey;
				}
			}
			component_T1.currentState.Exit();
			//component_T1.currentState = ¶ÔÓ¦×´Ì¬
			component_T1.currentState.Enter();
		}
	}
}
