namespace Arch.Net
{
    /// <summary>
    /// Built-in RPC message identifiers carried as first byte of RPC payload.
    /// </summary>
    public enum RpcIds : byte
    {
        None = 0,
        TypeManifest = 1,
        ArchetypeManifest = 2,
        ArchetypeManifestV2 = 3,
        StructCreate = 10,
        StructDestroy = 11,
        StructAdd = 12,
        StructRemove = 13,
        StructCommandGroup = 14,
        TopologyAdvert = 30,
        TopologyMetrics = 31,
        RoutingForceConnect = 32,
        PeerForward = 33,
        SyncRelay = 34,
    }
}
