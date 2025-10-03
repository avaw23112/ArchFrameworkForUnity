using System;

namespace Arch.Net
{
    /// <summary>
    /// Logical packet categories for routing.
    /// </summary>
    public enum PacketType : byte
    {
        Rpc = 1,
        Sync = 2,
        Control = 3,
    }

    /// <summary>
    /// Payload codec used.
    /// </summary>
    public enum CodecType : byte
    {
        MemoryPack = 1,
        Thrift = 2,
        Segments = 250, // internal segmented payload (multi-component blocks)
    }

    [Flags]
    public enum PacketFlags : byte
    {
        None         = 0,
        Compressed   = 1 << 0,
        Encrypted    = 1 << 1,
        Fragment     = 1 << 2,

        // Optional header blocks (presence bits)
        HasChannel    = 1 << 3,
        HasSyncLoc    = 1 << 4, // World/Archetype/Chunk/EntityBase/EntityCount
        HasReliability= 1 << 5, // Seq/Ack/AckBits
        HasTimestamp  = 1 << 6, // Timestamp
        HasPayloadLen = 1 << 7, // PayloadLength present
    }

    /// <summary>
    /// Fixed-size 44-byte header for all network frames (little-endian).
    /// </summary>
    public struct PacketHeader
    {
        public byte Version;
        public PacketType Type;
        public CodecType Codec;
        public PacketFlags Flags;

        public ushort Channel;
        public ushort Reserved;

        public uint WorldId;
        public uint ArchetypeId;
        public uint ChunkId;

        public ushort EntityBase;
        public ushort EntityCount;

        public uint Seq;
        public uint Ack;
        public uint AckBits;
        public uint Timestamp;

        public uint PayloadLength;

        public static PacketHeader Default(PacketType type, CodecType codec)
        {
            return new PacketHeader
            {
                Version = 1,
                Type = type,
                Codec = codec,
                Flags = PacketFlags.None,
                Channel = 0,
                Reserved = 0,
                WorldId = 0,
                ArchetypeId = 0,
                ChunkId = 0,
                EntityBase = 0,
                EntityCount = 0,
                Seq = 0,
                Ack = 0,
                AckBits = 0,
                Timestamp = 0,
                PayloadLength = 0
            };
        }

        /// <summary>
        /// Compute variable header size based on presence flags.
        /// </summary>
        public int GetSize()
        {
            int n = 4; // Version(1) + Type(1) + Codec(1) + Flags(1)
            if ((Flags & PacketFlags.HasChannel) != 0) n += 2;
            if ((Flags & PacketFlags.HasSyncLoc) != 0) n += (4 + 4 + 4 + 2 + 2);
            if ((Flags & PacketFlags.HasReliability) != 0) n += (4 + 4 + 4);
            if ((Flags & PacketFlags.HasTimestamp) != 0) n += 4;
            if ((Flags & PacketFlags.HasPayloadLen) != 0) n += 4;
            return n;
        }

        /// <summary>
        /// Write header to buffer at offset; returns header size written.
        /// </summary>
        public int WriteTo(byte[] buffer, int offset = 0)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            int need = GetSize();
            if (buffer.Length - offset < need) throw new ArgumentException("Buffer too small for header");

            int p = offset;
            buffer[p++] = Version;
            buffer[p++] = (byte)Type;
            buffer[p++] = (byte)Codec;
            buffer[p++] = (byte)Flags;

            if ((Flags & PacketFlags.HasChannel) != 0)
            {
                WriteUInt16(buffer, p, Channel); p += 2;
            }
            if ((Flags & PacketFlags.HasSyncLoc) != 0)
            {
                WriteUInt32(buffer, p, WorldId); p += 4;
                WriteUInt32(buffer, p, ArchetypeId); p += 4;
                WriteUInt32(buffer, p, ChunkId); p += 4;
                WriteUInt16(buffer, p, EntityBase); p += 2;
                WriteUInt16(buffer, p, EntityCount); p += 2;
            }
            if ((Flags & PacketFlags.HasReliability) != 0)
            {
                WriteUInt32(buffer, p, Seq); p += 4;
                WriteUInt32(buffer, p, Ack); p += 4;
                WriteUInt32(buffer, p, AckBits); p += 4;
            }
            if ((Flags & PacketFlags.HasTimestamp) != 0)
            {
                WriteUInt32(buffer, p, Timestamp); p += 4;
            }
            if ((Flags & PacketFlags.HasPayloadLen) != 0)
            {
                WriteUInt32(buffer, p, PayloadLength); p += 4;
            }
            return p - offset;
        }

        /// <summary>
        /// Read variable header from buffer; outputs header length consumed.
        /// </summary>
        public static PacketHeader ReadFrom(byte[] buffer, out int headerLength, int offset = 0)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (buffer.Length - offset < 4) throw new ArgumentException("Buffer too small for header base");

            PacketHeader h = default;
            int p = offset;
            h.Version = buffer[p++];
            h.Type = (PacketType)buffer[p++];
            h.Codec = (CodecType)buffer[p++];
            h.Flags = (PacketFlags)buffer[p++];

            h.Channel = 0; h.Reserved = 0;
            h.WorldId = 0; h.ArchetypeId = 0; h.ChunkId = 0; h.EntityBase = 0; h.EntityCount = 0;
            h.Seq = 0; h.Ack = 0; h.AckBits = 0; h.Timestamp = 0; h.PayloadLength = 0;

            if ((h.Flags & PacketFlags.HasChannel) != 0)
            {
                if (buffer.Length - p < 2) throw new ArgumentException("Buffer too small for Channel");
                h.Channel = ReadUInt16(buffer, p); p += 2;
            }
            if ((h.Flags & PacketFlags.HasSyncLoc) != 0)
            {
                if (buffer.Length - p < 16) throw new ArgumentException("Buffer too small for SyncLocator");
                h.WorldId = ReadUInt32(buffer, p); p += 4;
                h.ArchetypeId = ReadUInt32(buffer, p); p += 4;
                h.ChunkId = ReadUInt32(buffer, p); p += 4;
                h.EntityBase = ReadUInt16(buffer, p); p += 2;
                h.EntityCount = ReadUInt16(buffer, p); p += 2;
            }
            if ((h.Flags & PacketFlags.HasReliability) != 0)
            {
                if (buffer.Length - p < 12) throw new ArgumentException("Buffer too small for Reliability");
                h.Seq = ReadUInt32(buffer, p); p += 4;
                h.Ack = ReadUInt32(buffer, p); p += 4;
                h.AckBits = ReadUInt32(buffer, p); p += 4;
            }
            if ((h.Flags & PacketFlags.HasTimestamp) != 0)
            {
                if (buffer.Length - p < 4) throw new ArgumentException("Buffer too small for Timestamp");
                h.Timestamp = ReadUInt32(buffer, p); p += 4;
            }
            if ((h.Flags & PacketFlags.HasPayloadLen) != 0)
            {
                if (buffer.Length - p < 4) throw new ArgumentException("Buffer too small for PayloadLength");
                h.PayloadLength = ReadUInt32(buffer, p); p += 4;
            }

            headerLength = p - offset;
            return h;
        }

        private static void WriteUInt16(byte[] buffer, int offset, ushort value)
        {
            buffer[offset + 0] = (byte)(value & 0xFF);
            buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
        }

        private static void WriteUInt32(byte[] buffer, int offset, uint value)
        {
            buffer[offset + 0] = (byte)(value & 0xFF);
            buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
            buffer[offset + 2] = (byte)((value >> 16) & 0xFF);
            buffer[offset + 3] = (byte)((value >> 24) & 0xFF);
        }

        private static ushort ReadUInt16(byte[] buffer, int offset)
        {
            return (ushort)(buffer[offset + 0] | (buffer[offset + 1] << 8));
        }

        private static uint ReadUInt32(byte[] buffer, int offset)
        {
            return (uint)(buffer[offset + 0]
                         | (buffer[offset + 1] << 8)
                         | (buffer[offset + 2] << 16)
                         | (buffer[offset + 3] << 24));
        }
    }
}
