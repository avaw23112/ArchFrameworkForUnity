using System;

namespace Arch.Net
{
    /// <summary>
    /// Creates transports based on endpoint scheme.
    /// </summary>
    public static class TransportFactory
    {
        public static ITransport Create(string endpoint)
        {
            if (string.IsNullOrEmpty(endpoint)) throw new ArgumentNullException(nameof(endpoint));
            if (endpoint.StartsWith("lite://")) return new LiteNetLibTransport();
            if (endpoint.StartsWith("udp://")) throw new NotImplementedException("UDP transport not implemented");
            if (endpoint.StartsWith("tcp://")) throw new NotImplementedException("TCP transport not implemented");
            if (endpoint.StartsWith("kcp://")) throw new NotImplementedException("KCP transport not implemented");
            // default to lite
            return new LiteNetLibTransport();
        }
    }
}

