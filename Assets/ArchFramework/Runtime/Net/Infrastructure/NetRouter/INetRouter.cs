using System;
using UnityEditor.MemoryProfiler;

namespace Arch.Net
{
	/// <summary>一次出站发送所选择的“当前最佳直达路径”。</summary>
	public readonly struct Route
	{
		public readonly TransportId Transport;
		public readonly ConnectionId Conn;

		public Route(TransportId t, ConnectionId c)
		{ Transport = t; Conn = c; }

		public bool IsValid => Transport.Value >= 0 && Conn.Value >= 0;

		public override string ToString() => IsValid ? $"{Transport}:{Conn.Value}" : "<invalid>";
	}

	public interface INetRouter
	{
		bool TryResolveRoute(SessionId to, out Route route);

		void MarkLink(SessionId from, SessionId to, TransportId t, ConnectionId c, float quality = 1f);

		void RemoveLink(SessionId from, SessionId to, TransportId t, ConnectionId c);

		void ReportDegraded(TransportId t, ConnectionId c, string why = null);

		void Reroute(SessionId to);
	}
}