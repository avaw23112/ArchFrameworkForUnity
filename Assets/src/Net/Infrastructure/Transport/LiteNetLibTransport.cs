using System;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;

namespace Arch.Net
{
    /// <summary>
    /// LiteNetLib-based transport client.
    /// </summary>
    public sealed class LiteNetLibTransport : ITransport, INetEventListener
    {
        public TransportState State { get; private set; } = TransportState.Disconnected;
        public string Endpoint { get; private set; }
        public int LastRtt { get; private set; }

        public event Action Connected;
        public event Action<string> Disconnected;
        public event Action<byte[]> DataReceived;
        public event Action<string> NetworkUnstable;
        public event Action<int> LatencyUpdated;

        // Private fields follow naming: m_ + (p for object / v for value) + type tag
        private NetManager m_pClient;
        private NetPeer m_pPeer;
        private string m_szHost;
        private int m_nPort;

        public void Configure(string endpoint)
        {
            Endpoint = endpoint;
            ParseEndpoint(endpoint, out m_szHost, out m_nPort);
        }

        public void Connect()
        {
            if (m_pClient != null)
            {
                Disconnect("reconnect");
            }
            State = TransportState.Connecting;
            m_pClient = new NetManager(this)
            {
                UnconnectedMessagesEnabled = false,
                UnsyncedEvents = true,
                UpdateTime = 15,
                AutoRecycle = true,
            };
            m_pClient.Start();
            m_pClient.Connect(m_szHost, m_nPort, key: "arch");
        }

        public void Disconnect(string reason = null)
        {
            if (m_pClient == null) return;
            m_pPeer?.Disconnect();
            m_pClient.Stop();
            m_pPeer = null;
            m_pClient = null;
            State = TransportState.Disconnected;
            Disconnected?.Invoke(reason ?? "disconnect");
        }

        /// <summary>
        /// Send data selecting DeliveryMethod based on PacketType and config.
        /// </summary>
        public bool Send(byte[] data, int length)
        {
            if (State != TransportState.Connected || m_pPeer == null || data == null) return false;
            if (length <= 0) return true; // nothing to send

            // Choose delivery based on packet type (byte[1])
            DeliveryMethod vMethod = DeliveryMethod.ReliableOrdered;
            if (length >= 2)
            {
                var vType = (PacketType)data[1];
                switch (vType)
                {
                    case PacketType.Sync:
                        // Sync allows loss; prefer low-latency channel.
                        vMethod = Arch.Net.NetworkSettings.Config.SyncDelivery;
                        break;
                    case PacketType.Rpc:
                        vMethod = Arch.Net.NetworkSettings.Config.RpcDelivery;
                        break;
                    default:
                        vMethod = DeliveryMethod.ReliableUnordered;
                        break;
                }
            }

            m_pPeer.Send(data, 0, length, vMethod);
            Arch.Net.NetworkStats.RecordSend(length);
            return true;
        }

        public void Poll()
        {
            m_pClient?.PollEvents();
        }

        public void Dispose()
        {
            Disconnect("dispose");
        }

        private static void ParseEndpoint(string endpoint, out string host, out int port)
        {
            // format: lite://host:port
            host = "127.0.0.1";
            port = 9050;
            if (string.IsNullOrEmpty(endpoint)) return;
            var s = endpoint;
            if (s.StartsWith("lite://")) s = s.Substring(7);
            var idx = s.LastIndexOf(':');
            if (idx > 0)
            {
                host = s.Substring(0, idx);
                if (!int.TryParse(s.Substring(idx + 1), out port)) port = 9050;
            }
        }

        // INetEventListener
        public void OnPeerConnected(NetPeer peer)
        {
            m_pPeer = peer;
            State = TransportState.Connected;
            Connected?.Invoke();
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo info)
        {
            m_pPeer = null;
            var reason = info.Reason.ToString();
            State = TransportState.Disconnected;
            Disconnected?.Invoke(reason);
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError error)
        {
            NetworkUnstable?.Invoke(error.ToString());
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            var bytes = reader.GetRemainingBytes();
            DataReceived?.Invoke(bytes);
            reader.Recycle();
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            // not used
            reader.Recycle();
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {
            LastRtt = latency;
            LatencyUpdated?.Invoke(latency);
        }

        public void OnConnectionRequest(ConnectionRequest request)
        {
            // client only
            request.Reject();
        }
    }
}

