using Arch.Core;

namespace Arch.Net
{
    /// <summary>
    /// Strategy hook for ownership migration policies.
    /// Implementations can decide whether to transfer ownership of an entity to another client.
    /// Not wired by default; reserved for future use.
    /// </summary>
    public interface IOwnershipStrategy
    {
        bool ShouldTransfer(Entity e, int currentOwnerClientId, int candidateOwnerClientId, in OwnershipContext ctx);
    }

    public struct OwnershipContext
    {
        public int CurrentRtt;
        public float CurrentLoss;
        public float CurrentJitter;
        public int CandidateRtt;
        public float CandidateLoss;
        public float CandidateJitter;
    }

    /// <summary>
    /// Default conservative strategy: never auto-transfer.
    /// </summary>
    public sealed class DefaultOwnershipStrategy : IOwnershipStrategy
    {
        public bool ShouldTransfer(Entity e, int currentOwnerClientId, int candidateOwnerClientId, in OwnershipContext ctx) => false;
    }
}

