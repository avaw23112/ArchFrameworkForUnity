#if UNITY_EDITOR

using System.Linq;

namespace Arch.Compilation.Editor
{
	public class PureAwakeSection : BaseSystemSection
	{
		public PureAwakeSection() : base("PureAwake 系统", "systemBuildSetting.pureAwakeSystems",
			AttributeTargetRegistry.All<PureAwakeSystemRegistry, IPureAwake>().Select(o => o.GetType()))
		{ }
	}

	public class ReactiveAwakeSection : BaseSystemSection
	{
		public ReactiveAwakeSection() : base("ReactiveAwake 系统", "systemBuildSetting.reactiveAwakeSystems",
			AttributeTargetRegistry.All<ReactiveAwakeSystemRegistry, IReactiveAwake>().Select(o => o.GetType()))
		{ }
	}

	public class UpdateSection : BaseSystemSection
	{
		public UpdateSection() : base("Update 系统", "systemBuildSetting.updateSystems",
			AttributeTargetRegistry.All<UpdateSystemRegistry, IUpdate>().Select(o => o.GetType()))
		{ }
	}

	public class LateUpdateSection : BaseSystemSection
	{
		public LateUpdateSection() : base("LateUpdate 系统", "systemBuildSetting.lateUpdateSystems",
			AttributeTargetRegistry.All<LateUpdateSystemRegistry, ILateUpdate>().Select(o => o.GetType()))
		{ }
	}

	public class PureDestroySection : BaseSystemSection
	{
		public PureDestroySection() : base("PureDestroy 系统", "systemBuildSetting.pureDestroySystems",
			AttributeTargetRegistry.All<PureDestroySystemRegistry, IPureDestroy>().Select(o => o.GetType()))
		{ }
	}

	public class ReactiveDestroySection : BaseSystemSection
	{
		public ReactiveDestroySection() : base("ReactiveDestroy 系统", "systemBuildSetting.reactiveDestroySystems",
			AttributeTargetRegistry.All<ReactiveDestroySystemRegistry, IReactiveDestroy>().Select(o => o.GetType()))
		{ }
	}
}

#endif