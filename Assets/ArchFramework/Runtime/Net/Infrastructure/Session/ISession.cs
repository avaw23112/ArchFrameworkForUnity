// Arch.Net/ISession.cs
using System;

namespace Arch.Net
{
	public interface ISession : IDisposable
	{
		SessionId Id { get; }
		string Name { get; }
		bool IsLocal { get; }
		DateTime LastSeenUtc { get; }

		event Action<PooledBuffer> OnData;

		bool Send(in PooledBuffer buf, Delivery delivery, byte channel = 0);

		bool Send(ReadOnlySpan<byte> payload, Delivery delivery, byte channel = 0);

		bool Send(ReadOnlyMemory<byte> payload, Delivery delivery, byte channel = 0);

		void Touch();
	}
}