using Arch.Tools;
using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Arch.Net
{
	public sealed class LiteNetLibTransport : ITransport, INetEventListener
	{
		private NetManager _net;
		private readonly Dictionary<NetPeer, ConnectionId> _peerToConn = new();
		private readonly Dictionary<int, NetPeer> _connToPeer = new();
		private int _nextConnId = 1;
		private bool _isServer;
		private string _connectionKey;

		public bool IsServer => _isServer;
		public bool IsClient => !_isServer;

		public event Action<ConnectionId> OnConnected;

		public event Action<ConnectionId, DisconnectReason, string> OnDisconnected;

		public event Action<ConnectionId, PooledBuffer> OnReciveData;

		public void StartServer(TransportConfig config)
		{
			_isServer = true;
			_connectionKey = config.ConnectionKey; // ✅ 保存密钥
			_net = new NetManager(this)
			{
				IPv6Enabled = config.IPv6,
				UpdateTime = config.UpdateIntervalMs,
				AutoRecycle = true
			};
			_net.Start(config.Port);
		}

		public void StartClient(TransportConfig config)
		{
			_isServer = false;
			_connectionKey = config.ConnectionKey; // ✅ 保存密钥
			_net = new NetManager(this)
			{
				IPv6Enabled = config.IPv6,
				UpdateTime = config.UpdateIntervalMs,
				AutoRecycle = true
			};
			_net.Start();
			_net.Connect(config.Address, config.Port, config.ConnectionKey);
		}

		public void Stop()
		{
			_net?.Stop();
			_peerToConn.Clear();
			_connToPeer.Clear();
			_net = null;
		}

		public void Poll() => _net?.PollEvents();

		// ------------------- Send 系列 -------------------

		public bool Send(ConnectionId conn, ArraySegment<byte> payload, Delivery delivery, byte channel = 0)
			=> Send(conn, payload.AsMemory(), delivery, channel);

		public bool Send(ConnectionId conn, ReadOnlyMemory<byte> payload, Delivery delivery, byte channel = 0)
		{
			if (!_connToPeer.TryGetValue(conn.Value, out var peer) || peer.ConnectionState != ConnectionState.Connected)
				return false;
			peer.Send(payload.Span, channel, ConvertDelivery(delivery));
			return true;
		}

		public bool Send(ConnectionId conn, ReadOnlySpan<byte> payload, Delivery delivery, byte channel = 0)
		{
			if (!_connToPeer.TryGetValue(conn.Value, out var peer) || peer.ConnectionState != ConnectionState.Connected)
				return false;
			peer.Send(payload, channel, ConvertDelivery(delivery));
			return true;
		}

		// ------------------- Broadcast 系列 -------------------

		public int Broadcast(ArraySegment<byte> payload, Delivery delivery, byte channel = 0)
			=> Broadcast(payload.AsSpan(), delivery, channel);

		public int Broadcast(ReadOnlyMemory<byte> payload, Delivery delivery, byte channel = 0)
			=> Broadcast(payload.Span, delivery, channel);

		public int Broadcast(ReadOnlySpan<byte> payload, Delivery delivery, byte channel = 0)
		{
			int count = 0;
			foreach (var kv in _peerToConn)
			{
				var peer = kv.Key;
				if (peer.ConnectionState == ConnectionState.Connected)
				{
					peer.Send(payload, channel, ConvertDelivery(delivery));
					count++;
				}
			}
			return count;
		}

		// ------------------- 内部 -------------------

		private static DeliveryMethod ConvertDelivery(Delivery d) => d switch
		{
			Delivery.Unreliable => DeliveryMethod.Unreliable,
			Delivery.UnreliableSequenced => DeliveryMethod.Sequenced,
			Delivery.ReliableUnordered => DeliveryMethod.ReliableUnordered,
			Delivery.ReliableOrdered => DeliveryMethod.ReliableOrdered,
			_ => DeliveryMethod.ReliableOrdered
		};

		// ------------------- LiteNetLib 回调 -------------------

		void INetEventListener.OnPeerConnected(NetPeer peer)
		{
			var conn = new ConnectionId(_nextConnId++);
			_peerToConn[peer] = conn;
			_connToPeer[conn.Value] = peer;
			OnConnected?.Invoke(conn);
		}

		void INetEventListener.OnPeerDisconnected(NetPeer peer, DisconnectInfo info)
		{
			if (_peerToConn.Remove(peer, out var conn))
				_connToPeer.Remove(conn.Value);
			OnDisconnected?.Invoke(conn, (DisconnectReason)info.Reason, info.AdditionalData?.ToString());
		}

		void INetEventListener.OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
		{
			if (!_peerToConn.TryGetValue(peer, out var conn))
				return;

			int len = reader.AvailableBytes;
			var src = reader.RawData;
			int offset = reader.UserDataOffset;

			var owner = MemoryCache.Rent(len);
			src.AsSpan(offset, len).CopyTo(owner.Memory.Span);

			reader.Recycle();
			OnReciveData?.Invoke(conn, new PooledBuffer(owner, owner.Memory[..len]));
		}

		void INetEventListener.OnConnectionRequest(ConnectionRequest request)
		{
			if (_isServer)
				request.AcceptIfKey(_connectionKey);
			else
				request.Reject();
		}

		void INetEventListener.OnNetworkError(IPEndPoint endPoint, SocketError socketError)
		{
		}

		void INetEventListener.OnNetworkLatencyUpdate(NetPeer peer, int latency)
		{
		}

		void INetEventListener.OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
		{
		}

		public void Dispose() => Stop();
	}
}