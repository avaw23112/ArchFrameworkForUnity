using Arch;
using Arch.Tools;
using Events;
using UnityEngine.Scripting;

namespace Assets.HotFix
{
	[Preserve]
	internal class GameStart : Event<GameStartEvent>
	{
		public override void Run(GameStartEvent value)
		{
			ArchLog.LogInfo("GameStart");
		}
	}
}