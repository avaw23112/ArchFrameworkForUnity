using System;

namespace Arch.Net
{
	public interface ITransport : IDisposable
	{
		event Action<ConnectionId> OnConnected;

		event Action<ConnectionId, DisconnectReason, string> OnDisconnected;

		event Action<ConnectionId, PooledBuffer> OnReciveData;

		bool IsServer { get; }
		bool IsClient { get; }

		void StartServer(TransportConfig config);

		void StartClient(TransportConfig config);

		void Stop();

		void Poll();

		// 返回：是否通过前置校验并尝试入队发送
		bool Send(ConnectionId conn, ArraySegment<byte> payload, Delivery delivery, byte channel = 0);

		bool Send(ConnectionId conn, ReadOnlyMemory<byte> payload, Delivery delivery, byte channel = 0);

		bool Send(ConnectionId conn, ReadOnlySpan<byte> payload, Delivery delivery, byte channel = 0);

		bool Send(ConnectionId conn, in PooledBuffer buf, Delivery delivery, byte channel = 0)
			=> Send(conn, buf.Memory, delivery, channel);

		// 广播：返回已尝试发送的连接数
		int Broadcast(ArraySegment<byte> payload, Delivery delivery, byte channel = 0);

		int Broadcast(ReadOnlyMemory<byte> payload, Delivery delivery, byte channel = 0);

		int Broadcast(ReadOnlySpan<byte> payload, Delivery delivery, byte channel = 0);

		int Broadcast(in PooledBuffer buf, Delivery delivery, byte channel = 0)
			=> Broadcast(buf.Memory, delivery, channel);
	}
}