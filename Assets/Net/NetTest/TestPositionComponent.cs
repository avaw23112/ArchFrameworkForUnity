using Arch;
using Arch.Core;
using Arch.Net;

namespace Assets.Scripts.Test.Net
{
    /// <summary>
    /// Simple value-type component used to validate Sync scan/apply pipeline.
    /// </summary>
    [NetworkSync]
    public struct TestPosition : IComponent
    {
        public float x;
        public float y;
        public float z;
    }
}

