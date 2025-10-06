using System;
using System.IO;

namespace Arch.Tools
{
	public class ConsoleLogger : IArchLogger
	{
		private StreamWriter _writer;
		private string _logPath;
		public LogLevel Level { get; set; } = LogLevel.Debug;

		public void Initialize()
		{
			string dir = Path.Combine(AppContext.BaseDirectory, "Logs");
			Directory.CreateDirectory(dir);
			_logPath = Path.Combine(dir, $"log-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
			_writer = new StreamWriter(_logPath, true) { AutoFlush = true };
			AppDomain.CurrentDomain.ProcessExit += (_, __) => Shutdown();
			Console.WriteLine($"[ArchLog] Initialized at {_logPath}");
		}

		public void Log(LogLevel level, string message, string filePath = "", int lineNumber = 0, Exception ex = null, bool includeStackTrace = false)
		{
			if (level < Level) return;

			string prefix = $"[{DateTime.Now:HH:mm:ss}] [{level}] {message}";
			ConsoleColor color = level switch
			{
				LogLevel.Warning => ConsoleColor.Yellow,
				LogLevel.Error or LogLevel.Exception => ConsoleColor.Red,
				_ => ConsoleColor.Gray
			};

			Console.ForegroundColor = color;
			Console.WriteLine(prefix);
			Console.ResetColor();

			if (_writer != null)
			{
				_writer.WriteLine(prefix);
				_writer.WriteLine($"  at {Path.GetFileName(filePath)}:{lineNumber}");
				if (ex != null)
					_writer.WriteLine(ex.ToString());
			}
		}

		public void Shutdown()
		{
			_writer?.Flush();
			_writer?.Close();
			_writer = null;
		}
	}
}