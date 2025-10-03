using System;
using System.Collections.Concurrent;

namespace Arch.Net
{
    /// <summary>
    /// 同步包接收队列
    /// - 存放：解析后的 PacketHeader 与其在原始数据中的切片（Buffer/Offset/Length）。
    /// - 消费：由 SyncApplySystem 每帧按限流出队并应用。
    /// </summary>
    public static class SyncIncomingQueue
    {
        public struct SyncPacket
        {
            public PacketHeader Header;
            public byte[] Buffer;
            public int Offset;
            public int Length;
        }

        private static readonly ConcurrentQueue<SyncPacket> s_pQueue = new ConcurrentQueue<SyncPacket>();

        public static void Enqueue(in PacketHeader header, byte[] buffer, int offset, int length)
        {
            s_pQueue.Enqueue(new SyncPacket { Header = header, Buffer = buffer, Offset = offset, Length = length });
        }

        public static bool TryDequeue(out SyncPacket packet)
        {
            return s_pQueue.TryDequeue(out packet);
        }
    }
}
