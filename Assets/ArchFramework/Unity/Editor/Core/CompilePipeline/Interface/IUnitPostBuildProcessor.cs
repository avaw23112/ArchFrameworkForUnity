#if UNITY_EDITOR

using System.IO;

namespace Arch.Compilation.Editor
{
	public interface IProcessor
	{
		/// <summary> 唯一名称（用于配置序列化） </summary>
		string Name { get; }

		/// <summary> 描述说明（用于UI展示） </summary>
		string Description { get; }
	}

	/// <summary>
	/// 所有编译中处理器实现该接口
	/// </summary>
	public interface IUnitPostBuildProcessor : IProcessor
	{
		/// <summary> 执行后处理逻辑 </summary>
		void Process(ArchBuildConfig cfg, string builtDllPath);
	}
}

#endif