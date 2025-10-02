namespace Arch.Net
{
    /// <summary>
    /// Logical endpoint descriptor for routing.
    /// </summary>
    public struct Endpoint
    {
        public string Url;   // e.g., lite://host:port, tcp://host:port
        public int Weight;   // preferred weight/hint
    }
}

