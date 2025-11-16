using Arch.Tools;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Arch.Net
{
	/// <summary>
	/// 纯内存本地回环传输层，用于测试或无网络环境。
	/// </summary>
	public sealed class MockLoopbackTransport : ITransport
	{
		private static readonly ConcurrentDictionary<int, MockLoopbackTransport> s_servers = new();
		private bool _started;
		private int _id;

		public bool IsServer { get; private set; }
		public bool IsClient => !IsServer;

		public event Action<ConnectionId> OnConnected;

		public event Action<ConnectionId, DisconnectReason, string> OnDisconnected;

		public event Action<ConnectionId, PooledBuffer> OnReciveData;

		public void StartServer(TransportConfig config)
		{
			IsServer = true;
			_id = config.Port;
			s_servers[_id] = this;
			_started = true;
		}

		public void StartClient(TransportConfig config)
		{
			IsServer = false;
			_id = config.Port;
			_started = true;

			if (s_servers.TryGetValue(config.Port, out var server))
			{
				var conn = new ConnectionId(1);
				OnConnected?.Invoke(conn);
				server.OnConnected?.Invoke(conn);
			}
		}

		public void Stop()
		{
			if (IsServer)
				s_servers.TryRemove(_id, out _);
			_started = false;
		}

		public void Poll()
		{ /* 回环无需轮询 */ }

		public bool Send(ConnectionId conn, ArraySegment<byte> payload, Delivery delivery, byte channel = 0)
			=> Send(conn, payload.AsMemory(), delivery, channel);

		public bool Send(ConnectionId conn, ReadOnlyMemory<byte> payload, Delivery delivery, byte channel = 0)
		{
			if (!_started) return false;

			// 找目标
			var target = IsServer
				? s_servers.TryGetValue(_id, out var srv) ? srv : null
				: s_servers.TryGetValue(_id, out var server) ? server : null;

			if (target == null)
				return false;

			var owner = MemoryCache.Rent(payload.Length);
			payload.CopyTo(owner.Memory);
			var buf = new PooledBuffer(owner, owner.Memory.Slice(0, payload.Length));

			// 模拟异步传输延迟
			ThreadPool.QueueUserWorkItem(_ =>
			{
				target.OnReciveData?.Invoke(conn, buf);
			});

			return true;
		}

		public bool Send(ConnectionId conn, ReadOnlySpan<byte> payload, Delivery delivery, byte channel = 0)
		{
			var arr = new byte[payload.Length];
			payload.CopyTo(arr);
			return Send(conn, arr.AsMemory(), delivery, channel);
		}

		public int Broadcast(ArraySegment<byte> payload, Delivery delivery, byte channel = 0)
			=> Send(new ConnectionId(1), payload, delivery, channel) ? 1 : 0;

		public int Broadcast(ReadOnlyMemory<byte> payload, Delivery delivery, byte channel = 0)
			=> Send(new ConnectionId(1), payload, delivery, channel) ? 1 : 0;

		public int Broadcast(ReadOnlySpan<byte> payload, Delivery delivery, byte channel = 0)
			=> Send(new ConnectionId(1), payload, delivery, channel) ? 1 : 0;

		public void Dispose() => Stop();
	}
}