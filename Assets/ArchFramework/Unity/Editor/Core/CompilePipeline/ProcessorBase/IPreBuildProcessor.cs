namespace Arch.Compilation.Editor
{
	public interface IPreBuildProcessor : IProcessor
	{
		/// <summary>
		/// 执行编译前处理逻辑
		/// </summary>
		void Process(ArchBuildConfig cfg);
	}
}