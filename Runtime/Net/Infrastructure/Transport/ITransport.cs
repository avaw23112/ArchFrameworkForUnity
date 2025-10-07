using System;

namespace Arch.Net
{
	/// <summary>
	/// Transport abstraction; implementations provide event-driven networking.
	/// </summary>
	public interface ITransport : IDisposable
	{
	}
}