using System;

namespace Arch.Net
{
    /// <summary>
    /// Placeholder LZ4 compressor. Currently forwards to DeflateCompressor as a stub.
    /// Integrate a real LZ4 implementation later and swap via Compressor.Set(new LZ4Compressor()).
    /// </summary>
    public sealed class LZ4Compressor : ICompressor
    {
        private readonly ICompressor m_fallback = new DeflateCompressor();

        public bool TryCompress(ReadOnlySpan<byte> src, out byte[] dst)
        {
            // TODO: replace with real LZ4 framing/codec
            return m_fallback.TryCompress(src, out dst);
        }

        public bool TryDecompress(ReadOnlySpan<byte> src, out byte[] dst)
        {
            // TODO: replace with real LZ4 framing/codec
            return m_fallback.TryDecompress(src, out dst);
        }
    }
}

