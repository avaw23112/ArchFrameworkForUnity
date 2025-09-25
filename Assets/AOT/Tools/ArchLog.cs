using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using UnityEngine;

namespace Arch.Tools
{
	public static class ArchLog
	{
#if !UNITY_EDITOR

		private static StreamWriter _logFileWriter;
		private static readonly string LogDirectory = Path.Combine(Application.dataPath, "..", "Logs");
#endif

		private static readonly string LoggerNamespace = typeof(ArchLog).Namespace;

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

		public static void Initialize()
		{
			try
			{
				// 在编辑器中不初始化文件日志
#if !UNITY_EDITOR
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
#endif

				UnityEngine.Debug.Log("Logger initialized successfully");
			}
			catch (Exception ex)
			{
				UnityEngine.Debug.LogError($"Failed to initialize logger: {ex.Message}");
			}
		}

		public static void LogDebug(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
		{
			if (CurrentLogLevel > LogLevel.Debug) return;
			LogInternal(message, UnityEngine.LogType.Log, filePath, lineNumber);
		}

		public static void LogInfo(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
		{
			if (CurrentLogLevel > LogLevel.Info) return;
			LogInternal(message, UnityEngine.LogType.Log, filePath, lineNumber);
		}

		public static void LogWarning(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
		{
			if (CurrentLogLevel > LogLevel.Warning) return;
			LogInternal(message, UnityEngine.LogType.Warning, filePath, lineNumber);
		}

		public static void LogError(string message, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
		{
			if (CurrentLogLevel > LogLevel.Error) return;
			LogInternal(message, UnityEngine.LogType.Error, filePath, lineNumber, true);
		}

		public static void LogError(Exception ex, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0)
		{
			LogInternal(ex.ToString(), UnityEngine.LogType.Exception, filePath, lineNumber, true, ex);
			throw ex;
		}

		// 核心日志方法 - 堆栈深度最小化
		private static void LogInternal(string message, UnityEngine.LogType logType, string filePath, int lineNumber, bool includeStackTrace = false, Exception ex = null)
		{
			// 输出到Unity控制台 - 使用原生方法保持双击跳转功能
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


			// 写入文件日志 - 包含调用者信息（只在非编辑器环境下）
#if !UNITY_EDITOR
	// 获取简化文件名
			string fileName = filePath.Substring(filePath.IndexOf("Assets"));
            if (_logFileWriter != null)
            {
                _logFileWriter.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{logType}]");
                _logFileWriter.WriteLine("Message:");
                _logFileWriter.WriteLine($" {message}");

                // 对于Error和Exception级别的日志，添加过滤后的堆栈跟踪
                if (includeStackTrace)
                {
                    string filteredStackTrace = GetFilteredStackTrace();
                    if (!string.IsNullOrEmpty(filteredStackTrace))
                    {
                        _logFileWriter.WriteLine("Stack Trace:");
                        _logFileWriter.WriteLine(filteredStackTrace);
                        _logFileWriter.WriteLine(); // 空行分隔
                    }
                }
                else
                {
                    _logFileWriter.WriteLine("File Trace:");
                    _logFileWriter.WriteLine($"  at {fileName}:{lineNumber}");
                    _logFileWriter.WriteLine(); // 空行分隔
                    _logFileWriter.WriteLine(); // 空行分隔
                }
                if (ex != null)
                {
                    _logFileWriter.WriteLine($"Exception: ");
                    _logFileWriter.WriteLine($"  {ex}");
                }
            }
#endif

		}

		// 获取过滤后的堆栈跟踪，移除Logger框架本身的调用
#if !UNITY_EDITOR
		private static string GetFilteredStackTrace()
		{
			try
			{
				var stackTrace = new StackTrace(2, true); // 跳过当前方法和调用方法
				var frames = stackTrace.GetFrames();
				if (frames == null || frames.Length == 0) return null;

				var sb = new StringBuilder();
				bool foundFirstValidFrame = false;

				foreach (var frame in frames)
				{
					var method = frame.GetMethod();
					var declaringType = method?.DeclaringType;

					if (declaringType == null) continue;

					// 跳过Logger类和相关的调用
					if (declaringType == typeof(ArchLog) ||
						declaringType.Namespace == LoggerNamespace)
					{
						continue;
					}

					// 找到第一个有效帧后开始记录
					if (!foundFirstValidFrame)
					{
						foundFirstValidFrame = true;
					}

					// 获取文件名和行号
					string fileName = frame.GetFileName();
					int fileLine = frame.GetFileLineNumber();

					// 简化文件路径（只显示文件名，不显示完整路径）
					if (!string.IsNullOrEmpty(fileName))
					{
						// 确保路径从Assets开始
						int assetsIndex = fileName.IndexOf("Assets");
						if (assetsIndex >= 0)
						{
							fileName = fileName.Substring(assetsIndex);
						}
						else
						{
							fileName = Path.GetFileName(fileName);
						}
					}
					else
					{
						fileName = "Unknown";
					}

					sb.AppendLine($"  at {declaringType.Name}.{method.Name} (at {fileName}:{fileLine})");
				}

				return sb.Length > 0 ? sb.ToString() : null;
			}
			catch
			{
				return null;
			}
		}
#endif
	}
}