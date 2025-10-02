using System.Collections.Generic;
using MemoryPack;

namespace Arch.Net.ProtocolInternal
{
    [MemoryPackable]
    public partial class PeerEntry
    {
        public string PeerId;
        public string Endpoint;
        public int Weight;

        public PeerEntry() {}
        [MemoryPackConstructor]
        public PeerEntry(string peerId, string endpoint, int weight)
        {
            PeerId = peerId; Endpoint = endpoint; Weight = weight;
        }
    }

    [MemoryPackable]
    public partial class TopologyAdvertMsg
    {
        public List<PeerEntry> Entries;
        public TopologyAdvertMsg() {}
        [MemoryPackConstructor]
        public TopologyAdvertMsg(List<PeerEntry> entries) { Entries = entries; }
    }

    [MemoryPackable]
    public partial class EdgeEntry
    {
        public string A;
        public string B;
        public int Rtt;
        public float Loss;
        public float Jitter;

        public EdgeEntry() {}
        [MemoryPackConstructor]
        public EdgeEntry(string a, string b, int rtt, float loss, float jitter)
        {
            A = a; B = b; Rtt = rtt; Loss = loss; Jitter = jitter;
        }
    }

    [MemoryPackable]
    public partial class TopologyMetricsMsg
    {
        public List<EdgeEntry> Entries;
        public TopologyMetricsMsg() {}
        [MemoryPackConstructor]
        public TopologyMetricsMsg(List<EdgeEntry> entries) { Entries = entries; }
    }
}
