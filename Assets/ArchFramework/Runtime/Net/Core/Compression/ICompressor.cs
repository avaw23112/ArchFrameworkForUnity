using System;
using System.IO;
using System.IO.Compression;

namespace Arch.Net
{
    public interface ICompressor
    {
        bool TryCompress(ReadOnlySpan<byte> src, out byte[] dst);
        bool TryDecompress(ReadOnlySpan<byte> src, out byte[] dst);
    }

    /// <summary>
    /// Placeholder compressor (Deflate) until LZ4 is plugged. Swap at init via Compressor.Set.
    /// </summary>
    public sealed class DeflateCompressor : ICompressor
    {
        public bool TryCompress(ReadOnlySpan<byte> src, out byte[] dst)
        {
            try
            {
                using var ms = new MemoryStream();
                using (var ds = new DeflateStream(ms, CompressionLevel.Fastest, leaveOpen: true))
                {
                    ds.Write(src);
                }
                dst = ms.ToArray();
                return dst.Length > 0 && dst.Length < src.Length;
            }
            catch { dst = null; return false; }
        }

        public bool TryDecompress(ReadOnlySpan<byte> src, out byte[] dst)
        {
            try
            {
                using var input = new MemoryStream(src.ToArray());
                using var ds = new DeflateStream(input, CompressionMode.Decompress);
                using var output = new MemoryStream();
                ds.CopyTo(output);
                dst = output.ToArray();
                return dst.Length > 0;
            }
            catch { dst = null; return false; }
        }
    }

    public static class Compressor
    {
        private static ICompressor s_impl = new DeflateCompressor();
        public static void Set(ICompressor impl) { if (impl != null) s_impl = impl; }
        public static bool TryCompress(ReadOnlySpan<byte> src, out byte[] dst) => s_impl.TryCompress(src, out dst);
        public static bool TryDecompress(ReadOnlySpan<byte> src, out byte[] dst) => s_impl.TryDecompress(src, out dst);
    }
}

