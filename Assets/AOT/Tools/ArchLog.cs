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

		// ��־����ö��
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
				// �ڱ༭���в���ʼ���ļ���־
#if !UNITY_EDITOR
                // ������־Ŀ¼
                if (!Directory.Exists(LogDirectory))
                {
                    Directory.CreateDirectory(LogDirectory);
                }

                // ������־�ļ�
                string logFilePath = Path.Combine(LogDirectory, $"log-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
                _logFileWriter = new StreamWriter(logFilePath, true)
                {
                    AutoFlush = true
                };

                // д����־�ļ�ͷ
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

		// ������־���� - ��ջ�����С��
		private static void LogInternal(string message, UnityEngine.LogType logType, string filePath, int lineNumber, bool includeStackTrace = false, Exception ex = null)
		{
			// �����Unity����̨ - ʹ��ԭ����������˫����ת����
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


			// д���ļ���־ - ������������Ϣ��ֻ�ڷǱ༭�������£�
#if !UNITY_EDITOR
	// ��ȡ���ļ���
			string fileName = filePath.Substring(filePath.IndexOf("Assets"));
            if (_logFileWriter != null)
            {
                _logFileWriter.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{logType}]");
                _logFileWriter.WriteLine("Message:");
                _logFileWriter.WriteLine($" {message}");

                // ����Error��Exception�������־����ӹ��˺�Ķ�ջ����
                if (includeStackTrace)
                {
                    string filteredStackTrace = GetFilteredStackTrace();
                    if (!string.IsNullOrEmpty(filteredStackTrace))
                    {
                        _logFileWriter.WriteLine("Stack Trace:");
                        _logFileWriter.WriteLine(filteredStackTrace);
                        _logFileWriter.WriteLine(); // ���зָ�
                    }
                }
                else
                {
                    _logFileWriter.WriteLine("File Trace:");
                    _logFileWriter.WriteLine($"  at {fileName}:{lineNumber}");
                    _logFileWriter.WriteLine(); // ���зָ�
                    _logFileWriter.WriteLine(); // ���зָ�
                }
                if (ex != null)
                {
                    _logFileWriter.WriteLine($"Exception: ");
                    _logFileWriter.WriteLine($"  {ex}");
                }
            }
#endif

		}

		// ��ȡ���˺�Ķ�ջ���٣��Ƴ�Logger��ܱ���ĵ���
#if !UNITY_EDITOR
		private static string GetFilteredStackTrace()
		{
			try
			{
				var stackTrace = new StackTrace(2, true); // ������ǰ�����͵��÷���
				var frames = stackTrace.GetFrames();
				if (frames == null || frames.Length == 0) return null;

				var sb = new StringBuilder();
				bool foundFirstValidFrame = false;

				foreach (var frame in frames)
				{
					var method = frame.GetMethod();
					var declaringType = method?.DeclaringType;

					if (declaringType == null) continue;

					// ����Logger�����صĵ���
					if (declaringType == typeof(ArchLog) ||
						declaringType.Namespace == LoggerNamespace)
					{
						continue;
					}

					// �ҵ���һ����Ч֡��ʼ��¼
					if (!foundFirstValidFrame)
					{
						foundFirstValidFrame = true;
					}

					// ��ȡ�ļ������к�
					string fileName = frame.GetFileName();
					int fileLine = frame.GetFileLineNumber();

					// ���ļ�·����ֻ��ʾ�ļ���������ʾ����·����
					if (!string.IsNullOrEmpty(fileName))
					{
						// ȷ��·����Assets��ʼ
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