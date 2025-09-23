using Arch;
using Arch.Tools;

[System]
public class HotReloadTest_1_UpdateTest : IUpdate
{
	void IUpdate.Update()
	{
		ArchLog.Debug("update1");
	}
}
[System]
public class HotReloadTest_2_UpdateTest : IUpdate
{
	void IUpdate.Update()
	{
		ArchLog.Debug("update2");
	}
}


[System]
public class HotReloadTest_3_UpdateTest : IUpdate
{
	void IUpdate.Update()
	{
		ArchLog.Debug("hotupdate3");
	}
}

[System]
public class HotReloadTest_4_UpdateTest : IUpdate
{
	void IUpdate.Update()
	{
		ArchLog.Debug("hotupdate4");
	}
}
