using Arch.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Arch.Compilation.Editor
{
	public abstract class BaseTargetRegistry<TInterface, TAttr> : ITargetRegistry
		where TInterface : class
		where TAttr : Attribute
	{
		protected readonly Dictionary<string, TInterface> _map = new();

		public virtual IEnumerable<object> All() => _map.Values.Cast<object>();

		public bool TryGet(string name, out object processor)
		{
			var ok = _map.TryGetValue(name, out var p);
			processor = p;
			return ok;
		}

		public virtual IEnumerable<Type> RegisterTypes()
		{
			return GetType().Assembly.GetTypes()
				.Where(t => t.IsClass && !t.IsAbstract && typeof(TInterface).IsAssignableFrom(t))
				.Where(t => Attribute.IsDefined(t, typeof(TAttr)));
		}

		public virtual void RegisterAll()
		{
			_map.Clear();
			var types = RegisterTypes();
			foreach (var t in types)
			{
				try
				{
					var inst = (TInterface)Activator.CreateInstance(t);
					var nameProp = t.GetProperty("Name")?.GetValue(inst) as string ?? t.Name;
					_map[nameProp] = inst;
				}
				catch (Exception ex)
				{
					ArchLog.LogError($"[{typeof(TAttr).Name}] 注册失败: {t.Name} - {ex.Message}");
				}
			}
		}
	}
}