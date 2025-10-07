using Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Arch.DI
{
	/// <summary>
	/// 让 DI 容器自动扫描程序集内的服务类。
	/// </summary>
	public static class ServiceCollectionExtensions
	{
		/// <summary>
		/// 从指定程序集自动注册带有 [Service] 特性的类。
		/// </summary>
		public static IServiceCollection AddAllFromAssembly(
			this IServiceCollection services,
			Assembly assembly,
			Func<Type, bool> filter = null)
		{
			var types = assembly.GetTypes();
			List<(ServiceAttribute, Type)> serviceTypes = new List<(ServiceAttribute, Type)>();
			Collector.CollectAttributes(serviceTypes);

			foreach (var kv in serviceTypes)
			{
				ServiceAttribute serviceAttribute = kv.Item1;
				Type directType = kv.Item2;
				// 优先接口类型（如果未指定）
				var serviceType = serviceAttribute.ServiceType ?? directType.GetInterfaces().FirstOrDefault() ?? directType;

				switch (serviceAttribute.Lifetime)
				{
					case ServiceLifetime.Singleton:
						services.Add(ServiceDescriptor.Singleton(serviceType, directType));
						break;

					case ServiceLifetime.Transient:
						services.Add(ServiceDescriptor.Transient(serviceType, directType));
						break;

					case ServiceLifetime.Scoped:
						services.Add(ServiceDescriptor.Scoped(serviceType, directType));
						break;
				}
			}
			return services;
		}
	}

	/// <summary>
	/// 标记一个类自动注册到 DI 容器。
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class ServiceAttribute : BaseAttribute
	{
		public ServiceLifetime Lifetime { get; }
		public Type ServiceType { get; }

		public ServiceAttribute(ServiceLifetime lifetime = ServiceLifetime.Singleton, Type serviceType = null)
		{
			Lifetime = lifetime;
			ServiceType = serviceType;
		}
	}
}