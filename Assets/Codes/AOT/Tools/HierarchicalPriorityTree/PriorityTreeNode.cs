using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arch
{
	using System.Collections.Generic;

	/// <summary>
	/// 层级优先树的节点
	/// </summary>
	public class PriorityTreeNode
	{
		/// <summary>
		/// 节点的唯一标识
		/// </summary>
		public long Key { get; private set; }

		/// <summary>
		/// 节点的优先级
		/// </summary>
		public int Priority { get; set; }

		/// <summary>
		/// 子节点集合
		/// </summary>
		public List<PriorityTreeNode> Children { get; private set; }

		/// <summary>
		/// 父节点引用
		/// </summary>
		public PriorityTreeNode Parent { get; private set; }

		/// <summary>
		/// 构造函数
		/// </summary>
		/// <param name="key">节点唯一标识</param>
		/// <param name="priority">节点优先级</param>
		public PriorityTreeNode(long key, int priority)
		{
			Key = key;
			Priority = priority;
			Children = new List<PriorityTreeNode>();
			Parent = null;
		}

		/// <summary>
		/// 添加子节点
		/// </summary>
		/// <param name="child">要添加的子节点</param>
		public void AddChild(PriorityTreeNode child)
		{
			if (child == null) return;

			// 移除子节点之前的父节点关联
			if (child.Parent != null)
			{
				child.Parent.Children.Remove(child);
			}

			child.Parent = this;
			Children.Add(child);
		}

		/// <summary>
		/// 移除子节点
		/// </summary>
		/// <param name="child">要移除的子节点</param>
		/// <returns>是否移除成功</returns>
		public bool RemoveChild(PriorityTreeNode child)
		{
			if (child == null) return false;

			if (Children.Remove(child))
			{
				child.Parent = null;
				return true;
			}

			return false;
		}

		/// <summary>
		/// 检查是否包含指定Key的子节点（递归）
		/// </summary>
		public bool ContainsKey(long key)
		{
			return FindNode(key) != null;
		}

		/// <summary>
		/// 查找指定Key的节点（递归）
		/// </summary>
		public PriorityTreeNode FindNode(long key)
		{
			if (Key == key)
			{
				return this;
			}

			foreach (var child in Children)
			{
				var foundNode = child.FindNode(key);
				if (foundNode != null)
				{
					return foundNode;
				}
			}

			return null;
		}
	}

}
