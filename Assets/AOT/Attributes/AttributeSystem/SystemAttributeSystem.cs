using Arch.Tools;
using Attributes;
using System;

namespace Arch
{
	internal class SystemAttributeSystem : AttributeSystem<SystemAttribute>
	{
		public override void Process(SystemAttribute attribute, Type directType)
		{
			// 检查是否实现了 ISystem 或 IReactiveSystem 接口
			if (!typeof(ISystem).IsAssignableFrom(directType) && !typeof(IReactiveSystem).IsAssignableFrom(directType))
			{
				ArchLog.Error($"{directType.Name} 不是 ISystem 或 IReactiveSystem 的实现");
			}
		}
	}
}
