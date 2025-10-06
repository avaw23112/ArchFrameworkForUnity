using Arch.Tools;
using Arch.Tools.Pool;
using Attributes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Arch.Compilation.Editor
{
	internal static class SystemRegistryHelper
	{
		public static IEnumerable<Type> RegisterTypes<Interface>()
		{
			List<(SystemAttribute, Type)> values = ListPool<(SystemAttribute, Type)>.Get();
			Collector.CollectAttributes(values);
			var result = values
				.Select(item => item.Item2)
				.Where(t => typeof(Interface).IsAssignableFrom(t)) // 过滤实现了 IPureAwake 的类型
				.ToList();
			ListPool<(SystemAttribute, Type)>.Release(values);
			return result;
		}
	}

	[TargetRegistry]
	public class PureAwakeSystemRegistry : BaseTargetRegistry<IPureAwake, SystemAttribute>
	{
		public override IEnumerable<Type> RegisterTypes()
		{
			return SystemRegistryHelper.RegisterTypes<IPureAwake>();
		}
	}

	[TargetRegistry]
	public class ReactiveAwakeSystemRegistry : BaseTargetRegistry<IReactiveAwake, SystemAttribute>
	{
		public override IEnumerable<Type> RegisterTypes()
		{
			return SystemRegistryHelper.RegisterTypes<IReactiveAwake>();
		}
	}

	[TargetRegistry]
	public class UpdateSystemRegistry : BaseTargetRegistry<IUpdate, SystemAttribute>
	{
		public override IEnumerable<Type> RegisterTypes()
		{
			return SystemRegistryHelper.RegisterTypes<IUpdate>();
		}
	}

	[TargetRegistry]
	public class LateUpdateSystemRegistry : BaseTargetRegistry<ILateUpdate, SystemAttribute>
	{
		public override IEnumerable<Type> RegisterTypes()
		{
			return SystemRegistryHelper.RegisterTypes<ILateUpdate>();
		}
	}

	[TargetRegistry]
	public class ReactiveDestroySystemRegistry : BaseTargetRegistry<IReactiveDestroy, SystemAttribute>
	{
		public override IEnumerable<Type> RegisterTypes()
		{
			return SystemRegistryHelper.RegisterTypes<IReactiveDestroy>();
		}
	}

	[TargetRegistry]
	public class PureDestroySystemRegistry : BaseTargetRegistry<IPureDestroy, SystemAttribute>
	{
		public override IEnumerable<Type> RegisterTypes()
		{
			return SystemRegistryHelper.RegisterTypes<IPureDestroy>();
		}
	}
}