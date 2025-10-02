using System;

namespace Arch.Net
{
    /// <summary>
    /// Logical session abstraction independent of transport implementation.
    /// </summary>
    public interface ISession : IDisposable
    {
        string SessionId { get; }
        ITransport Transport { get; }

        event Action OnConnect;
        event Action<string> OnDisconnect;
        event Action OnReconnect;
        event Action<string> OnNetworkUnstable;
        event Action OnUpdate;

        /// <summary>
        /// Attach a transport implementation.
        /// </summary>
        void AttachTransport(ITransport transport);
        /// <summary>
        /// Connect to endpoint.
        /// </summary>
        void Connect(string endpoint);
        /// <summary>
        /// Disconnect.
        /// </summary>
        void Disconnect(string reason = null);
        /// <summary>
        /// Send raw packet bytes.
        /// </summary>
        bool Send(byte[] data, int length);
        /// <summary>
        /// Pump transport events.
        /// </summary>
        void Update();
    }
}
