namespace Tools
{
    public class Logger
    {
        public static void Debug(string message)
        {
            UnityEngine.Debug.Log(message);
        }

        public static void Warning(string message)
        {
            UnityEngine.Debug.LogWarning(message);
        }

        public static void Error(string message)
        {
            UnityEngine.Debug.LogError(message);
        }
    }
}