using Attributes;

namespace Arch
{
	/// <summary>
	/// 用于避免某System,AttributeSystem或者其他系统在热重载中被重置
	/// </summary>
	internal class NotHotReloadAttribute : BaseAttribute
	{
	}
}
