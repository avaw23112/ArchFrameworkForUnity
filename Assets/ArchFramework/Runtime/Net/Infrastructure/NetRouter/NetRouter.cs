using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Arch.Net
{
	public sealed class NetRouter : INetRouter
	{
		private readonly ConcurrentDictionary<int, Route> _best = new();
		private readonly ConcurrentDictionary<int, List<(Route route, float quality)>> _candidates = new();

		public bool TryResolveRoute(SessionId to, out Route route)
		{
			if (_best.TryGetValue(to.Value, out route) && route.IsValid) return true;

			if (_candidates.TryGetValue(to.Value, out var list) && list.Count > 0)
			{
				list.Sort((a, b) => b.quality.CompareTo(a.quality));
				foreach (var (r, q) in list)
				{
					if (r.IsValid) { _best[to.Value] = r; route = r; return true; }
				}
			}
			route = default;
			return false;
		}

		public void MarkLink(SessionId from, SessionId to, TransportId t, ConnectionId c, float quality = 1f)
		{
			var routes = _candidates.GetOrAdd(to.Value, _ => new List<(Route, float)>());
			var r = new Route(t, c);
			routes.RemoveAll(x => !x.route.IsValid || (x.route.Transport.Equals(t) && x.route.Conn.Equals(c)));
			routes.Add((r, quality));
			_best.TryAdd(to.Value, r);
		}

		public void RemoveLink(SessionId from, SessionId to, TransportId t, ConnectionId c)
		{
			if (_candidates.TryGetValue(to.Value, out var list))
				list.RemoveAll(x => x.route.Transport.Equals(t) && x.route.Conn.Equals(c));

			if (_best.TryGetValue(to.Value, out var best)
				&& best.Transport.Equals(t) && best.Conn.Equals(c))
			{
				_best.TryRemove(to.Value, out _);
			}
		}

		public void ReportDegraded(TransportId t, ConnectionId c, string why = null)
		{
			foreach (var kv in _best)
			{
				var r = kv.Value;
				if (r.Transport.Equals(t) && r.Conn.Equals(c))
					_best.TryRemove(kv.Key, out _);
			}
		}

		public void Reroute(SessionId to)
		{
			if (_candidates.TryGetValue(to.Value, out var list) && list.Count > 0)
			{
				list.Sort((a, b) => b.quality.CompareTo(a.quality));
				foreach (var (r, q) in list)
				{
					if (r.IsValid) { _best[to.Value] = r; return; }
				}
			}
			_best.TryRemove(to.Value, out _);
		}
	}
}