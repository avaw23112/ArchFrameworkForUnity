using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Tools
{
	public class Logger
	{
		[HideInCallstack]
		public static void Debug(string message)
		{
			UnityEngine.Debug.Log(message);
		}
		[HideInCallstack]
		public static void Warning(string message)
		{
			UnityEngine.Debug.LogWarning(message);
		}
		[HideInCallstack]
		public static void Error(string message)
		{
			UnityEngine.Debug.LogError(message);
		}
	}
}