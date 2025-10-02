
namespace Arch
{
	/// <summary>
	/// 层级优先树管理类
	/// </summary>
	public class HierarchicalPriorityTree
	{
		/// <summary>
		/// 根节点
		/// </summary>
		private PriorityTreeNode _rootNode;

		/// <summary>
		/// 构造函数
		/// </summary>
		public HierarchicalPriorityTree()
		{
			// 创建根节点，Key为0，优先级最低
			_rootNode = new PriorityTreeNode(0, int.MinValue);
		}

		/// <summary>
		/// 构造函数
		/// </summary>
		/// <param name="rootPriority">根节点优先级</param>
		public HierarchicalPriorityTree(int rootPriority)
		{
			_rootNode = new PriorityTreeNode(0, rootPriority);
		}

		/// <summary>
		/// 添加节点到树中
		/// </summary>
		/// <param name="parentKey">父节点Key</param>
		/// <param name="key">当前节点Key</param>
		/// <param name="priority">当前节点优先级</param>
		/// <returns>是否添加成功</returns>
		public bool AddNode(long parentKey, long key, int priority)
		{
			// 检查Key是否已存在
			if (FindNode(key) != null)
			{
				return false;
			}

			// 查找父节点
			var parentNode = FindNode(parentKey);
			if (parentNode == null)
			{
				return false;
			}

			// 创建并添加新节点
			var newNode = new PriorityTreeNode(key, priority);
			parentNode.AddChild(newNode);
			return true;
		}

		/// <summary>
		/// 从树中移除节点
		/// </summary>
		/// <param name="key">要移除的节点Key</param>
		/// <returns>是否移除成功</returns>
		public bool RemoveNode(long key)
		{
			if (key == 0) // 不能移除根节点
			{
				return false;
			}

			var node = FindNode(key);
			if (node == null || node.Parent == null)
			{
				return false;
			}

			return node.Parent.RemoveChild(node);
		}

		/// <summary>
		/// 查找节点
		/// </summary>
		/// <param name="key">节点Key</param>
		/// <returns>找到的节点，找不到返回null</returns>
		public PriorityTreeNode FindNode(long key)
		{
			return _rootNode.FindNode(key);
		}

		/// <summary>
		/// 更新节点优先级
		/// </summary>
		/// <param name="key">节点Key</param>
		/// <param name="newPriority">新的优先级</param>
		/// <returns>是否更新成功</returns>
		public bool UpdateNodePriority(long key, int newPriority)
		{
			var node = FindNode(key);
			if (node == null)
			{
				return false;
			}

			node.Priority = newPriority;
			return true;
		}

		/// <summary>
		/// 校验状态切换是否允许
		/// </summary>
		/// <param name="currentKey">当前状态Key</param>
		/// <param name="targetKey">目标状态Key</param>
		/// <returns>是否允许切换</returns>
		public bool IsSwitchAllowed(long currentKey, long targetKey)
		{
			// 相同状态，无需切换
			if (currentKey == targetKey)
			{
				return false;
			}

			// 查找当前节点和目标节点
			var currentNode = FindNode(currentKey);
			var targetNode = FindNode(targetKey);

			// 节点不存在则禁止切换
			if (currentNode == null || targetNode == null)
			{
				return false;
			}

			// 基础规则：目标节点优先级 >= 当前节点优先级
			// 可以根据需求修改此规则
			return targetNode.Priority >= currentNode.Priority;
		}

		/// <summary>
		/// 检查节点是否为另一个节点的祖先
		/// </summary>
		public bool IsAncestor(long ancestorKey, long nodeKey)
		{
			var node = FindNode(nodeKey);
			if (node == null)
			{
				return false;
			}

			var current = node.Parent;
			while (current != null)
			{
				if (current.Key == ancestorKey)
				{
					return true;
				}
				current = current.Parent;
			}

			return false;
		}

		/// <summary>
		/// 检查两个节点是否在同一分支上
		/// </summary>
		public bool IsInSameBranch(long key1, long key2)
		{
			return IsAncestor(key1, key2) || IsAncestor(key2, key1);
		}
	}

}
