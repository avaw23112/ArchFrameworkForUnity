namespace Arch.Net.Application.Bootstrap.Steps
{
	public sealed class RoutingInitializationStep : INetworkInitializationStep
	{
		public void Initialize(Session session, ref NetworkRuntime runtime)
		{
			NetworkRouter.Initialize(session);
			var selector = NetworkSettings.Config.RouteSelector;
			switch (selector)
			{
				case NetworkConfig.RouteSelectorType.FFIM:
					NetworkRouter.UseSelector(new FfimSelector());
					break;

				default:
					NetworkRouter.UseSelector(new SimpleLatencySelector());
					break;
			}
		}
	}
}