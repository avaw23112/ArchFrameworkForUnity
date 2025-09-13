using System;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Tools
{
	public static class Logger
	{
		private static bool _isInitialized = false;
		private static StreamWriter _logFileWriter;
		private static readonly string LogDirectory = Path.Combine(Application.dataPath, "..", "Logs");

		// 日志级别枚举
		public enum LogLevel
		{
			Debug = 0,
			Info = 1,
			Warning = 2,
			Error = 3,
			Exception = 4
		}

		public static LogLevel CurrentLogLevel = LogLevel.Debug;


		[RuntimeInitializeOnLoadMethod]
		public static void Initialize()
		{
			if (_isInitialized) return;

			try
			{
				// 创建日志目录
				if (!Directory.Exists(LogDirectory))
				{
					Directory.CreateDirectory(LogDirectory);
				}

				// 创建日志文件
				string logFilePath = Path.Combine(LogDirectory, $"log-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
				_logFileWriter = new StreamWriter(logFilePath, true)
				{
					AutoFlush = true
				};

				// 写入日志文件头
				_logFileWriter.WriteLine($"Log started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
				_logFileWriter.WriteLine("==========================================");

				Application.quitting += () =>
				{
					if (_logFileWriter != null)
					{
						_logFileWriter.WriteLine("==========================================");
						_logFileWriter.WriteLine($"Log ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
						_logFileWriter.Close();
						_logFileWriter = null;
					}
				};

				_isInitialized = true;
				UnityEngine.Debug.Log("Logger initialized successfully");
			}
			catch (Exception ex)
			{
				UnityEngine.Debug.LogError($"Failed to initialize logger: {ex.Message}");
			}
		}

		public static void Debug(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
		{
			if (CurrentLogLevel > LogLevel.Debug) return;
			LogInternal(message, UnityEngine.LogType.Log, filePath, lineNumber);
		}

		public static void Info(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
		{
			if (CurrentLogLevel > LogLevel.Info) return;
			LogInternal(message, UnityEngine.LogType.Log, filePath, lineNumber);
		}

		public static void Warning(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
		{
			if (CurrentLogLevel > LogLevel.Warning) return;
			LogInternal(message, UnityEngine.LogType.Warning, filePath, lineNumber);
		}

		public static void Error(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
		{
			if (CurrentLogLevel > LogLevel.Error) return;
			LogInternal(message, UnityEngine.LogType.Error, filePath, lineNumber);
		}

		public static void Exception(Exception ex, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
		{
			LogInternal(ex.ToString(), UnityEngine.LogType.Exception, filePath, lineNumber, ex);
		}

		// 核心日志方法 - 堆栈深度最小化
		private static void LogInternal(string message, UnityEngine.LogType logType, string filePath, int lineNumber, Exception ex = null)
		{
			if (!_isInitialized) Initialize();

			// 获取简化文件名
			//string fileName = Path.GetFileName(filePath);
			string fileName = filePath.Substring(filePath.IndexOf("Assets"));

			// 写入文件日志 - 包含调用者信息
			if (_logFileWriter != null)
			{
				_logFileWriter.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{logType}] {message}\n at {fileName}:{lineNumber}\n");

				if (ex != null)
				{
					_logFileWriter.WriteLine($"Exception: {ex}");
				}
			}

			// 输出到Unity控制台 - 使用原生方法保持双击跳转功能
			// 这里我们直接调用Unity的Debug方法，堆栈深度最小
			switch (logType)
			{
				case UnityEngine.LogType.Warning:
					UnityEngine.Debug.LogWarning(message);
					break;
				case UnityEngine.LogType.Error:
				case UnityEngine.LogType.Exception:
					UnityEngine.Debug.LogError(message);
					break;
				default:
					UnityEngine.Debug.Log(message);
					break;
			}
		}
	}
}