using Arch.Core;
using System.Collections.Generic;
using UnityEngine;

namespace Arch
{
    public struct ViewComponent : IViewComponent
    {
        public GameObject gameObject;
    }

    [Unique]
    public struct EntityBindingComponent : IModelComponent
    {
        public Dictionary<Entity, List<Entity>> dicEntitiesBindings;
    }

    public struct EntityTransform : IModelComponent
    {
        public Entity parent;
        public List<Entity> entities;
    }
}