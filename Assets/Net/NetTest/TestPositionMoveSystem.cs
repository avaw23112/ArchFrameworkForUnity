using Arch;
using Arch.Core;
using Arch.Core.Extensions;

namespace Assets.Scripts.Test.Net
{
    /// <summary>
    /// Animate TestPosition so that Sync scanner has changing values.
    /// </summary>
    [System]
    public sealed class TestPositionMoveSystem : UpdateSystem<TestPosition>
    {
        protected override void Run(Entity entity, ref TestPosition component_T1)
        {
            component_T1.x += 0.01f;
            component_T1.y += 0.02f;
            component_T1.z += 0.03f;
            entity.Set(in component_T1);
        }
    }
}

