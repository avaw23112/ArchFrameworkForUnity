using Arch;
using Arch.Core;

namespace Assets.Scripts.Test.Net
{
    /// <summary>
    /// Spawn an entity with TestPosition to drive Sync scanning demo.
    /// </summary>
    [System]
    public sealed class TestPositionSpawnSystem : AwakeSystem<TestPosition>
    {
        protected override void Run(Entity entity, ref TestPosition component_T1)
        {
            // no-op: this Awake triggers when TestPosition is added
        }

        [System]
        public sealed class Bootstrap : IAwake
        {
            public void Awake()
            {
                var e = NamedWorld.DefaultWord.Create(new TestPosition { x = 0, y = 0, z = 0 });
            }
        }
    }
}

