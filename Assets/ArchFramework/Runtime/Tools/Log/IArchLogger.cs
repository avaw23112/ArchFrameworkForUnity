using System;

namespace Arch.Tools
{
	public enum LogLevel
	{
		Debug = 0,
		Info = 1,
		Warning = 2,
		Error = 3,
		Exception = 4
	}

	public interface IArchLogger
	{
		LogLevel Level { get; set; }

		void Initialize();

		void Log(LogLevel level, string message, string filePath = "", int lineNumber = 0, Exception ex = null);

		void Shutdown();
	}
}