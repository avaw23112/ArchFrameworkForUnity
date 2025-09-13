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
				// 禁用Unity默认的日志堆栈跟踪
				Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);
				Application.SetStackTraceLogType(LogType.Warning, StackTraceLogType.None);
				Application.SetStackTraceLogType(LogType.Error, StackTraceLogType.None);

				// 配置 Serilog，使用自定义的Unity控制台输出
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

			// 获取简化文件名
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

		// 自定义Unity控制台输出Sink
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

				// 根据日志级别选择不同的Unity输出方法
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

		// 自定义格式化器，用于控制Unity控制台的输出格式
		private class UnityConsoleFormatter : ITextFormatter
		{
			public void Format(LogEvent logEvent, TextWriter output)
			{
				// 输出基本日志信息
				output.Write($"{logEvent.Timestamp:HH:mm:ss} [{logEvent.Level.ToString().Substring(0, 3).ToUpper()}] {logEvent.RenderMessage()}");

				// 添加过滤后的堆栈跟踪
				string filteredStackTrace = GetFilteredStackTrace();
				if (!string.IsNullOrEmpty(filteredStackTrace))
				{
					output.Write("\n" + filteredStackTrace);
				}

				// 如果有异常，添加异常信息
				if (logEvent.Exception != null)
				{
					output.Write($"\nException: {logEvent.Exception}");
				}

				output.WriteLine();
			}

			// 获取过滤后的堆栈跟踪，移除Logger框架本身的调用
			private string GetFilteredStackTrace()
			{
				try
				{
					var stackTrace = new StackTrace(4, true); // 跳过4帧（当前方法、格式化器、Sink和Logger方法）
					var frames = stackTrace.GetFrames();
					if (frames == null || frames.Length == 0) return null;

					var sb = new StringBuilder();
					bool foundFirstValidFrame = false;

					foreach (var frame in frames)
					{
						var method = frame.GetMethod();
						var declaringType = method?.DeclaringType;

						if (declaringType == null) continue;

						// 跳过Logger类和Serilog相关的调用
						if (declaringType == typeof(Logger) ||
							declaringType.Namespace == LoggerNamespace ||
							declaringType.Namespace?.StartsWith("Serilog") == true)
						{
							continue;
						}

						// 找到第一个有效帧后开始记录
						if (!foundFirstValidFrame)
						{
							foundFirstValidFrame = true;
							sb.AppendLine("Stack Trace:");
						}

						// 获取文件名和行号
						string fileName = frame.GetFileName();
						int fileLine = frame.GetFileLineNumber();

						// 简化文件路径（只显示文件名，不显示完整路径）
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