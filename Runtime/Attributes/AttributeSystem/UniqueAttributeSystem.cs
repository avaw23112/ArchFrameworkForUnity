using Arch.Core;
using Arch.Tools;
using Attributes;
using System;

namespace Arch
{
	[NotHotReload]
	internal class UniqueAttributeSystem : AttributeSystem<UniqueAttribute>
	{
		public override void Process(UniqueAttribute attribute, Type directType)
		{
			if (directType.IsClass || directType.IsAbstract)
			{
				ArchLog.LogError($"{directType} is not struct");
				throw new Exception($"{directType} is not struct");
			}
			if (directType.GetInterface(nameof(IComponent)) == null)
			{
				ArchLog.LogError($"{directType} is not component");
				throw new Exception($"{directType} is not component");
			}
			if (ComponentRegistry.TryGet(directType, out ComponentType componentType))
			{
				Unique.Component.Set(componentType, Activator.CreateInstance(directType));
			}
		}
	}
}