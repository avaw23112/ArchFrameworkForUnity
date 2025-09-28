using Arch.Core;
using Arch.Tools;
using Arch.Tools.Pool;
using Attributes;
using MemoryPack;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Arch.Net
{
    public static class ComponentSerializer
    {
        public static void RegisterAllSerializers()
        {
            List<Type> componentTypes = ListPool<Type>.Get();
            Collector.CollectTypes<IComponent>(componentTypes);

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

                    //如果该类型需要序列化，则创建实例触发序列化器注册
                    if (isMarkedSerialized(type))
                        Activator.CreateInstance(type);
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

        private static bool isMarkedSerialized(Type type)
        {
            return type.GetCustomAttributes(typeof(MemoryPackableAttribute), false).Length > 0;
        }

        public static void Deserialize<T>(byte[] bytes, ref T component)
        {
            if (component == null)
            {
                return;
            }
            MemoryPackSerializer.Deserialize(bytes, ref component);
        }

        public static byte[] Serialize<T>(T component)
        {
            return MemoryPackSerializer.Serialize(component);
        }

        public static void TestSerialize<T>(T component)
        {
            int typeId = ComponentRegistryExtensions.GetTypeId(component.GetType());
            byte[] bytes = Serialize(component);
            TestDeserialize(typeId, bytes);
        }

        public static void TestDeserialize(int typeId, byte[] bytes)
        {
            Type type = ComponentRegistryExtensions.GetType(typeId);
            if (type == null)
            {
                throw new Exception("未找到类型Id为" + typeId + "的组件类型");
            }

            MethodInfo method = typeof(MemoryPackSerializer).GetMethod("Deserialize", new Type[] { typeof(byte[]), type.MakeByRefType() });
            if (method == null)
            {
                throw new Exception("未找到反序列化方法");
            }
        }
    }
}