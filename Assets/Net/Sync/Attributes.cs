using System;

namespace Arch.Net
{
    /// <summary>
    /// Mark a component type as participating in network Sync scan/apply.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class NetworkSyncAttribute : Attribute {}

    /// <summary>
    /// Enable delta encoding for a component during Sync. Future source generators can use field-level info.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class SyncDeltaAttribute : Attribute
    {
        public float TriggerRatio { get; } // send delta when changed-bytes/total >= ratio; default 0.1f
        public SyncDeltaAttribute(float triggerRatio = 0.1f) { TriggerRatio = triggerRatio; }
    }

    /// <summary>
    /// Hint for interpolation on the receiver side. Future generators can produce optimized paths.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class InterpolateAttribute : Attribute
    {
        public int WindowMs { get; }
        public InterpolateAttribute(int windowMs = 100) { WindowMs = windowMs; }
    }

    /// <summary>
    /// Optional per-field declaration for future generators (order/quantization). Not used at runtime now.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class SyncFieldAttribute : Attribute
    {
        public int Order { get; }
        public int QuantizeBits { get; }
        public SyncFieldAttribute(int order, int quantizeBits = 0)
        {
            Order = order; QuantizeBits = quantizeBits;
        }
    }
}

