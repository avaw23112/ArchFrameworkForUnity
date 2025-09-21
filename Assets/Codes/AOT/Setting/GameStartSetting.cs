using UnityEngine;

namespace Arch
{
	//初始化启动配置SO对象
	[CreateAssetMenu(fileName = "GameStartSetting", menuName = "Arch/GameStartSetting")]
	public class GameStartSetting : ScriptableObject
	{
		public bool isRemoteUpdate;
	}

}
