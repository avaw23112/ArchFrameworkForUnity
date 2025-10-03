using Arch.Core;

namespace Arch.Net
{
    /// <summary>\n    /// Unit marker components.\n    /// </summary>
    public struct Unit : IComponent
    {
        public ulong UnitId;    // Align with NetworkEntityId; can be generated locally for non-network units
    }

    /// <summary>\n    /// Unit marker components.\n    /// </summary>
    public struct UnitName : IComponent
    {
        public string Value;
    }
}


