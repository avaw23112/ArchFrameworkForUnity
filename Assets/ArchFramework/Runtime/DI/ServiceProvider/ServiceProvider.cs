using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Arch.DI
{
	public sealed class ServiceProvider : IServiceProvider, IDisposable
	{
		private readonly Dictionary<Type, ServiceDescriptor> _map;
		private readonly ConcurrentDictionary<Type, object> _singletons = new();
		private readonly object _lock = new();
		private readonly ServiceProvider _root;
		private readonly bool _isScope;
		private bool _disposed;

		internal ServiceProvider(IEnumerable<ServiceDescriptor> descriptors, ServiceProvider root = null)
		{
			_map = descriptors.ToDictionary(d => d.ServiceType, d => d);
			_root = root ?? this;
			_isScope = root != null;
		}

		public object GetService(Type serviceType)
		{
			if (_disposed) throw new ObjectDisposedException(nameof(ServiceProvider));

			if (!_map.TryGetValue(serviceType, out var desc))
			{
				if (_root != this && _root._map.TryGetValue(serviceType, out var parentDesc))
					return _root.Resolve(parentDesc, new HashSet<Type>());
				return null;
			}
			return Resolve(desc, new HashSet<Type>());
		}

		public T Get<T>() => (T)GetService(typeof(T));

		private object Resolve(ServiceDescriptor desc, HashSet<Type> callstack)
		{
			switch (desc.Lifetime)
			{
				case ServiceLifetime.Instance:
					return desc.Instance;

				case ServiceLifetime.Singleton:
					return _root.GetOrCreateSingleton(desc, callstack);

				case ServiceLifetime.Transient:
					return CreateInstance(desc, callstack);

				case ServiceLifetime.Scoped:
					var owner = _isScope ? this : _root;
					return owner.GetOrCreateSingleton(desc, callstack);

				default:
					throw new NotSupportedException();
			}
		}

		private object GetOrCreateSingleton(ServiceDescriptor desc, HashSet<Type> callstack)
		{
			return _singletons.GetOrAdd(desc.ServiceType, _ =>
			{
				lock (_lock) return CreateInstance(desc, callstack);
			});
		}

		private object CreateInstance(ServiceDescriptor desc, HashSet<Type> callstack)
		{
			//优先用工程创建
			if (desc.Factory != null)
				return desc.Factory(this);

			//无效服务类型
			if (desc.ImplType == null)
				throw new InvalidOperationException($"No implementation for {desc.ServiceType}");

			//禁止循环引用
			if (!callstack.Add(desc.ImplType))
				throw new InvalidOperationException($"Circular dependency: {desc.ImplType}");

			//反射创建实例
			try
			{
				var ctor = desc.ImplType
					.GetConstructors()
					.OrderByDescending(c => c.GetParameters().Length)
					.FirstOrDefault();
				var ps = ctor.GetParameters();
				var args = ps.Select(p => GetService(p.ParameterType)).ToArray();
				var instance = Activator.CreateInstance(desc.ImplType, args);
				InjectMembers(instance);
				return instance;
			}
			finally
			{
				callstack.Remove(desc.ImplType);
			}
		}

		private void InjectMembers(object instance)
		{
			var t = instance.GetType();

			foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
			{
				if (f.GetCustomAttribute<InjectAttribute>() == null) continue;
				var dep = GetService(f.FieldType);
				if (dep != null) f.SetValue(instance, dep);
			}

			foreach (var p in t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
			{
				if (p.GetCustomAttribute<InjectAttribute>() == null || !p.CanWrite) continue;
				var dep = GetService(p.PropertyType);
				if (dep != null) p.SetValue(instance, dep, null);
			}
		}

		public ServiceProvider CreateScope(IEnumerable<ServiceDescriptor> overrides = null)
		{
			var combined = _map.Values.ToList();
			if (overrides != null)
			{
				foreach (var d in overrides)
				{
					combined.RemoveAll(x => x.ServiceType == d.ServiceType);
					combined.Add(d);
				}
			}
			return new ServiceProvider(combined, this);
		}

		public void Dispose()
		{
			if (_disposed) return;
			_disposed = true;
			foreach (var obj in _singletons.Values)
				(obj as IDisposable)?.Dispose();
		}
	}
}