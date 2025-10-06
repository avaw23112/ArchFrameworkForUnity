using System;
using System.Collections.Concurrent;

namespace Arch.Net
{
	/// <summary>
	/// In-memory loopback transport for local echo/testing without external dependencies.
	/// </summary>
	public sealed class MockLoopbackTransport : ITransport
	{
		public void Dispose()
		{
		}
	}
}