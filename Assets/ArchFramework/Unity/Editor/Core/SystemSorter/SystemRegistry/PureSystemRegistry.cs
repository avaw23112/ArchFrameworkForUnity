namespace Arch.Compilation.Editor
{
	[TargetRegistry]
	public class PureAwakeSystemRegistry : BaseTargetRegistry<ISystem, SystemAttribute>
	{
	}

	[TargetRegistry]
	public class PureUpdateSystemRegistry : BaseTargetRegistry<ISystem, SystemAttribute>
	{
	}

	[TargetRegistry]
	public class PureSystemRegistry : BaseTargetRegistry<ISystem, SystemAttribute>
	{
	}
}