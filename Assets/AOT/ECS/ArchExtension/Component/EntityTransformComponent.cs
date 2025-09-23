using Arch.Core;
using System.Collections.Generic;
using Unity.Collections;

namespace Arch
{
	public struct EntityTransform : IComponent
	{
		public Entity parent;
		public NativeList<Entity> entities;
	}
}