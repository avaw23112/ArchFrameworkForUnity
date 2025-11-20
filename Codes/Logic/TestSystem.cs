using Arch;
using Arch.Tools;
using Attributes;

namespace Codes.Logic
{
	[Forget]
	[System]
	internal class TestSystem : IPureUpdate
	{
		public void Update()
		{
			ArchLog.LogDebug("99999");
		}
	}

	[System]
	internal class Test3System : IPureUpdate
	{
		public void Update()
		{
			ArchLog.LogInfo("99999");
		}
	}

	[Forget]
	[System]
	internal class Test1System : IPureAwake
	{
		public void Awake()
		{
			ArchLog.LogDebug("1");
		}
	}

	[Forget]
	[System]
	internal class Test2System : IPureAwake
	{
		public void Awake()
		{
			ArchLog.LogDebug("2");
		}
	}
}