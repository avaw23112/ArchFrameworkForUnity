using Arch.Buffer;
using Collections;
using System.Collections.Generic;

namespace Arch
{
	public class CommandBufferHandler
	{
		public bool isHasCommand;
		public CommandBuffer commendBuffer;
	}

	[Unique]
	public struct CommendBuffersComponent : IComponent
	{
		public Dictionary<int, CommandBufferHandler> commandBuffers;
	}
}