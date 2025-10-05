#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Arch.Compilation.Editor
{
	[AttributeUsage(AttributeTargets.Class)]
	public class GlobalPostBuildProcessorAttribute : Attribute
	{ }

	public static partial class GlobalPostBuildProcessorRegistry
	{
		private static readonly Dictionary<string, IGlobalPostProcessor> _globalProcessors = new();

		static GlobalPostBuildProcessorRegistry()
		{
			RegisterGlobalProcessors();
		}

		private static void RegisterGlobalProcessors()
		{
			var types = typeof(GlobalPostBuildProcessorRegistry).Assembly.GetTypes()
				.Where(t => t.IsClass && !t.IsAbstract && typeof(IGlobalPostProcessor).IsAssignableFrom(t))
				.Where(t => Attribute.IsDefined(t, typeof(GlobalPostBuildProcessorAttribute)));

			foreach (var t in types)
			{
				try
				{
					var inst = (IGlobalPostProcessor)Activator.CreateInstance(t);
					_globalProcessors[inst.Name] = inst;
				}
				catch (Exception ex)
				{
					Debug.LogError($"[PostBuild] 注册全局后处理器失败: {t.Name} - {ex.Message}");
				}
			}
		}

		public static IEnumerable<IGlobalPostProcessor> All => _globalProcessors.Values;

		public static bool TryGet(string name, out IGlobalPostProcessor processor)
			=> _globalProcessors.TryGetValue(name, out processor);
	}
}

#endif