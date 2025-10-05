using System;
using System.Buffers;

namespace Arch.Tools
{
	/// <summary>
	/// 全局字节内存池封装，避免频繁GC。
	/// </summary>
	public static class MemoryCache
	{
		private static readonly MemoryPool<byte> s_pool = MemoryPool<byte>.Shared;

		/// <summary>
		/// 从池中租用一个内存块。
		/// 调用方必须在用完后调用 Release(owner)。
		/// </summary>
		public static IMemoryOwner<byte> Rent(int size)
		{
			return s_pool.Rent(size);
		}

		/// <summary>
		/// 获取一个指定大小的 Memory 块，并返回 owner。
		/// </summary>
		public static Memory<byte> GetMemory(int size, out IMemoryOwner<byte> owner)
		{
			owner = s_pool.Rent(size);
			return owner.Memory.Slice(0, size);
		}

		/// <summary>
		/// 拷贝一份 byte[] 到池化内存。
		/// </summary>
		public static Memory<byte> CopyFrom(byte[] src, out IMemoryOwner<byte> owner)
		{
			owner = s_pool.Rent(src.Length);
			var memory = owner.Memory.Slice(0, src.Length);
			src.CopyTo(memory);
			return memory;
		}

		/// <summary>
		/// 将内存归还到池。
		/// </summary>
		public static void Release(IMemoryOwner<byte> owner)
		{
			owner?.Dispose();
		}
	}
}