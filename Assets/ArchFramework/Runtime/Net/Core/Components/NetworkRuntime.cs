using Arch;
using Arch.Core;

namespace Arch.Net
{
    // Unique runtime holder to hook into ECS loop
    [Unique]
    public struct NetworkRuntime : IComponent
    {
        public Entity Self;
        public string Endpoint;
    }
}
