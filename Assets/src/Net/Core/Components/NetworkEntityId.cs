using Arch.Core;

namespace Arch.Net
{
    /// <summary>
    /// Network entity identifier.
    /// </summary>
    [NetworkSync]
    public struct NetworkEntityId : IComponent
    {
        public ulong Value;
    }
}
