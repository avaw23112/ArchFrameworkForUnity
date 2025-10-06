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

	[TargetRegistry]
	public class PreBuildProcessorRegistry : BaseTargetRegistry<IPreBuildProcessor, PreBuildProcessorAttribute>
	{
	}
}

#endif