using Arch.Core;

namespace Arch.Net
{
    /// <summary>
    /// Ownership component: marks an entity's authoritative clientId.
    /// </summary>
    public struct NetworkOwner : IComponent
    {
        public int OwnerClientId;
    }

    /// <summary>
    /// Holds local client identity.
    /// </summary>
    public static class OwnershipService
    {
        public static int MyClientId = 0;
        private static uint s_localCounter = 1;

        public static bool IsOwner(int ownerId) => ownerId == MyClientId;

        /// <summary>
        /// Generates a globally unique network-entity id using (clientId << 32) | localIncrement.
        /// </summary>
        public static ulong GenerateEntityId()
        {
            var id = ((ulong)(uint)MyClientId << 32) | s_localCounter;
            s_localCounter++;
            return id;
        }
    }
}
