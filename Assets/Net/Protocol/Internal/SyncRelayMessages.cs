using MemoryPack;

namespace Arch.Net.ProtocolInternal
{
    [MemoryPackable]
    public partial class SyncRelayMsg
    {
        public string SourcePeerId;
        public byte[] Packet;
        public byte Ttl;

        public SyncRelayMsg() {}
        [MemoryPackConstructor]
        public SyncRelayMsg(string sourcePeerId, byte[] packet, byte ttl = 16)
        {
            SourcePeerId = sourcePeerId;
            Packet = packet;
            Ttl = ttl;
        }
    }
}

