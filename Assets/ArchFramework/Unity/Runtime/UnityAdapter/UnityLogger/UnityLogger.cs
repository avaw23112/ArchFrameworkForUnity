#if UNITY_2020_1_OR_NEWER

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;

namespace Arch.Tools
{
	public class UnityLogger : IArchLogger
	{
		private StreamWriter _writer;
		public LogLevel Level { get; set; } = LogLevel.Debug;
		private static readonly string LogDirectory = Path.Combine(Application.dataPath, "..", "Logs");
		private static readonly string LoggerNamespace = typeof(ArchLog).Namespace;

		public void Initialize()
		{
			try
			{
#if !UNITY_EDITOR
				// 在编辑器中不初始化文件日志
				// 创建日志目录
				if (!Directory.Exists(LogDirectory))
				{
					Directory.CreateDirectory(LogDirectory);
				}

				// 创建日志文件
				string logFilePath = Path.Combine(LogDirectory, $"log-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
				_writer = new StreamWriter(logFilePath, true)
				{
					AutoFlush = true
				};

				// 写入日志文件头
				_writer.WriteLine($"Log started at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
				_writer.WriteLine("==========================================");

				Application.quitting += () =>
				{
					if (_writer != null)
					{
						_writer.WriteLine("==========================================");
						_writer.WriteLine($"Log ended at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
						_writer.Close();
						_writer = null;
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

		public void Log(LogLevel level, string message, string filePath = "", int lineNumber = 0, Exception ex = null, bool includeStackTrace = false)
		{
			string prefix = $"[{DateTime.Now:HH:mm:ss}] [{level}] {message}";
			switch (level)
			{
				case LogLevel.Warning: UnityEngine.Debug.LogWarning(prefix); break;
				case LogLevel.Error: UnityEngine.Debug.LogError(prefix); break;
				case LogLevel.Exception: UnityEngine.Debug.LogError(prefix); break;
				default: UnityEngine.Debug.Log(prefix); break;
			}

			if (level < LogLevel.Info)
			{
				return;
			}

#if !UNITY_EDITOR
			// 获取简化文件名
			string fileName = filePath.Substring(filePath.IndexOf("Assets"));
			if (_writer != null)
			{
				_writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}]");
				_writer.WriteLine("Message:");
				_writer.WriteLine($" {message}");

				// 对于Error和Exception级别的日志，添加过滤后的堆栈跟踪
				if (includeStackTrace)
				{
					string filteredStackTrace = GetFilteredStackTrace();
					if (!string.IsNullOrEmpty(filteredStackTrace))
					{
						_writer.WriteLine("Stack Trace:");
						_writer.WriteLine(filteredStackTrace);
						_writer.WriteLine(); // 空行分隔
					}
				}
				_writer.WriteLine("File Trace:");
				_writer.WriteLine($"  at {fileName}:{lineNumber}");
				_writer.WriteLine(); // 空行分隔
				_writer.WriteLine(); // 空行分隔
				if (ex != null)
				{
					_writer.WriteLine($"Exception: ");
					_writer.WriteLine($"  {ex}");
				}
			}
#endif
		}

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

		public void Shutdown()
		{
			_writer?.Flush();
			_writer?.Close();
			_writer = null;
		}
	}
}

#endif