using MemoryPack;

namespace Arch.Net.ProtocolInternal
{
    [MemoryPackable]
    public partial class RoutingForceConnectMsg
    {
        public string Endpoint;
        public RoutingForceConnectMsg() {}
        [MemoryPackConstructor]
        public RoutingForceConnectMsg(string endpoint) { Endpoint = endpoint; }
    }
}
