using System;
using System.Collections.Concurrent;

namespace Arch.Net
{
    /// <summary>
    /// In-memory loopback transport for local echo/testing without external dependencies.
    /// </summary>
    public sealed class MockLoopbackTransport : ITransport
    {
        // m_ + p(object)
        private readonly ConcurrentQueue<byte[]> m_pInbox = new ConcurrentQueue<byte[]>();
        public TransportState State { get; private set; } = TransportState.Disconnected;
        public string Endpoint { get; private set; }
        public int LastRtt { get; private set; }

        public event Action Connected;
        public event Action<string> Disconnected;
        public event Action<byte[]> DataReceived;
        public event Action<string> NetworkUnstable;
        public event Action<int> LatencyUpdated;

        /// <summary>
        /// Configure endpoint for loopback transport.
        /// </summary>
        public void Configure(string endpoint)
        {
            Endpoint = endpoint;
        }

        /// <summary>
        /// Immediate connect (loopback).
        /// </summary>
        public void Connect()
        {
            State = TransportState.Connected;
            Connected?.Invoke();
        }

        /// <summary>
        /// Disconnect.
        /// </summary>
        public void Disconnect(string reason = null)
        {
            if (State == TransportState.Disconnected) return;
            State = TransportState.Disconnected;
            Disconnected?.Invoke(reason ?? "user");
        }

        /// <summary>
        /// Enqueue a copy of the buffer for local loopback.
        /// </summary>
        public bool Send(byte[] data, int length)
        {
            if (State != TransportState.Connected || data == null) return false;
            var pCopy = new byte[length];
            System.Buffer.BlockCopy(data, 0, pCopy, 0, length);
            m_pInbox.Enqueue(pCopy);
            return true;
        }

        /// <summary>
        /// Drain inbox and raise DataReceived.
        /// </summary>
        public void Poll()
        {
            while (m_pInbox.TryDequeue(out var pPkt))
            {
                DataReceived?.Invoke(pPkt);
            }
        }

        public void Dispose()
        {
            Disconnect("dispose");
        }
    }
}
