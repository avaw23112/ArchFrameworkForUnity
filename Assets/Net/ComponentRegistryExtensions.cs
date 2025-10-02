using Arch.Core;
using Arch.Tools;
using Arch.Tools.Pool;
using Attributes;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Arch
{
    public static class ComponentRegistryExtensions
    {
        /// <summary>
        /// 根据组件Id获取组件类型
        /// </summary>
        /// <param name="typeId"></param>
        /// <returns></returns>
        public static Type GetType(int typeId) => ComponentRegistry.Types[typeId];

        /// <summary>
        /// 根据组件类型获取组件Id
        /// </summary>
        /// <param name="typeId"></param>
        /// <returns></returns>
        public static int GetTypeId(Type type)
        {
            if (ComponentRegistry.TryGet(type, out var componentType))
            {
                return componentType.Id;
            }
            else
            {
                ArchLog.LogWarning($"未找到组件类型：{type.FullName}");
                throw new InvalidOperationException($"未找到组件类型：{type.FullName}");
            }
        }

        /// <summary>
        /// 自动注册所有实现IComponent的类型（客户端与服务端启动时调用）
        /// </summary>
        public static void RegisterAllComponents()
        {
            List<Type> componentTypes = ListPool<Type>.Get();
            Collector.CollectTypes<IComponent>(componentTypes);
            // 按 FullName 排序，确保 TypeId 分配稳定一致
            componentTypes.Sort((a, b) => string.CompareOrdinal(a.FullName, b.FullName));

            // 2. 获取Unsafe.SizeOf方法用于计算值类型大小
            var sizeOfMethod = typeof(Unsafe).GetMethod("SizeOf");
            if (sizeOfMethod == null)
            {
                throw new InvalidOperationException("未找到Unsafe.SizeOf方法，无法计算组件大小");
            }
            try
            {
                // 3. 逐个注册组件类型
                foreach (var type in componentTypes)
                {
                    // 跳过已注册的类型
                    if (ComponentRegistry.TryGet(type, out _))
                    {
                        continue;
                    }

                    // 验证类型合法性（可选，根据框架要求添加）
                    if (!IsValidComponentType(type))
                    {
                        ArchLog.LogWarning($"跳过无效的IComponent类型：{type.FullName}");
                        continue;
                    }

                    // 计算组件内存大小
                    int byteSize = CalculateComponentSize(type, sizeOfMethod);

                    // 注册到ComponentRegistry，自动分配TypeId（Id = 当前注册数量 + 1）
                    ComponentRegistry.Add(type, new ComponentType(ComponentRegistry.Size + 1, byteSize));
                    ArchLog.LogDebug($"已注册组件类型：{type.FullName}，TypeId：{ComponentRegistry.Size}");
                }
            }
            catch (Exception e)
            {
                ArchLog.LogError(e);
                throw;
            }
            finally
            {
                // 释放临时列表（使用对象池优化性能）
                ListPool<Type>.Release(componentTypes);
            }
        }

        /// <summary>
        /// 验证组件类型是否合法（可根据业务需求扩展）
        /// </summary>
        private static bool IsValidComponentType(Type type)
        {
            // 示例：排除抽象类、接口（IComponent本身是接口，实现类不应是接口）
            if (type.IsInterface || type.IsAbstract)
            {
                return false;
            }

            // 示例：如果框架要求组件必须是结构体，可添加此判断
            // if (!type.IsValueType)
            // {
            //     return false;
            // }

            return true;
        }

        /// <summary>
        /// 计算组件的内存大小
        /// </summary>
        private static int CalculateComponentSize(Type type, MethodInfo sizeOfMethod)
        {
            if (type.IsValueType)
            {
                // 值类型：使用Unsafe.SizeOf<T>计算实际大小
                var genericMethod = sizeOfMethod.MakeGenericMethod(type);
                return (int)genericMethod.Invoke(null, null);
            }
            else
            {
                // 引用类型：使用指针大小（32位系统4字节，64位系统8字节）
                return IntPtr.Size;
            }
        }
    }
}
