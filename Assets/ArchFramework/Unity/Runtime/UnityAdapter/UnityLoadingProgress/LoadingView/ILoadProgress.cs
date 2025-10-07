using System;

namespace Arch
{
	/// <summary>加载进度与提示的可订阅服务。</summary>
	public interface ILoadProgress
	{
		/// 0..1
		float Value { get; }

		string Tip { get; }

		/// 主动上报（由 IGameLoader 或其他流程调用）
		void Report(float value);

		void ReportTip(string tip);

		/// 原子更新（可选）
		void Set(float value, string tip);
	}
}