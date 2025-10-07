using System;

namespace Arch.Tools
{
	/// <summary>
	/// 可自行更换实现
	/// </summary>
	public sealed class UnityLoadProgress : ILoadProgress
	{
		public float Value { get; private set; }
		public string Tip { get; private set; }

		public void Report(float value)
		{
			if (value < 0f) value = 0f;
			else if (value > 1f) value = 1f;
			Value = value;
		}

		public void ReportTip(string tip)
		{
			Tip = tip ?? string.Empty;
		}

		public void Set(float value, string tip)
		{
			Value = Math.Clamp(value, 0f, 1f);
			Tip = tip ?? string.Empty;
		}
	}
}