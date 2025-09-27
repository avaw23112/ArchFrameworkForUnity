using Arch;
using Arch.Tools;
using Assets.Scripts;
using Events;
using MemoryPack;


namespace Assets.HotFix
{

	internal class GameStart : Event<GameStartEvent>
	{
		public override void Run(GameStartEvent value)
		{
			ArchLog.LogInfo("game start");
			HotReloadTest_Model hotReloadTest_Model = new HotReloadTest_Model();
			byte[] bin = MemoryPackSerializer.Serialize(hotReloadTest_Model);
			HotReloadTest_Model hotReloadTest_Model1 = new HotReloadTest_Model();
			MemoryPackSerializer.Deserialize(bin, ref hotReloadTest_Model1);
			ArchLog.LogInfo($"{hotReloadTest_Model1.a1}");
		}
	}
}
