using System;

namespace Arch.DI
{
	// 服务生命周期枚举
	public enum ServiceLifetime
	{
		Singleton, // 全局唯一
		Transient, // 每次新建
		Instance,  // 已有实例注册
		Scoped     // 每个 Scope 独立（比如每个 World）
	}

	// 注册接口：负责“告诉容器有哪些服务”
	public interface IServiceCollection
	{
		IServiceCollection Add(ServiceDescriptor descriptor);

		IServiceCollection AddSingleton<TService, TImpl>() where TImpl : TService;

		IServiceCollection AddSingleton<TService>(Func<IServiceProvider, TService> factory);

		IServiceCollection AddSingleton<TService>(TService instance);

		IServiceCollection AddTransient<TService, TImpl>() where TImpl : TService;

		IServiceCollection AddTransient<TService>(Func<IServiceProvider, TService> factory);

		IServiceCollection AddScoped<TService, TImpl>() where TImpl : TService;

		IServiceCollection AddScoped<TService>(Func<IServiceProvider, TService> factory);

		void Reset();
	}

	// 模块接口：模块化注册的入口
	public interface IService
	{
		void ConfigureServices(IServiceCollection services);
	}

	// 标记属性：用于字段、属性、构造函数的自动注入
	[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Field | AttributeTargets.Property)]
	public sealed class InjectAttribute : Attribute
	{ }
}