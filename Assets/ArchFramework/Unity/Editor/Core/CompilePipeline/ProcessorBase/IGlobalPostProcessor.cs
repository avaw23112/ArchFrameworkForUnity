#if UNITY_EDITOR

namespace Arch.Compilation.Editor
{
	/// <summary>
	/// 全局后处理器接口（所有独立编译后处理完成后执行）
	/// </summary>
	public interface IGlobalPostProcessor : IProcessor
	{
		/// <summary>
		/// 执行全局后处理逻辑（例如打包所有产物）
		/// </summary>
		void Process(ArchBuildConfig cfg);
	}
}

#endif