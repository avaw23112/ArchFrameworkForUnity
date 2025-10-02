# Network Roadmap (Skeleton)

This document maps the staged plan to concrete, verifiable checkpoints.

## Week 0 – Baseline
- Bring up Session + Transport (mock) + header + command queue.
- Unity demo: NetworkDriver sends echo packet; logs header parsed.

## Week 1 – Session events
- OnConnect/OnDisconnect/OnReconnect/OnNetworkUnstable surfaced.
- Demo: trigger disconnect/reconnect with mock transport hooks.

## Week 2 – Protocol layer
- PacketHeader stable; helpers to build RPC/Sync frames.
- Demo: log header fields; unit smoke via mock transport.

## Week 3 – Thread bridge
- Network thread → main thread command queue stable.
- Demo: stress enqueue/drain at 60 FPS (no GC spikes).

Later weeks: LiteNetLib, world handshake, full/sparse sync, RPC routing.

