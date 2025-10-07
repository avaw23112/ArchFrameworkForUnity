using System;

namespace Arch.Net
{
	/// <summary>传输层实例的索引句柄，仅 SessionManager 持有真实实例。</summary>
	public readonly struct TransportId : IEquatable<TransportId>
	{
		public readonly int Value;

		public TransportId(int v) => Value = v;

		public static readonly TransportId Invalid = new(-1);

		public bool Equals(TransportId other) => Value == other.Value;

		public override bool Equals(object obj) => obj is TransportId o && Equals(o);

		public override int GetHashCode() => Value;

		public override string ToString() => $"T#{Value}";
	}
}