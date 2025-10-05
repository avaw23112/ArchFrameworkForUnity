#if UNITY_2020_1_OR_NEWER

using System;
using System.IO;
using UnityEngine;

namespace Arch.Tools
{
	public class UnityLogger : IArchLogger
	{
		private StreamWriter _writer;
		private string _logPath;
		public LogLevel Level { get; set; } = LogLevel.Debug;

		public void Initialize()
		{
			string dir = Path.Combine(Application.dataPath, "..", "Logs");
			Directory.CreateDirectory(dir);
			_logPath = Path.Combine(dir, $"log-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
			_writer = new StreamWriter(_logPath, true) { AutoFlush = true };
			Application.quitting += Shutdown;
			Debug.Log($"[ArchLog] Initialized at {_logPath}");
		}

		public void Log(LogLevel level, string message, string filePath = "", int lineNumber = 0, Exception ex = null)
		{
			if (level < Level) return;

			string prefix = $"[{DateTime.Now:HH:mm:ss}] [{level}] {message}";
			switch (level)
			{
				case LogLevel.Warning: Debug.LogWarning(prefix); break;
				case LogLevel.Error:
				case LogLevel.Exception: Debug.LogError(prefix); break;
				default: Debug.Log(prefix); break;
			}

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

#endif