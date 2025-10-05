#if UNITY_EDITOR

using Arch.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Arch.Compilation.Editor
{
	[AttributeUsage(AttributeTargets.Class)]
	public class PreBuildProcessorAttribute : Attribute
	{ }

	public static class PreBuildProcessorRegistry
	{
		private static readonly Dictionary<string, IPreBuildProcessor> _processors = new();

		static PreBuildProcessorRegistry()
		{
			RegisterAll();
		}

		private static void RegisterAll()
		{
			var types = typeof(PreBuildProcessorRegistry).Assembly.GetTypes()
				.Where(t => t.IsClass && !t.IsAbstract && typeof(IPreBuildProcessor).IsAssignableFrom(t))
				.Where(t => Attribute.IsDefined(t, typeof(PreBuildProcessorAttribute)));

			foreach (var t in types)
			{
				try
				{
					var inst = (IPreBuildProcessor)Activator.CreateInstance(t);
					_processors[inst.Name] = inst;
				}
				catch (Exception ex)
				{
					ArchLog.LogError($"[PreBuild] 注册失败: {t.Name} - {ex.Message}");
				}
			}
		}

		public static IEnumerable<IPreBuildProcessor> All => _processors.Values;

		public static bool TryGet(string name, out IPreBuildProcessor processor)
			=> _processors.TryGetValue(name, out processor);
	}
}

#endif