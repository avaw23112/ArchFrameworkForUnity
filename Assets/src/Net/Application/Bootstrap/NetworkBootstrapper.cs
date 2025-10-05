using System;
using System.Collections.Generic;
using System.Linq;

namespace Arch.Net.Application.Bootstrap
{
	public sealed class NetworkBootstrapper
	{
		private readonly INetworkSessionFactory m_pSessionFactory;
		private readonly IReadOnlyList<INetworkInitializationStep> m_pSteps;

		public NetworkBootstrapper(INetworkSessionFactory sessionFactory, IEnumerable<INetworkInitializationStep> steps)
		{
			m_pSessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
			if (steps == null) throw new ArgumentNullException(nameof(steps));
			m_pSteps = steps as IReadOnlyList<INetworkInitializationStep> ?? steps.ToList();
		}

		public static NetworkBootstrapper CreateDefault()
		{
			return new NetworkBootstrapper(
				new DefaultNetworkSessionFactory(),
				new INetworkInitializationStep[]
				{
					new Steps.SessionLifecycleStep(),
					new Steps.RoutingInitializationStep(),
					new Steps.ConnectStep(),
					new Steps.RpcRegistrationStep(),
					new Steps.ServiceRegistrationStep(),
					new Steps.ManifestSynchronizationStep(),
					new Steps.WorldInitializationStep(),
				});
		}

		public Session Initialize(ref NetworkRuntime runtime)
		{
			var session = m_pSessionFactory.Create(ref runtime);
			foreach (var step in m_pSteps)
			{
				step.Initialize(session, ref runtime);
			}
			return session;
		}
	}
}