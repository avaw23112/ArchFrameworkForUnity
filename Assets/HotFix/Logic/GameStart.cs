using Arch;
using Arch.Net;
using Arch.Tools;
using Assets.Scripts;
using Events;

namespace Assets.HotFix
{
    internal class GameStart : Event<GameStartEvent>
    {
        public override void Run(GameStartEvent value)
        {
            ArchLog.LogInfo("game start");
            HotReloadTest_Model hotReloadTest_Model = new HotReloadTest_Model() { a1 = 10 };
            byte[] bin = ComponentSerializer.Serialize(hotReloadTest_Model);
            HotReloadTest_Model hotReloadTest_Model1 = new HotReloadTest_Model();
            ComponentSerializer.Deserialize(bin, ref hotReloadTest_Model1);
            ArchLog.LogInfo($"{hotReloadTest_Model1.a1}");
        }
    }
}