using System;

namespace Arch.Net
{
	/// <summary>用户态节点 ID（不等同于底层 ConnectionId）。</summary>
	public readonly struct SessionId : IEquatable<SessionId>
	{
		public readonly int Value;

		public SessionId(int value) => Value = value;

		public static readonly SessionId Invalid = new(-1);

		public bool Equals(SessionId other) => Value == other.Value;

		public override bool Equals(object obj) => obj is SessionId o && Equals(o);

		public override int GetHashCode() => Value;

		public override string ToString() => $"Sess#{Value}";
	}
}