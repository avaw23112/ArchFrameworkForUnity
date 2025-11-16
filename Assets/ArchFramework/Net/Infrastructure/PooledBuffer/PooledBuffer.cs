// Arch.Net/PooledBuffer.cs
using System;
using System.Buffers;
using System.Runtime.InteropServices;

namespace Arch.Net
{
	/// <summary>来自内存池的字节缓冲；Dispose 归还。</summary>
	public readonly struct PooledBuffer : IDisposable
	{
		public readonly IMemoryOwner<byte> Owner;
		public readonly ReadOnlyMemory<byte> Memory;

		public PooledBuffer(IMemoryOwner<byte> owner, ReadOnlyMemory<byte> mem)
		{ Owner = owner; Memory = mem; }

		public ReadOnlySpan<byte> Span => Memory.Span;

		public bool TryGetArray(out ArraySegment<byte> seg)
			=> MemoryMarshal.TryGetArray(Memory, out seg); // 不保证一定能拿到数组

		public void Dispose() => Owner?.Dispose();
	}
}