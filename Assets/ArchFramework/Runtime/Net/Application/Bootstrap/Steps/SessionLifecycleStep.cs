using Arch.Tools;

namespace Arch.Net.Application.Bootstrap.Steps
{
	public sealed class SessionLifecycleStep : INetworkInitializationStep
	{
		public void Initialize(Session session, ref NetworkRuntime runtime)
		{
			var endpoint = runtime.Endpoint;
			if (string.IsNullOrEmpty(endpoint))
			{
				endpoint = NetworkSettings.Config.DefaultEndpoint;
				runtime.Endpoint = endpoint;
			}
			session.OnConnect += () => ArchLog.LogInfo($"Connected {endpoint}");
			session.OnDisconnect += reason => ArchLog.LogWarning($"Disconnected: {reason}");
			session.OnReconnect += () => ArchLog.LogInfo("Reconnected");
			session.OnNetworkUnstable += hint => ArchLog.LogWarning($"Network unstable: {hint}");

			NetworkCommandQueue.RegisterPacketHandler(session.HandlePacket);
		}
	}
}