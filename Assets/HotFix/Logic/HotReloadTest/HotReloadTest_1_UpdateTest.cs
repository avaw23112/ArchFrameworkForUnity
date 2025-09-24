using Arch;
using Arch.Core;
using Arch.Tools;


[System]
public class HotReloadTest_1_Start : IAwake
{
	public void Awake()
	{
		Entity entity = NamedWorld.DefaultWord.Create(new HotReloadTest_Model() { a1 = 1 });
	}
}

[System]
public class HotReloadTest_1_UpdateTest : UpdateSystem<HotReloadTest_Model>
{
	protected override void Run(Entity entity, ref HotReloadTest_Model component_T1)
	{
		ArchLog.Debug($"{component_T1.a1 + 99}");
		ArchLog.Debug($"{component_T1.a1 + 999}");
		ArchLog.Debug($"{component_T1.a1 + 1}");
		ArchLog.Debug($"{component_T1.a1 + 2}"); ArchLog.Debug($"{component_T1.a1 + 3}");
		ArchLog.Debug($"{component_T1.a1 + 4}"); ArchLog.Debug($"{component_T1.a1 + 59}");
		ArchLog.Debug($"{component_T1.a1 + 929}");
	}
}

