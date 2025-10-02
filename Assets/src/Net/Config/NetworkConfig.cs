using UnityEngine;
using LiteNetLib;

namespace Arch.Net
{
    /// <summary>
    /// Scriptable configuration for networking behavior.
    /// Place an asset at Resources/NetworkConfig to override defaults.
    /// </summary>
    public sealed class NetworkConfig : ScriptableObject
    {
        public enum RouteSelectorType
        {
            SimpleLatency = 0,
            FFIM = 1,
        }
        [Header("Driver")]
        [SerializeField] private string m_szDefaultEndpoint = "loopback://local";

        [Header("Queue")]
        [SerializeField] private int m_nCommandsPerFrame = 256;
        [SerializeField] private int m_nPacketsPerFrame = 512;
        [SerializeField] private int m_nEntitiesPerPacket = 64;

        [Header("Packet Flags")]
        [SerializeField] private bool m_vRpcIncludeTimestamp = true;
        [SerializeField] private bool m_vSyncIncludeTimestamp = true;
        [SerializeField] private bool m_vRpcIncludeChannel = false;
        [SerializeField] private bool m_vSyncIncludeChannel = false;

        [Header("Delivery Methods (LiteNetLib)")]
        [SerializeField] private DeliveryMethod m_vRpcDelivery = DeliveryMethod.ReliableOrdered;
        [SerializeField] private DeliveryMethod m_vSyncDelivery = DeliveryMethod.Unreliable;

        [Header("Scan Mode")]
        [SerializeField] private bool m_vUseChunkScan = true;

        [Header("Compression")]
        [SerializeField] private bool m_vEnableCompression = true;
        [SerializeField] private int m_nCompressThresholdBytes = 512;

        [Header("Topology / Routing")]
        [SerializeField] private int m_nTopologyUpdateIntervalFrames = 60;          // ~1s @60fps
        [SerializeField] private int m_nTopologyMinMigrationIntervalFrames = 300;   // ~5s @60fps
        [SerializeField] private int m_nTopologyImprovementThreshold = 50;          // score units
        [SerializeField] private int m_nTopologyRequiredConsecutiveBetter = 2;      // consecutive confirmations
        [SerializeField] private RouteSelectorType m_vRouteSelector = RouteSelectorType.FFIM;
        [SerializeField] private bool m_vTopologyEnabled = true;
        [SerializeField] private int m_nFfimRttWeight = 1;
        [SerializeField] private int m_nFfimJitterWeight = 20;
        [SerializeField] private int m_nFfimLossWeight = 2000;
        [SerializeField] private int m_nFfimEndpointWeightBonus = 5;
        [SerializeField] private int m_nStaleIdCullFrames = 5;
        [SerializeField] private int m_nStructCommandFlushIntervalFrames = 1;
        [SerializeField] private bool m_vEnableInterpolationFallback = true;
        [Header("Sync Relay")]
        [SerializeField] private bool m_vEnableSyncRelay = true;
        [SerializeField] private int m_nSyncRelayTtl = 16;
        [SerializeField] private int m_nSyncRelayDedupWindowFrames = 120;
        [SerializeField] private bool m_vEnableSyncRelayLog = false;
        [SerializeField] private int m_nSyncRelayDedupCapacity = 8192;
        [SerializeField] private int m_nSyncRelayLogSampleRate = 100;

        [Header("Sync Apply Logging")]
        [SerializeField] private bool m_vEnableSyncApplyLog = false;
        [SerializeField] private int m_nSyncApplyLogSampleRate = 100;

        public string DefaultEndpoint => m_szDefaultEndpoint;
        public int CommandsPerFrame => m_nCommandsPerFrame;
        public int PacketsPerFrame => m_nPacketsPerFrame;
        public int EntitiesPerPacket => m_nEntitiesPerPacket;
        public bool UseChunkScan => m_vUseChunkScan;
        public bool EnableCompression => m_vEnableCompression;
        public int CompressThresholdBytes => m_nCompressThresholdBytes;
        public bool RpcIncludeTimestamp => m_vRpcIncludeTimestamp;
        public bool SyncIncludeTimestamp => m_vSyncIncludeTimestamp;
        public bool RpcIncludeChannel => m_vRpcIncludeChannel;
        public bool SyncIncludeChannel => m_vSyncIncludeChannel;
        public DeliveryMethod RpcDelivery => m_vRpcDelivery;
        public DeliveryMethod SyncDelivery => m_vSyncDelivery;

        // Topology getters
        public int TopologyUpdateIntervalFrames => m_nTopologyUpdateIntervalFrames;
        public int TopologyMinMigrationIntervalFrames => m_nTopologyMinMigrationIntervalFrames;
        public int TopologyImprovementThreshold => m_nTopologyImprovementThreshold;
        public int TopologyRequiredConsecutiveBetter => m_nTopologyRequiredConsecutiveBetter;
        public RouteSelectorType RouteSelector => m_vRouteSelector;
        public bool TopologyEnabled => m_vTopologyEnabled;
        public int FfimRttWeight => m_nFfimRttWeight;
        public int FfimJitterWeight => m_nFfimJitterWeight;
        public int FfimLossWeight => m_nFfimLossWeight;
        public int FfimEndpointWeightBonus => m_nFfimEndpointWeightBonus;
        public int StaleIdCullFrames => m_nStaleIdCullFrames;
        public int StructCommandFlushIntervalFrames => m_nStructCommandFlushIntervalFrames;
        public bool EnableInterpolationFallback => m_vEnableInterpolationFallback;
        public bool EnableSyncRelay => m_vEnableSyncRelay;
        public int SyncRelayTtl => m_nSyncRelayTtl;
        public int SyncRelayDedupWindowFrames => m_nSyncRelayDedupWindowFrames;
        public bool EnableSyncRelayLog => m_vEnableSyncRelayLog;
        public int SyncRelayDedupCapacity => m_nSyncRelayDedupCapacity;
        public int SyncRelayLogSampleRate => m_nSyncRelayLogSampleRate;

        // SyncApply logging
        public bool EnableSyncApplyLog => m_vEnableSyncApplyLog;
        public int SyncApplyLogSampleRate => m_nSyncApplyLogSampleRate;
    }

    /// <summary>
    /// Helper loader for NetworkConfig.
    /// </summary>
    public static class NetworkSettings
    {
        private static NetworkConfig s_pConfig;
        public static NetworkConfig Config
        {
            get
            {
                if (s_pConfig == null)
                {
                    s_pConfig = Resources.Load<NetworkConfig>("NetworkConfig");
                    if (s_pConfig == null)
                    {
                        s_pConfig = ScriptableObject.CreateInstance<NetworkConfig>();
                    }
                }
                return s_pConfig;
            }
        }
    }
}
