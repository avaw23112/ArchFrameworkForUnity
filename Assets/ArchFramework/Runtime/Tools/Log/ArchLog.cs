using System;
using System.Runtime.CompilerServices;

namespace Arch.Tools
{
	public static class ArchLog
	{
		private static IArchLogger _logger;

		public static void SetLogger(IArchLogger logger)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_logger.Initialize();
		}

		public static void Shutdown() => _logger?.Shutdown();

		public static LogLevel CurrentLevel
		{
			get => _logger?.Level ?? LogLevel.Debug;
			set { if (_logger != null) _logger.Level = value; }
		}

		private static void Write(LogLevel level, string msg, Exception ex, string file, int line)
		{
			_logger?.Log(level, msg, file, line, ex);
		}

		public static void LogDebug(string msg, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
			=> Write(LogLevel.Debug, msg, null, file, line);

		public static void LogInfo(string msg, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
			=> Write(LogLevel.Info, msg, null, file, line);

		public static void LogWarning(string msg, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
			=> Write(LogLevel.Warning, msg, null, file, line);

		public static void LogError(string msg, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
			=> Write(LogLevel.Error, msg, null, file, line);

		public static void LogError(Exception ex, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
			=> Write(LogLevel.Exception, ex?.Message, ex, file, line);
	}
}