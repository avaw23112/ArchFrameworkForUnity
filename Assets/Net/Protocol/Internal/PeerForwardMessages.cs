using MemoryPack;

namespace Arch.Net.ProtocolInternal
{
    [MemoryPackable]
    public partial class PeerForwardMsg
    {
        public string TargetPeerId;
        public string SourcePeerId;
        public byte[] Payload;
        public byte Ttl;

        public PeerForwardMsg() {}
        [MemoryPackConstructor]
        public PeerForwardMsg(string targetPeerId, string sourcePeerId, byte[] payload, byte ttl = 16)
        {
            TargetPeerId = targetPeerId;
            SourcePeerId = sourcePeerId;
            Payload = payload;
            Ttl = ttl;
        }
    }
}
