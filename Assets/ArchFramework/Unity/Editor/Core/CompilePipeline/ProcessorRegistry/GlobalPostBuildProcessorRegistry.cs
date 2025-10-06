#if UNITY_EDITOR

using System;

namespace Arch.Compilation.Editor
{
	[AttributeUsage(AttributeTargets.Class)]
	public class GlobalPostBuildProcessorAttribute : Attribute
	{ }

	[TargetRegistry]
	public class GlobalPostBuildProcessorRegistry : BaseTargetRegistry<IGlobalPostProcessor, GlobalPostBuildProcessorAttribute>
	{
	}
}

#endif