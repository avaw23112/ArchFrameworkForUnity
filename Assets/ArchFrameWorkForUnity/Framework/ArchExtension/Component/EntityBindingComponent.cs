using Arch.Core;
using System.Collections.Generic;

namespace Arch
{
    [Unique]
    public struct EntityBindingComponent : IComponent
    {
        public Dictionary<Entity, List<Entity>> dicEntitiesBindings;
    }
}