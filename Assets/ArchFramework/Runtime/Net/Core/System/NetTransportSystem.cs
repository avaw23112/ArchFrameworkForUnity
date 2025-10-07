using Arch.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Arch.Net
{
	[System]
	internal class NetTransportSystem : Unique.LateUpdateSystem<NetRuntime>
	{
		protected override void OnLateUpdate(ref NetRuntime component)
		{
			var transport = component.transport;
			if (transport == null)
			{
				transport = new MockLoopbackTransport();
				ArchLog.LogWarning("Cause not regist any transport,check to internal net mode");
			}
			transport.Poll();
		}
	}
}