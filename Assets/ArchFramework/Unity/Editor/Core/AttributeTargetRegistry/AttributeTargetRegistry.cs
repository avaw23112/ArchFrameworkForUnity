using Arch.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Arch.Compilation.Editor
{
	[AttributeUsage(AttributeTargets.Class)]
	public class TargetRegistryAttribute : Attribute
	{
	}

	public static class AttributeTargetRegistry
	{
		private static readonly Dictionary<Type, ITargetRegistry> _registries = new();

		public static void RegisterAllRegistries()
		{
			_registries.Clear();
			var types = typeof(AttributeTargetRegistry).Assembly.GetTypes()
				.Where(t => t.IsClass && !t.IsAbstract && typeof(ITargetRegistry).IsAssignableFrom(t))
				.Where(t => Attribute.IsDefined(t, typeof(TargetRegistryAttribute)));

			foreach (var t in types)
			{
				try
				{
					var inst = (ITargetRegistry)Activator.CreateInstance(t);
					inst.RegisterAll(); // 统一注册处理器
					_registries[t] = inst;
				}
				catch (Exception ex)
				{
					ArchLog.LogError($"[TargetRegistry] 注册失败: {t.Name} - {ex.Message}");
				}
			}
		}

		public static IEnumerable<TTarget> All<TRegistry, TTarget>()
	where TRegistry : ITargetRegistry
	where TTarget : class
		{
			if (_registries.TryGetValue(typeof(TRegistry), out ITargetRegistry registry))
			{
				return registry.All().Cast<TTarget>();
			}
			else
			{
				throw new Exception($"不存在此目标注册器：{typeof(TRegistry).Name}");
			}
		}

		public static TRegistry Get<TRegistry>() where TRegistry : class, ITargetRegistry
			=> _registries.TryGetValue(typeof(TRegistry), out var reg) ? reg as TRegistry : null;

		public static bool TryGet<TRegistry, TProcessor>(string name, out TProcessor processor)
			where TRegistry : class, ITargetRegistry
			where TProcessor : class
		{
			if (_registries.TryGetValue(typeof(TRegistry), out var reg) && reg.TryGet(name, out var obj))
			{
				processor = obj as TProcessor;
				return processor != null;
			}
			processor = null;
			return false;
		}
	}
}