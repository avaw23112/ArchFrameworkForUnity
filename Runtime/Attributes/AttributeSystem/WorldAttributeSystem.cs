using Arch.Core;
using Arch.Tools;
using Attributes;
using System;

namespace Arch
{
	internal class WorldAttributeSystem : AttributeSystem<WorldAttribute>
	{
		public override void Process(WorldAttribute attribute, Type derectType)
		{
			if (typeof(IGlobalSystem).IsAssignableFrom(derectType))
			{
				ArchLog.LogError($"系统：{derectType} 属于全局系统，不应该被World属性标记！");
				throw new Exception($"系统：{derectType} 属于全局系统，不应该被World属性标记！");
			}
			if (typeof(ISystem).IsAssignableFrom(derectType))
			{
				ArchLog.LogError($"类型：{derectType} 非可设置世界的系统，请检查实现！");
				return;
			}
			if (!typeof(IReactiveSystem).IsAssignableFrom(derectType))
			{
				ArchLog.LogError($"类型：{derectType} 非系统，请检查实现！");
				throw new Exception($"类型：{derectType} 非系统，请检查实现！");
			}
		}
	}
}