using System;

namespace Arch.Net
{
    /// <summary>
    /// Connection state of the underlying transport.
    /// </summary>
    public enum TransportState
    {
        Disconnected,
        Connecting,
        Connected,
        Reconnecting,
    }

    /// <summary>
    /// Transport abstraction; implementations provide event-driven networking.
    /// </summary>
    public interface ITransport : IDisposable
    {
        TransportState State { get; }
        string Endpoint { get; }
        int LastRtt { get; }

        event Action Connected;
        event Action<string> Disconnected; // reason
        event Action<byte[]> DataReceived; // raw packet
        event Action<string> NetworkUnstable; // hint
        event Action<int> LatencyUpdated;

        /// <summary>
        /// Configure transport with an endpoint string (e.g., lite://host:port).
        /// </summary>
        void Configure(string endpoint);
        /// <summary>
        /// Open the connection using the configured endpoint.
        /// </summary>
        void Connect();
        /// <summary>
        /// Close the connection.
        /// </summary>
        void Disconnect(string reason = null);
        /// <summary>
        /// Send a packet buffer.
        /// </summary>
        bool Send(byte[] data, int length);
        /// <summary>
        /// Pump underlying events (invoke from main thread).
        /// </summary>
        void Poll(); // pump events
    }
}
