using System;

namespace Arch.Net
{
	/// <summary>消息传输的可靠性/有序性语义，映射到具体库（LiteNetLib 等）。</summary>
	public enum Delivery
	{
		Unreliable,            // 不保证送达，不保证顺序
		UnreliableSequenced,   // 不保证送达，但保证同频道序更近者覆盖旧包
		ReliableUnordered,     // 保证送达，不保证顺序
		ReliableOrdered        // 保证送达，保证顺序
	}

	public enum DisconnectReason
	{
		None,
		Timeout,
		Remote,
		LocalShutdown,
		ConnectionRejected,
		Error
	}

	/// <summary>连接句柄，屏蔽底层 peer 对象。</summary>
	public readonly struct ConnectionId : IEquatable<ConnectionId>
	{
		public readonly int Value;

		public ConnectionId(int v) => Value = v;

		public bool Equals(ConnectionId other) => Value == other.Value;

		public override bool Equals(object obj) => obj is ConnectionId o && Equals(o);

		public override int GetHashCode() => Value;

		public override string ToString() => $"Con#{Value}";

		public static readonly ConnectionId Invalid = new ConnectionId(-1);
	}

	/// <summary>启动/连接配置。保持纯 BCL，不含 Unity 类型。</summary>
	public sealed class TransportConfig
	{
		public string Address = "127.0.0.1";
		public int Port = 7777;
		public int MaxConnections = 64;
		public string ConnectionKey = "";     // LiteNetLib 的连接秘钥
		public bool IPv6 = false;
		public int UpdateIntervalMs = 15;     // 推荐 10~20ms
	}
}