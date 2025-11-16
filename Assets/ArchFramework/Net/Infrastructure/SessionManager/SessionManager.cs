// Arch.Net/SessionManager.cs
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Arch.Net
{
	public sealed class SessionManager : ISessionManager
	{
		private readonly INetRouter _router;

		// Sessions
		private readonly ConcurrentDictionary<int, Session> _sessions = new();

		// 目标 SessionId -> 与之相关的 (TransportId, ConnectionId) 集合
		private readonly ConcurrentDictionary<int, HashSet<(TransportId t, ConnectionId c)>> _links
			= new(new IntComparer());

		// Transport 注册表（仅此类持有实例）
		private readonly Dictionary<int, ITransport> _transports = new();

		private int _nextTransportId = 0;

		// (TransportId, ConnectionId) -> SessionId 映射（由握手填充）
		private readonly ConcurrentDictionary<(int tid, int cid), SessionId> _connToSession = new();

		public event Action<ISession> OnSessionAdded;

		public event Action<ISession> OnSessionRemoved;

		public SessionManager(INetRouter router)
		{
			_router = router ?? throw new ArgumentNullException(nameof(router));
		}

		public ISession GetOrCreate(SessionId id, string name = null, bool isLocal = false)
		{
			return _sessions.GetOrAdd(id.Value, _ =>
			{
				var s = new Session(this, id, name, isLocal);
				OnSessionAdded?.Invoke(s);
				return s;
			});
		}

		public bool Remove(SessionId id)
		{
			if (_sessions.TryRemove(id.Value, out var s))
			{
				_links.TryRemove(id.Value, out _);
				OnSessionRemoved?.Invoke(s);
				s.Dispose();
				return true;
			}
			return false;
		}

		// ---------- Transport 注册 / 事件订阅 ----------

		public TransportId AttachTransport(ITransport transport)
		{
			if (transport == null) throw new ArgumentNullException(nameof(transport));
			var tid = new TransportId(_nextTransportId++);
			_transports.Add(tid.Value, transport);

			// 统一事件入口（只在此类）
			transport.OnConnected += conn => OnConnected(tid, conn);
			transport.OnDisconnected += (conn, reason, msg) => OnDisconnected(tid, conn, reason, msg);
			transport.OnReciveData += (conn, buf) => OnData(tid, conn, buf);
			return tid;
		}

		public void DetachTransport(TransportId transportId)
		{
			if (_transports.Remove(transportId.Value, out var t))
			{
				t.OnConnected -= conn => OnConnected(transportId, conn);
				t.OnDisconnected -= (conn, reason, msg) => OnDisconnected(transportId, conn, reason, msg);
				t.OnReciveData -= (conn, buf) => OnData(transportId, conn, buf);
				t.Stop();
				t.Dispose();
			}
		}

		// ---------- 绑定/解绑 会话与底层连接 ----------

		public void Bind(SessionId id, TransportId transport, ConnectionId connId, float quality = 1f)
		{
			var sess = (Session)GetOrCreate(id);
			var set = _links.GetOrAdd(id.Value, _ => new HashSet<(TransportId, ConnectionId)>(new LinkComparer()));
			set.Add((transport, connId));
			_router.MarkLink(SessionId.Invalid, id, transport, connId, quality);
		}

		public void Unbind(SessionId id, TransportId transport, ConnectionId connId)
		{
			if (_links.TryGetValue(id.Value, out var set))
			{
				set.Remove((transport, connId));
				_router.RemoveLink(SessionId.Invalid, id, transport, connId);
			}
		}

		public void MapConnectionToSession(TransportId transport, ConnectionId conn, SessionId sid)
			=> _connToSession[(transport.Value, conn.Value)] = sid;

		// ---------- 发送（Session -> Router -> Transport） ----------

		public bool Send(SessionId target, in PooledBuffer buf, Delivery delivery, byte channel = 0)
		{
			if (!_router.TryResolveRoute(target, out var route) || !route.IsValid)
				return false;

			if (!_transports.TryGetValue(route.Transport.Value, out var t))
				return false;

			return t.Send(route.Conn, buf, delivery, channel);
		}

		// ---------- 传输层事件汇聚（仅此类订阅） ----------

		private void OnConnected(TransportId tid, ConnectionId conn)
		{
			// 等待握手，把 (tid,conn) 归属到某个 SessionId：
			// -> MapConnectionToSession(tid, conn, sid); Bind(sid, tid, conn)
		}

		private void OnDisconnected(TransportId tid, ConnectionId conn, DisconnectReason reason, string msg)
		{
			// 清理所有涉及该 (tid,conn) 的绑定，并通知路由退化
			foreach (var kv in _links)
			{
				var set = kv.Value;
				(TransportId t, ConnectionId c)? hit = null;
				foreach (var link in set)
				{
					if (link.t.Equals(tid) && link.c.Equals(conn)) { hit = link; break; }
				}
				if (hit.HasValue)
				{
					set.Remove(hit.Value);
					_router.ReportDegraded(tid, conn, "disconnect");
				}
			}
		}

		private void OnData(TransportId tid, ConnectionId conn, PooledBuffer buf)
		{
			if (TryResolveSessionByConnection(tid, conn, out var sid)
				&& _sessions.TryGetValue(sid.Value, out var sess))
			{
				sess.RaiseData(in buf);
				return;
			}

			// 未映射到 Session：交给你的握手/发现层；此处避免泄露
			buf.Dispose();
		}

		private bool TryResolveSessionByConnection(TransportId tid, ConnectionId conn, out SessionId sid)
		{
			if (_connToSession.TryGetValue((tid.Value, conn.Value), out var s)) { sid = s; return true; }
			sid = SessionId.Invalid; return false;
		}

		public void Dispose()
		{
			foreach (var kv in _transports) { kv.Value.Stop(); kv.Value.Dispose(); }
			_transports.Clear();
			_sessions.Clear();
			_links.Clear();
			_connToSession.Clear();
		}

		// ---------- 比较器 ----------
		private sealed class IntComparer : IEqualityComparer<int>

		{ public bool Equals(int x, int y) => x == y; public int GetHashCode(int obj) => obj; }

		private sealed class LinkComparer : IEqualityComparer<(TransportId, ConnectionId)>
		{
			public bool Equals((TransportId, ConnectionId) x, (TransportId, ConnectionId) y)
				=> x.Item1.Equals(y.Item1) && x.Item2.Equals(y.Item2);

			public int GetHashCode((TransportId, ConnectionId) obj)
				=> HashCode.Combine(obj.Item1.GetHashCode(), obj.Item2.GetHashCode());
		}
	}
}