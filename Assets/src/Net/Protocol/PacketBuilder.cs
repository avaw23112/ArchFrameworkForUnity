using System;
using System.Runtime.CompilerServices;

namespace Arch.Net
{
    /// <summary>
    /// Helpers to construct frames with unified PacketHeader.
    /// </summary>
    public static class PacketBuilder
    {
        // 自定义回调委托：传入段索引与目标写入窗口
        public delegate void FillDelegate(int index, Span<byte> buffer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int VarIntSize(uint v)
        {
            int n = 0; do { n++; v >>= 7; } while (v != 0); return n;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void WriteVarUInt(byte[] buf, ref int p, uint v)
        {
            while (v >= 0x80)
            {
                buf[p++] = (byte)(v | 0x80);
                v >>= 7;
            }
            buf[p++] = (byte)v;
        }

        /// <summary>
        /// Build an RPC packet with minimal header based on settings.
        /// </summary>
        public static byte[] BuildRpc(byte[] payload)
        {
            var header = PacketHeader.Default(PacketType.Rpc, CodecType.MemoryPack);
            header.Flags = 0;
            if (Arch.Net.NetworkSettings.Config.RpcIncludeTimestamp)
            {
                header.Flags |= PacketFlags.HasTimestamp;
                header.Timestamp = (uint)Environment.TickCount;
            }
            if (Arch.Net.NetworkSettings.Config.RpcIncludeChannel)
            {
                header.Flags |= PacketFlags.HasChannel;
                header.Channel = 0; // reserved for future channel mapping
            }

            int headerLen = header.GetSize();
            int payloadLen = payload?.Length ?? 0;
            var buf = new byte[headerLen + payloadLen];
            header.WriteTo(buf);
            if (payloadLen > 0)
            {
                System.Buffer.BlockCopy(payload, 0, buf, headerLen, payloadLen);
            }
            return buf;
        }

        /// <summary>
        /// Build a Sync packet carrying locator (world/arch/chunk/entity range).
        /// </summary>
        public static byte[] BuildSync(uint worldId, uint archId, uint chunkId, ushort entityBase, ushort entityCount, byte[] payload)
        {
            var header = PacketHeader.Default(PacketType.Sync, CodecType.MemoryPack);
            header.Flags = PacketFlags.HasSyncLoc;
            header.WorldId = worldId;
            header.ArchetypeId = archId;
            header.ChunkId = chunkId;
            header.EntityBase = entityBase;
            header.EntityCount = entityCount;
            if (Arch.Net.NetworkSettings.Config.SyncIncludeTimestamp)
            {
                header.Flags |= PacketFlags.HasTimestamp;
                header.Timestamp = (uint)Environment.TickCount;
            }
            if (Arch.Net.NetworkSettings.Config.SyncIncludeChannel)
            {
                header.Flags |= PacketFlags.HasChannel;
                header.Channel = 0;
            }

            int headerLen = header.GetSize();
            int payloadLen = payload?.Length ?? 0;
            var buf = new byte[headerLen + payloadLen];
            header.WriteTo(buf);
            if (payloadLen > 0)
            {
                System.Buffer.BlockCopy(payload, 0, buf, headerLen, payloadLen);
            }
            return buf;
        }

        /// <summary>
        /// Build a Sync packet with segmented payload (multi-component blocks).
        /// Segment format: [segCount u8] then repeated [typeId varint][elemSize varint][flags u8][byteLen varint][bytes...]
        /// flags bit0=delta segment, bytes layout for delta: [maskLen varint][mask bytes][delta bytes]
        /// NOTE: Each segment carries its explicit byte length; allows passing pooled buffers whose Length may exceed used bytes.
        /// </summary>
        public static byte[] BuildSyncSegments(uint worldId, uint archId, uint chunkId, ushort entityBase, ushort entityCount, (int typeId, int elemSize, byte flags, byte[] data, int length)[] segments)
        {
            // compute payload size
            int payloadLen = 1; // segCount
            for (int i = 0; i < segments.Length; i++)
            {
                var seg = segments[i];
                int blen = Math.Max(0, seg.length);
                payloadLen += VarIntSize((uint)seg.typeId) + VarIntSize((uint)seg.elemSize) + 1 /*flags*/ + VarIntSize((uint)blen) + blen;
            }

            var header = PacketHeader.Default(PacketType.Sync, CodecType.Segments);
            header.Flags = PacketFlags.HasSyncLoc;
            header.WorldId = worldId;
            header.ArchetypeId = archId;
            header.ChunkId = chunkId;
            header.EntityBase = entityBase;
            header.EntityCount = entityCount;

            // build raw payload first
            var payload = new byte[payloadLen];
            int p = 0;
            payload[p++] = (byte)segments.Length;
            for (int i = 0; i < segments.Length; i++)
            {
                var seg = segments[i];
                int blen = Math.Max(0, seg.length);
                WriteVarUInt(payload, ref p, (uint)seg.typeId);
                WriteVarUInt(payload, ref p, (uint)seg.elemSize);
                payload[p++] = seg.flags;
                WriteVarUInt(payload, ref p, (uint)blen);
                if (blen > 0)
                {
                    System.Buffer.BlockCopy(seg.data, 0, payload, p, blen);
                    p += blen;
                }
            }

            // optional compression
            if (Arch.Net.NetworkSettings.Config.EnableCompression && payload.Length >= Arch.Net.NetworkSettings.Config.CompressThresholdBytes)
            {
                if (Compressor.TryCompress(payload, out var comp) && comp.Length < payload.Length)
                {
                    header.Flags |= PacketFlags.Compressed;
                    payload = comp;
                }
            }

            int headerLen = header.GetSize();
            var buf = new byte[headerLen + payload.Length];
            int o = 0;
            o += header.WriteTo(buf, o);
            System.Buffer.BlockCopy(payload, 0, buf, o, payload.Length);
            return buf;
        }

        /// <summary>
        /// Build a Sync packet (segments) with writer callback，避免中间段数组。
        /// Segment format 参考上一个重载；此处仅由调用方负责在 fill 中写入每段的 bytes。
        /// </summary>
        public static byte[] BuildSyncSegments(uint worldId, uint archId, uint chunkId, ushort entityBase, ushort entityCount, (int typeId, int elemSize, byte flags, int length)[] segments, FillDelegate fill)
        {
            // compute payload size
            int payloadLen = 1; // segCount
            for (int i = 0; i < segments.Length; i++)
            {
                var seg = segments[i];
                int blen = Math.Max(0, seg.length);
                payloadLen += VarIntSize((uint)seg.typeId) + VarIntSize((uint)seg.elemSize) + 1 /*flags*/ + VarIntSize((uint)blen) + blen;
            }

            var header = PacketHeader.Default(PacketType.Sync, CodecType.Segments);
            header.Flags = PacketFlags.HasSyncLoc;
            header.WorldId = worldId;
            header.ArchetypeId = archId;
            header.ChunkId = chunkId;
            header.EntityBase = entityBase;
            header.EntityCount = entityCount;

            var payload = new byte[payloadLen];
            int p = 0;
            payload[p++] = (byte)segments.Length;
            for (int i = 0; i < segments.Length; i++)
            {
                var seg = segments[i];
                int blen = Math.Max(0, seg.length);
                WriteVarUInt(payload, ref p, (uint)seg.typeId);
                WriteVarUInt(payload, ref p, (uint)seg.elemSize);
                payload[p++] = seg.flags;
                WriteVarUInt(payload, ref p, (uint)blen);
                if (blen > 0)
                {
                    fill?.Invoke(i, new Span<byte>(payload, p, blen));
                    p += blen;
                }
            }

            // optional compression
            if (Arch.Net.NetworkSettings.Config.EnableCompression && payload.Length >= Arch.Net.NetworkSettings.Config.CompressThresholdBytes)
            {
                if (Compressor.TryCompress(payload, out var comp) && comp.Length < payload.Length)
                {
                    header.Flags |= PacketFlags.Compressed;
                    payload = comp;
                }
            }

            int headerLen = header.GetSize();
            var buf = new byte[headerLen + payload.Length];
            int o = 0;
            o += header.WriteTo(buf, o);
            System.Buffer.BlockCopy(payload, 0, buf, o, payload.Length);
            return buf;
        }
    }
}
