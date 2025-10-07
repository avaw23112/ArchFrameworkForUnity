using System;
using System.Collections.Generic;

namespace Arch.DI
{
	public sealed class ServiceCollection : IServiceCollection
	{
		private readonly List<ServiceDescriptor> _list = new();

		public IServiceCollection Add(ServiceDescriptor descriptor)
		{
			_list.Add(descriptor);
			return this;
		}

		public IServiceCollection AddSingleton<TService, TImpl>() where TImpl : TService
			=> Add(ServiceDescriptor.Singleton<TService, TImpl>());

		public IServiceCollection AddSingleton<TService>(Func<IServiceProvider, TService> factory)
			=> Add(ServiceDescriptor.Singleton(factory));

		public IServiceCollection AddSingleton<TService>(TService instance)
			=> Add(ServiceDescriptor.Singleton(instance));

		// ✅ Transient 普通注册
		public IServiceCollection AddTransient<TService, TImpl>() where TImpl : TService
			=> Add(ServiceDescriptor.Transient<TService, TImpl>());

		// ✅ Transient 工厂注册
		public IServiceCollection AddTransient<TService>(Func<IServiceProvider, TService> factory)
		{
			_list.Add(new ServiceDescriptor(
				typeof(TService), null,
				ServiceLifetime.Transient,
				sp => factory(sp),
				null));
			return this;
		}

		// ✅ Scoped 普通注册
		public IServiceCollection AddScoped<TService, TImpl>() where TImpl : TService
			=> Add(ServiceDescriptor.Scoped<TService, TImpl>());

		// ✅ Scoped 工厂注册
		public IServiceCollection AddScoped<TService>(Func<IServiceProvider, TService> factory)
		{
			_list.Add(new ServiceDescriptor(
				typeof(TService), null,
				ServiceLifetime.Scoped,
				sp => factory(sp),
				null));
			return this;
		}

		public IReadOnlyList<ServiceDescriptor> ToDescriptors() => _list;

		public void Reset()
		{
			_list.Clear();
		}
	}
}