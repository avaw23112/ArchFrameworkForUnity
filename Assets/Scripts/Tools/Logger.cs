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


		[RuntimeInitializeOnLoadMethod]
		public static void Initialize()
		{
			if (_isInitialized) return;

			try
			{
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

		// ������־���� - ��ջ�����С��
		private static void LogInternal(string message, UnityEngine.LogType logType, string filePath, int lineNumber, Exception ex = null)
		{
			if (!_isInitialized) Initialize();

			// ��ȡ���ļ���
			//string fileName = Path.GetFileName(filePath);
			string fileName = filePath.Substring(filePath.IndexOf("Assets"));

			// д���ļ���־ - ������������Ϣ
			if (_logFileWriter != null)
			{
				_logFileWriter.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{logType}] {message}\n at {fileName}:{lineNumber}\n");

				if (ex != null)
				{
					_logFileWriter.WriteLine($"Exception: {ex}");
				}
			}

			// �����Unity����̨ - ʹ��ԭ����������˫����ת����
			// ��������ֱ�ӵ���Unity��Debug��������ջ�����С
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