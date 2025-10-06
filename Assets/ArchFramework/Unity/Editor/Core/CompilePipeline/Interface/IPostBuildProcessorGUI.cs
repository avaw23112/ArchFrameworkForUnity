using UnityEditor;

namespace Arch.Compilation.Editor
{
	public interface IPostBuildProcessorGUI
	{
		/// <summary>
		/// 在配置面板中绘制此处理器的独立 GUI。
		/// </summary>
		void OnGUI(SerializedObject config);
	}
}