using Arch.Core;
using Arch.Tools;
using Arch.Tools.Pool;
using MemoryPack;
using System;
using System.Collections.Generic;

namespace Arch.Net
{
	public static class ComponentSerializer
	{
		public static void RegisterAllSerializers()
		{
			List<Type> componentTypes = ListPool<Type>.Get();
			Attributes.Collector.CollectTypes<IComponent>(componentTypes);

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
	}
}