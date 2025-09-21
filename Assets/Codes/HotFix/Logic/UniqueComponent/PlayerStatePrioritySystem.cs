using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arch
{
	public class PlayerStatePrioritySystem : UniqueComponentSystem<PlayerStatePriorityTreeComponent>
	{
		protected override void OnAwake(ref PlayerStatePriorityTreeComponent component)
		{
			//在这里初始化角色的状态优先树
			component.playerStateProrityTree = new HierarchicalPriorityTree();
		}

		protected override void OnDestroy(ref PlayerStatePriorityTreeComponent component)
		{
		}
	}
}
