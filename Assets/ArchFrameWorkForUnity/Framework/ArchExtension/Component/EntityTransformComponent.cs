using Arch.Core;
using System.Collections.Generic;

namespace Arch
{
    public struct EntityTransform : IComponent
    {
        public Entity parent;
        public List<Entity> entities;
    }
}