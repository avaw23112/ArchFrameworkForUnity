// Arch.Net/ISessionManager.cs
using System;

namespace Arch.Net
{
	public interface ISessionManager : IDisposable
	{
		event Action<ISession> OnSessionAdded;

		event Action<ISession> OnSessionRemoved;

		ISession GetOrCreate(SessionId id, string name = null, bool isLocal = false);

		bool Remove(SessionId id);

		bool Send(SessionId target, in PooledBuffer buf, Delivery delivery, byte channel = 0);

		// 传输层注册/注销：返回 TransportId，外部只保存这个索引
		TransportId AttachTransport(ITransport transport);

		void DetachTransport(TransportId transportId);

		// 握手完成后，绑定会话到某条底层连接（用 TransportId）
		void Bind(SessionId id, TransportId transport, ConnectionId connId, float quality = 1f);

		void Unbind(SessionId id, TransportId transport, ConnectionId connId);

		// 连接到会话的解析（首包握手解析出 SessionId 后调用）
		void MapConnectionToSession(TransportId transport, ConnectionId conn, SessionId sid);
	}
}