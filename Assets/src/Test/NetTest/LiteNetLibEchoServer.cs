using System;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace Arch.Net
{
    /// <summary>
    /// Minimal echo server to allow local testing in play mode.
    /// </summary>
    public sealed class LiteNetLibEchoServer : MonoBehaviour, INetEventListener
    {
        [SerializeField] private int m_nPort = 9050;
        [SerializeField] private bool m_vAutoStart = true;

        // m_ + p(object)
        private NetManager m_pServer;

        void Start()
        {
            if (m_vAutoStart) StartServer();
        }

        public void StartServer()
        {
            if (m_pServer != null) return;
            m_pServer = new NetManager(this)
            {
                UnsyncedEvents = true,
                AutoRecycle = true,
            };
            m_pServer.Start(m_nPort);
        }

        public void StopServer()
        {
            if (m_pServer == null) return;
            m_pServer.Stop();
            m_pServer = null;
        }

        void Update()
        {
            m_pServer?.PollEvents();
        }

        void OnDestroy()
        {
            StopServer();
        }

        // INetEventListener
        public void OnPeerConnected(NetPeer peer) { }
        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo info) { }
        public void OnNetworkError(IPEndPoint endPoint, SocketError error) { }
        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { reader.Recycle(); }
        public void OnConnectionRequest(ConnectionRequest request) { request.AcceptIfKey("arch"); }
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod deliveryMethod)
        {
            var data = reader.GetRemainingBytes();
            // echo back with same delivery method
            peer.Send(data, deliveryMethod);
            reader.Recycle();
        }
    }
}

