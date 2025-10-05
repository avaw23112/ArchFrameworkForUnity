using Arch.Core;
using System.Collections.Generic;
using Unity.Collections;

namespace Arch
{
	[Unique]
	public struct EntityBindingComponent : IComponent
	{
		public NativeMultiHashMap<Entity, Entity> dicEntitiesBindings;
	}
}