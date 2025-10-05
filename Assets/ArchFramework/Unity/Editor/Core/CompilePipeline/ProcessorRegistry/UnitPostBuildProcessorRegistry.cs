#if UNITY_EDITOR

using Arch.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Arch.Compilation.Editor
{
	[AttributeUsage(AttributeTargets.Class)]
	public class PostBuildProcessorAttribute : Attribute
	{ }

	public class UnitPostBuildProcessorRegistry : IProcessorRegistry
	{
		private static readonly Dictionary<string, IUnitPostBuildProcessor> _processors = new();

		static UnitPostBuildProcessorRegistry()
		{
			// 反射自动注册
			var types = typeof(UnitPostBuildProcessorRegistry).Assembly.GetTypes()
				.Where(t => t.IsClass && !t.IsAbstract && typeof(IUnitPostBuildProcessor).IsAssignableFrom(t))
				.Where(t => Attribute.IsDefined(t, typeof(PostBuildProcessorAttribute)));

			foreach (var t in types)
			{
				try
				{
					var inst = (IUnitPostBuildProcessor)Activator.CreateInstance(t);
					_processors[inst.Name] = inst;
				}
				catch (Exception ex)
				{
					ArchLog.LogError($"[PostBuild] 注册失败: {t.Name} - {ex.Message}");
				}
			}
		}

		public static IEnumerable<IUnitPostBuildProcessor> All => _processors.Values;

		public static bool TryGet(string name, out IUnitPostBuildProcessor processor)
			=> _processors.TryGetValue(name, out processor);
	}
}

#endif