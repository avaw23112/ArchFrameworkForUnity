using Arch.DI;

namespace Arch.Net
{
	[System]
	public class NetRuntimeSystem : Unique.LifecycleSystem<NetRuntime>
	{
		protected override void OnAwake(ref NetRuntime component)
		{
			component.transport = ArchKernel.Resolve<ITransport>();
		}

		protected override void OnDestroy(ref NetRuntime component)
		{
			component.transport.Dispose();
		}
	}
}