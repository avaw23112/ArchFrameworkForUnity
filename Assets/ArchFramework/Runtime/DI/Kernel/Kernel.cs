using System;
using System.Collections.Generic;

namespace Arch.DI
{
	public static class ArchKernel
	{
		public static ServiceProvider Root { get; private set; }

		public static void Init(params IService[] modules)
		{
			var services = new ServiceCollection();

			foreach (var m in modules)
				m.ConfigureServices(services);

			Root?.Dispose();
			Root = new ServiceProvider(services.ToDescriptors());
		}

		public static T Resolve<T>() => Root.Get<T>();

		public static WorldScope CreateWorldScope(IEnumerable<ServiceDescriptor> overrides = null)
		{
			var scope = Root.CreateScope(overrides);
			return new WorldScope(scope);
		}
	}

	public sealed class WorldScope : IDisposable, IServiceProvider
	{
		private readonly ServiceProvider _provider;

		internal WorldScope(ServiceProvider provider) => _provider = provider;

		public T Get<T>() => _provider.Get<T>();

		public object GetService(Type t) => _provider.GetService(t);

		public void Dispose() => _provider.Dispose();
	}
}