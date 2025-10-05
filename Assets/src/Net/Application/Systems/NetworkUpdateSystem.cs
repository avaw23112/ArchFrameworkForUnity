namespace Arch.Net
{
	/// <summary>
	/// Pump transport and drain command queue each frame; limited to entities with NetworkRuntime.
	/// Marked [Last] to run after other update systems.
	/// </summary>
	[System]
	[Last]
	public sealed class NetworkUpdateSystem : IUpdate
	{
		/// <summary>
		/// Pump transport and drain command/packet queues according to NetworkSettings.
		/// </summary>
		public void Update()
		{
			Unique.Component<NetworkRuntime>.Setter((ref NetworkRuntime runtime) =>
			{
				NetworkSingleton.EnsureInitialized(ref runtime);
				NetworkSingleton.Session?.Update();
				NetworkCommandQueue.Drain(NetworkSettings.Config.CommandsPerFrame, NetworkSettings.Config.PacketsPerFrame);
				NetworkRouter.Tick();
			});
		}
	}
}