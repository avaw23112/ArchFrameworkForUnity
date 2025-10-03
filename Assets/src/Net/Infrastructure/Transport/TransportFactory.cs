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

            if (endpoint.StartsWith("loopback://", StringComparison.OrdinalIgnoreCase) ||
                endpoint.StartsWith("local://", StringComparison.OrdinalIgnoreCase))
            {
                return new MockLoopbackTransport();
            }

            if (endpoint.StartsWith("lite://", StringComparison.OrdinalIgnoreCase)) return new LiteNetLibTransport();
            if (endpoint.StartsWith("udp://", StringComparison.OrdinalIgnoreCase)) throw new NotImplementedException("UDP transport not implemented");
            if (endpoint.StartsWith("tcp://", StringComparison.OrdinalIgnoreCase)) throw new NotImplementedException("TCP transport not implemented");
            if (endpoint.StartsWith("kcp://", StringComparison.OrdinalIgnoreCase)) throw new NotImplementedException("KCP transport not implemented");

            // Fallback to loopback to keep editor workflow functional when scheme is unknown.
            return new MockLoopbackTransport();
        }
    }
}

