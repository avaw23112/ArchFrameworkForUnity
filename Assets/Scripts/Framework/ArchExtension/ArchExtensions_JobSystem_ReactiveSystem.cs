using Unity.Collections;
using Unity.Jobs;

namespace Arch
{
	// 一个简单的Job示例
	public struct MyJob : IJob
	{
		public float a;
		public float b;
		public NativeArray<float> result; // 用于存储结果的NativeContainer

		public void Execute()
		{
			result[0] = a + b;
		}
	}
}
