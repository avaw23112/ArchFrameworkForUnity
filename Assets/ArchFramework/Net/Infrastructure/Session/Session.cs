// Arch.Net/Session.cs
using Arch.Tools;
using System;

namespace Arch.Net
{
	public sealed class Session : ISession
	{
		private readonly ISessionManager _manager;
		public SessionId Id { get; }
		public string Name { get; }
		public bool IsLocal { get; }
		public DateTime LastSeenUtc { get; private set; } = DateTime.UtcNow;

		public event Action<PooledBuffer> OnData;

		internal Session(ISessionManager manager, SessionId id, string name, bool isLocal)
		{
			_manager = manager ?? throw new ArgumentNullException(nameof(manager));
			Id = id; Name = name ?? id.ToString(); IsLocal = isLocal;
		}

		public void Touch() => LastSeenUtc = DateTime.UtcNow;

		public bool Send(in PooledBuffer buf, Delivery delivery, byte channel = 0)
			=> _manager.Send(Id, buf, delivery, channel);

		public bool Send(ReadOnlySpan<byte> payload, Delivery delivery, byte channel = 0)
		{
			var owner = MemoryCache.Rent(payload.Length);
			payload.CopyTo(owner.Memory.Span);
			using var pooled = new PooledBuffer(owner, owner.Memory[..payload.Length]);
			return Send(pooled, delivery, channel);
		}

		public bool Send(ReadOnlyMemory<byte> payload, Delivery delivery, byte channel = 0)
			=> Send(payload.Span, delivery, channel);

		internal void RaiseData(in PooledBuffer buf)
		{ Touch(); OnData?.Invoke(buf); }

		public void Dispose()
		{ }
	}
}