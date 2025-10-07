using System;

namespace Arch.DI
{
	/// <summary>
	/// 每一个IService都要主动描述自己是什么服务，需要对接到哪个接口上
	/// </summary>
	public sealed class ServiceDescriptor
	{
		/// <summary>
		/// 服务对外暴露的类型（通常是接口），如 IShortNameService
		/// 解决：我要将ImplType对应的服务注册到哪里？
		/// </summary>
		public Type ServiceType { get; }

		/// <summary>
		/// 具体实现类型，如 ShortNameService
		/// 解决：我要注册哪些服务？
		/// </summary>
		public Type ImplType { get; }

		/// <summary>
		/// 生命周期策略（Singleton / Scoped / Transient / Instance）
		/// 和DI容器中对实例的存储策略有关
		/// </summary>
		public ServiceLifetime Lifetime { get; }

		/// <summary>
		/// 可选的Factory方法，可用于避免反射调用
		/// </summary>
		public Func<IServiceProvider, object> Factory { get; }

		/// <summary>
		/// 服务的实例，ImplType的实例
		/// 在单例模式中，会复用该Instance
		/// </summary>
		public object Instance { get; }

		public ServiceDescriptor(Type service, Type impl, ServiceLifetime lifetime,
			Func<IServiceProvider, object> factory, object instance)
		{
			ServiceType = service;
			ImplType = impl;
			Lifetime = lifetime;
			Factory = factory;
			Instance = instance;
		}

		public static ServiceDescriptor Singleton(Type TService, Type TImpl)
		=> new(TService, TImpl, ServiceLifetime.Singleton, null, null);

		public static ServiceDescriptor Singleton(Type TService, object instance)
		=> new(TService, null, ServiceLifetime.Instance, null, instance);

		public static ServiceDescriptor Transient(Type TService, Type TImpl)
=> new(TService, TImpl, ServiceLifetime.Transient, null, null);

		public static ServiceDescriptor Scoped(Type TService, Type TImpl)
			=> new(TService, TImpl, ServiceLifetime.Scoped, null, null);

		public static ServiceDescriptor Singleton<TService, TImpl>()
			where TImpl : TService
			=> new(typeof(TService), typeof(TImpl), ServiceLifetime.Singleton, null, null);

		public static ServiceDescriptor Singleton<TService>(TService instance)
			=> new(typeof(TService), null, ServiceLifetime.Instance, null, instance);

		public static ServiceDescriptor Transient<TService, TImpl>()
			where TImpl : TService
			=> new(typeof(TService), typeof(TImpl), ServiceLifetime.Transient, null, null);

		public static ServiceDescriptor Scoped<TService, TImpl>()
			where TImpl : TService
			=> new(typeof(TService), typeof(TImpl), ServiceLifetime.Scoped, null, null);

		public static ServiceDescriptor Singleton<TService>(Func<IServiceProvider, TService> factory)
	=> new(typeof(TService), null, ServiceLifetime.Singleton, sp => factory(sp), null);
	}
}