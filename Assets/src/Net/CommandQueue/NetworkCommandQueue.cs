using System;
using System.Collections.Concurrent;

namespace Arch.Net
{
    /// <summary>
    /// Main-thread queues for network callbacks.
    /// Provides no-capture packet queue to reduce GC.
    /// </summary>
    public static class NetworkCommandQueue
    {
        // m_ + p(object)
        private static readonly ConcurrentQueue<Action> m_pActionQueue = new ConcurrentQueue<Action>();
        private static readonly ConcurrentQueue<byte[]> m_pPacketQueue = new ConcurrentQueue<byte[]>();
        private static Action<byte[]> m_pPacketHandler;

        public static void Enqueue(Action action)
        {
            if (action == null) return;
            m_pActionQueue.Enqueue(action);
        }

        /// <summary>
        /// Enqueue a raw packet to be processed on main thread without capturing lambda allocations.
        /// </summary>
        public static void EnqueuePacket(byte[] data)
        {
            if (data == null) return;
            m_pPacketQueue.Enqueue(data);
        }

        /// <summary>
        /// Register a single handler to process packet queue items.
        /// </summary>
        public static void RegisterPacketHandler(Action<byte[]> handler)
        {
            m_pPacketHandler = handler;
        }

        /// <summary>
        /// Drain both action and packet queues with configurable limits.
        /// </summary>
        public static int Drain(int maxActions = -1, int maxPackets = -1)
        {
            int nActions = 0;
            int nPackets = 0;
            int vMaxActions = maxActions < 0 ? Arch.Net.NetworkSettings.Config.CommandsPerFrame : maxActions;
            int vMaxPackets = maxPackets < 0 ? Arch.Net.NetworkSettings.Config.PacketsPerFrame : maxPackets;

            while (nActions < vMaxActions && m_pActionQueue.TryDequeue(out var a))
            {
                try { a(); }
                catch { /* swallow to protect main loop */ }
                nActions++;
            }
            if (m_pPacketHandler != null)
            {
                while (nPackets < vMaxPackets && m_pPacketQueue.TryDequeue(out var p))
                {
                    try { m_pPacketHandler(p); }
                    catch { /* swallow */ }
                    nPackets++;
                }
            }
            return nActions + nPackets;
        }
    }
}

