using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arch
{
	public interface IState
	{
		public long StateKey { get; set; }
		public void Enter();
		public void Exit();
	}

	public struct StateChangeEvent
	{
		public long nNextStateKey;
	}
}
