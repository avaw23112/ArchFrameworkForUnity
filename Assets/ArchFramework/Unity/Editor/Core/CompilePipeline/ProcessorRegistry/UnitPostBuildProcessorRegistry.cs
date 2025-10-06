#if UNITY_EDITOR

using Arch.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Arch.Compilation.Editor
{
	[AttributeUsage(AttributeTargets.Class)]
	public class PostBuildProcessorAttribute : Attribute
	{ }

	[TargetRegistry]
	public class UnitPostBuildProcessorRegistry : BaseTargetRegistry<IUnitPostBuildProcessor, PostBuildProcessorAttribute>
	{
	}
}

#endif