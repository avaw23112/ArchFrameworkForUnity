using System;
using System.Collections.Generic;
using System.Linq;

namespace Arch
{
    internal class Sorter
    {
        internal static void SortSystems<T>(List<T> systems)
        {
            if (systems == null) throw new ArgumentNullException(nameof(systems));
            if (systems.Count == 0) return;

            // 为每个系统建立依赖追踪器
            var nodes = systems.ToDictionary(
                s => s.GetType(),
                s => new ObjectNode(s)
            );

            SortInternal(nodes);

            // 按最终权重排序
            systems.Sort((a, b) =>
            {
                var aNode = nodes[a.GetType()];
                var bNode = nodes[b.GetType()];
                return aNode.MinOrder.CompareTo(bNode.MinOrder);
            });
        }

        private static void SortInternal(Dictionary<Type, ObjectNode> nodes)
        {
            // 建立完整依赖链
            foreach (var node in nodes.Values)
            {
                var type = node.System.GetType();

                // First属性处理（强制最低优先级）
                if (type.GetCustomAttributes(typeof(FirstAttribute), false).Length > 0)
                {
                    foreach (var other in nodes.Values.Where(n => n != node))
                    {
                        other.MinOrder = Math.Max(other.MinOrder, node.MinOrder + 1);
                        node.MaxOrder = Math.Min(node.MaxOrder, other.MinOrder - 1);
                    }
                    continue;
                }

                // Last属性处理（强制最高优先级）
                if (type.GetCustomAttributes(typeof(LastAttribute), false).Length > 0)
                {
                    foreach (var other in nodes.Values.Where(n => n != node))
                    {
                        other.MaxOrder = Math.Min(other.MaxOrder, node.MaxOrder - 1);
                        node.MinOrder = Math.Max(node.MinOrder, other.MaxOrder + 1);
                    }
                    continue;
                }

                // 处理 After 依赖
                foreach (var after in (AfterAttribute[])type.GetCustomAttributes(typeof(AfterAttribute), false))
                {
                    if (nodes.TryGetValue(after.At, out var afterNode))
                    {
                        // 当前系统必须晚于 afterNode
                        node.MinOrder = Math.Max(node.MinOrder, afterNode.MinOrder + 1);
                        afterNode.MaxOrder = Math.Min(afterNode.MaxOrder, node.MinOrder - 1);
                    }
                }

                // 处理 Before 依赖
                foreach (var before in (BeforeAttribute[])type.GetCustomAttributes(typeof(BeforeAttribute), false))
                {
                    if (nodes.TryGetValue(before.At, out var beforeNode))
                    {
                        // 当前系统必须早于 beforeNode
                        node.MaxOrder = Math.Min(node.MaxOrder, beforeNode.MaxOrder - 1);
                        beforeNode.MinOrder = Math.Max(beforeNode.MinOrder, node.MaxOrder + 1);
                    }
                }
            }

            // 动态调整权重（处理交叉依赖）
            bool changed;
            do
            {
                changed = false;
                foreach (var node in nodes.Values)
                {
                    foreach (var other in nodes.Values.Where(n => n != node))
                    {
                        if (node.MinOrder > other.MaxOrder)
                        {
                            var newMin = other.MaxOrder + 1;
                            if (node.MinOrder < newMin)
                            {
                                node.MinOrder = newMin;
                                changed = true;
                            }
                        }

                        if (node.MaxOrder < other.MinOrder)
                        {
                            int newMax = other.MinOrder - 1;
                            if (node.MaxOrder > newMax)
                            {
                                node.MaxOrder = newMax;
                                changed = true;
                            }
                        }
                    }
                }
            } while (changed);


        }

        private class ObjectNode
        {
            public object System { get; }
            public int MinOrder { get; set; } = int.MinValue;
            public int MaxOrder { get; set; } = int.MaxValue;

            public ObjectNode(object system)
            {
                System = system;
            }
        }
    }
}