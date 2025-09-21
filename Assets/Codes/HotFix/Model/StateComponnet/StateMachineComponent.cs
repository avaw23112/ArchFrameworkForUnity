using Arch;
using System.Collections;
using System.Collections.Generic;

namespace Arch
{

	public struct StateMachineComponent : IComponent
	{
		public Queue<StateChangeEvent> queueStateCommend;
		public IState currentState;
	}
}

