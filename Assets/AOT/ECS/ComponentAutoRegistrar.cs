using Arch;
using Arch.Core;
using Arch.Tools.Pool;
using Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

public static class ComponentAutoRegistrar
{
	/// <summary>
	/// 自动注册所有实现IComponent的类型（客户端与服务端启动时调用）
	/// </summary>
	public static void RegisterAllIComponents()
	{
		// 1. 反射查找所有实现IComponent的类型（struct或class）
		List<Type> componentTypes = ListPool<Type>.Get();
		Collector.CollectTypesParallel<IComponent>(componentTypes);

		var method = typeof(Unsafe).GetMethod("SizeOf");
		// 3. 逐个注册到ComponentRegistry
		foreach (var type in componentTypes)
		{
			// 跳过已注册类型
			if (ComponentRegistry.TryGet(type, out _))
				continue;
			var GenericMethod = method.MakeGenericMethod(type);
			// 计算ByteSize（值类型与引用类型区分处理）
			int byteSize = type.IsValueType
				? (int)GenericMethod.Invoke(null, null) // 值类型用实际大小
				: IntPtr.Size;         // 引用类型用指针大小

			// 创建ComponentType并注册（Id由注册顺序自动分配）
			ComponentRegistry.Add(type, new ComponentType(ComponentRegistry.Size + 1, byteSize));
		}
	}
}
