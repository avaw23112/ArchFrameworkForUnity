using System;
using Arch.Tools;

namespace Arch.Net
{
    public sealed class Session : ISession
    {
        public string SessionId { get; }
        public ITransport Transport { get; private set; }

        public event Action OnConnect;
        public event Action<string> OnDisconnect;
        public event Action OnReconnect;
        public event Action<string> OnNetworkUnstable;
        public event Action OnUpdate;

        // m_ + p(object) / v(value)
        private volatile bool m_vWasConnected;
        private System.Collections.Generic.Dictionary<byte, Action<byte[], int, int>> m_pRpcHandlers = new System.Collections.Generic.Dictionary<byte, Action<byte[], int, int>>();

        public Session(string sessionId)
        {
            SessionId = sessionId;
        }

        public void AttachTransport(ITransport transport)
        {
            if (Transport == transport) return;
            if (Transport != null)
            {
                Unsubscribe(Transport);
                Transport.Dispose();
            }
            Transport = transport;
            Subscribe(Transport);
        }

        public void Connect(string endpoint)
        {
            if (Transport == null) throw new InvalidOperationException("Transport not attached");
            Transport.Configure(endpoint);
            Transport.Connect();
        }

        public void Disconnect(string reason = null)
        {
            Transport?.Disconnect(reason);
        }

        public bool Send(byte[] data, int length)
        {
            if (Transport == null) return false;
            return Transport.Send(data, length);
        }

        public void Update()
        {
            Transport?.Poll();
            OnUpdate?.Invoke();
        }

        /// <summary>
        /// Register RPC handler by message id. Payload slice excludes PacketHeader, includes RpcId as first byte.
        /// </summary>
        public void RegisterRpc(byte id, Action<byte[], int, int> handler)
        {
            if (handler == null) return;
            m_pRpcHandlers[id] = handler;
        }

        private void Subscribe(ITransport t)
        {
            t.Connected += HandleConnected;
            t.Disconnected += HandleDisconnected;
            t.NetworkUnstable += HandleUnstable;
            t.DataReceived += HandleData;
        }

        private void Unsubscribe(ITransport t)
        {
            t.Connected -= HandleConnected;
            t.Disconnected -= HandleDisconnected;
            t.NetworkUnstable -= HandleUnstable;
            t.DataReceived -= HandleData;
        }

        private void HandleConnected()
        {
            if (m_vWasConnected)
            {
                OnReconnect?.Invoke();
            }
            else
            {
                OnConnect?.Invoke();
                m_vWasConnected = true;
            }
        }

        private void HandleDisconnected(string reason)
        {
            OnDisconnect?.Invoke(reason);
        }

        private void HandleUnstable(string hint)
        {
            OnNetworkUnstable?.Invoke(hint);
        }

        private void HandleData(byte[] packet)
        {
            // Avoid capture allocations by queuing raw packet only
            NetworkCommandQueue.EnqueuePacket(packet);
        }

        /// <summary>
        /// Process a raw packet on main thread; parse header and route.
        /// </summary>
        public void HandlePacket(byte[] packet)
        {
            if (packet == null)
            {
                ArchLog.LogWarning("Received null packet");
                return;
            }
            if (packet.Length < 4)
            {
                ArchLog.LogWarning($"Packet too small: {packet.Length}");
                return;
            }
            int nHeader;
            PacketHeader header;
            try { header = PacketHeader.ReadFrom(packet, out nHeader); }
            catch (Exception ex)
            {
                ArchLog.LogWarning($"Header parse failed: {ex.Message}");
                return;
            }

            int vPayloadLen = Math.Max(0, packet.Length - nHeader);
            Arch.Net.NetworkStats.RecordRecv(packet.Length);
            // If payload is compressed at packet level, try decompress
            if ((header.Flags & PacketFlags.Compressed) != 0 && vPayloadLen > 0)
            {
                var span = new ReadOnlySpan<byte>(packet, nHeader, vPayloadLen);
                if (Compressor.TryDecompress(span, out var decomp))
                {
                    // Replace packet payload with decompressed buffer, keep original header bytes
                    var orig = packet;
                    var newPacket = new byte[nHeader + decomp.Length];
                    System.Buffer.BlockCopy(orig, 0, newPacket, 0, nHeader);
                    System.Buffer.BlockCopy(decomp, 0, newPacket, nHeader, decomp.Length);
                    packet = newPacket;
                    vPayloadLen = decomp.Length;
                }
            }
            bool hasLoc = (header.Flags & PacketFlags.HasSyncLoc) != 0;
            bool hasRel = (header.Flags & PacketFlags.HasReliability) != 0;
            bool hasTs = (header.Flags & PacketFlags.HasTimestamp) != 0;
            ArchLog.LogDebug(
                $"Pkt v{header.Version} type={header.Type} codec={header.Codec} flags={header.Flags} " +
                (hasRel ? $"seq={header.Seq} " : "") +
                (hasTs ? $"ts={header.Timestamp} " : "") +
                $"len={vPayloadLen} " +
                (hasLoc ? $"world={header.WorldId} arch={header.ArchetypeId} chunk={header.ChunkId}" : ""));

            switch (header.Type)
            {
                case PacketType.Rpc:
                    if (vPayloadLen <= 0) return;
                    byte id = packet[nHeader];
                    if (m_pRpcHandlers.TryGetValue(id, out var h))
                    {
                        h(packet, nHeader, vPayloadLen);
                    }
                    break;
                case PacketType.Sync:
                    SyncIncomingQueue.Enqueue(in header, packet, nHeader, vPayloadLen);
                    break;
            }
        }

        public void Dispose()
        {
            try { Transport?.Dispose(); }
            catch { /* ignore */ }
        }
    }
}
