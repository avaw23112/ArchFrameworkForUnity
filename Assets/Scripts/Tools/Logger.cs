using Serilog;
using Serilog.Context;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace Tools
{
	public static class Logger
	{
		private static Serilog.ILogger _logger;
		private static bool _isInitialized = false;
		private static readonly string LoggerNamespace = typeof(Logger).Namespace;

#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod]
#endif
		public static void Initialize()
		{
			if (_isInitialized) return;

			try
			{
				// ����UnityĬ�ϵ���־��ջ����
				Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
				Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
				Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.None);

				// ���� Serilog��ʹ���Զ����Unity����̨���
				_logger = new LoggerConfiguration()
					.MinimumLevel.Debug()
					.Enrich.FromLogContext()
					.WriteTo.Sink(new UnityConsoleSink())
					.WriteTo.File("Logs/log-.txt",
								 rollingInterval: RollingInterval.Day,
								 outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj} [at {Caller}]{NewLine}{Exception}")
					.CreateLogger();

				Log.Logger = _logger;

				Application.quitting += () =>
				{
					Log.CloseAndFlush();
				};

				_isInitialized = true;
			}
			catch (System.Exception ex)
			{
				UnityEngine.Debug.LogError($"Failed to initialize logger: {ex.Message}");
			}
		}

		public static void Debug(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
		{
			EnsureInitialized();
			LogWithCallerInfo(message, LogLevel.Debug, filePath, lineNumber);
		}

		public static void Info(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
		{
			EnsureInitialized();
			LogWithCallerInfo(message, LogLevel.Info, filePath, lineNumber);
		}

		public static void Warning(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
		{
			EnsureInitialized();
			LogWithCallerInfo(message, LogLevel.Warning, filePath, lineNumber);
		}

		public static void Error(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
		{
			EnsureInitialized();
			LogWithCallerInfo(message, LogLevel.Error, filePath, lineNumber);
		}

		public static void Exception(System.Exception ex, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
		{
			EnsureInitialized();
			LogWithCallerInfo(ex.ToString(), LogLevel.Exception, filePath, lineNumber, ex);
		}

		private static void EnsureInitialized()
		{
			if (!_isInitialized)
			{
				Initialize();
			}
		}

		private static void LogWithCallerInfo(string message, LogLevel level, string filePath, int lineNumber, System.Exception ex = null)
		{
			if (_logger == null) return;

			// ��ȡ���ļ���
			string fileName = System.IO.Path.GetFileName(filePath);
			string callerInfo = $"{fileName}:{lineNumber}";

			using (LogContext.PushProperty("Caller", callerInfo))
			{
				switch (level)
				{
					case LogLevel.Debug:
						_logger.Debug(message);
						break;
					case LogLevel.Info:
						_logger.Information(message);
						break;
					case LogLevel.Warning:
						_logger.Warning(message);
						break;
					case LogLevel.Error:
						_logger.Error(message);
						break;
					case LogLevel.Exception:
						_logger.Error(ex, message);
						break;
				}
			}
		}

		// �Զ���Unity����̨���Sink
		private class UnityConsoleSink : ILogEventSink
		{
			private readonly ITextFormatter _formatter;

			public UnityConsoleSink()
			{
				_formatter = new UnityConsoleFormatter();
			}

			public void Emit(LogEvent logEvent)
			{
				var writer = new StringWriter();
				_formatter.Format(logEvent, writer);
				string formattedLog = writer.ToString();

				// ������־����ѡ��ͬ��Unity�������
				switch (logEvent.Level)
				{
					case LogEventLevel.Warning:
						UnityEngine.Debug.LogWarning(formattedLog);
						break;
					case LogEventLevel.Error:
					case LogEventLevel.Fatal:
						UnityEngine.Debug.LogError(formattedLog);
						break;
					default:
						UnityEngine.Debug.Log(formattedLog);
						break;
				}
			}
		}

		// �Զ����ʽ���������ڿ���Unity����̨�������ʽ
		private class UnityConsoleFormatter : ITextFormatter
		{
			public void Format(LogEvent logEvent, TextWriter output)
			{
				// ���������־��Ϣ
				output.Write($"{logEvent.Timestamp:HH:mm:ss} [{logEvent.Level.ToString().Substring(0, 3).ToUpper()}] {logEvent.RenderMessage()}");

				// ��ӹ��˺�Ķ�ջ����
				string filteredStackTrace = GetFilteredStackTrace();
				if (!string.IsNullOrEmpty(filteredStackTrace))
				{
					output.Write("\n" + filteredStackTrace);
				}

				// ������쳣������쳣��Ϣ
				if (logEvent.Exception != null)
				{
					output.Write($"\nException: {logEvent.Exception}");
				}

				output.WriteLine();
			}

			// ��ȡ���˺�Ķ�ջ���٣��Ƴ�Logger��ܱ���ĵ���
			private string GetFilteredStackTrace()
			{
				try
				{
					var stackTrace = new StackTrace(4, true); // ����4֡����ǰ��������ʽ������Sink��Logger������
					var frames = stackTrace.GetFrames();
					if (frames == null || frames.Length == 0) return null;

					var sb = new StringBuilder();
					bool foundFirstValidFrame = false;

					foreach (var frame in frames)
					{
						var method = frame.GetMethod();
						var declaringType = method?.DeclaringType;

						if (declaringType == null) continue;

						// ����Logger���Serilog��صĵ���
						if (declaringType == typeof(Logger) ||
							declaringType.Namespace == LoggerNamespace ||
							declaringType.Namespace?.StartsWith("Serilog") == true)
						{
							continue;
						}

						// �ҵ���һ����Ч֡��ʼ��¼
						if (!foundFirstValidFrame)
						{
							foundFirstValidFrame = true;
							sb.AppendLine("Stack Trace:");
						}

						// ��ȡ�ļ������к�
						string fileName = frame.GetFileName();
						int fileLine = frame.GetFileLineNumber();

						// ���ļ�·����ֻ��ʾ�ļ���������ʾ����·����
						if (!string.IsNullOrEmpty(fileName))
						{
							fileName = System.IO.Path.GetFileName(fileName);
						}
						else
						{
							fileName = "Unknown";
						}

						sb.AppendLine($"  at {declaringType.Name}.{method.Name} (in {fileName}:{fileLine})");
					}

					return sb.Length > 0 ? sb.ToString() : null;
				}
				catch
				{
					return null;
				}
			}
		}

		private enum LogLevel
		{
			Debug,
			Info,
			Warning,
			Error,
			Exception
		}
	}
}