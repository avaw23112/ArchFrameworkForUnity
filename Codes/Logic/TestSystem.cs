using Arch;
using Arch.Tools;

namespace Codes.Logic
{
	[System]
	internal class TestSystem : IPureUpdate
	{
		public void Update()
		{
			ArchLog.LogDebug("99999");
		}
	}
}