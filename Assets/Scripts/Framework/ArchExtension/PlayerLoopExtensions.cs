using System.Linq;
using UnityEditor;
using UnityEngine.LowLevel;

namespace Arch
{
    public static class PlayerLoopExtensions
    {
        public static void InsertSystemAfter<T>(this PlayerLoopSystem system, PlayerLoopSystem newSystem)
            where T : struct
        {
            var loopList = system.subSystemList.ToList();
            int index = loopList.FindIndex(s => s.type == typeof(T));
            if (index >= 0)
            {
                loopList.Insert(index + 1, newSystem);
                system.subSystemList = loopList.ToArray();
            }
        }

        public static void InsertSystemBefore<T>(this PlayerLoopSystem system, PlayerLoopSystem newSystem)
            where T : struct
        {
            var loopList = system.subSystemList.ToList();
            int index = loopList.FindIndex(s => s.type == typeof(T));
            if (index >= 0)
            {
                loopList.Insert(index, newSystem);
                system.subSystemList = loopList.ToArray();
            }
        }

        public static void InsertSystemWhenDestroy(this PlayerLoopSystem system, PlayerLoopSystem destroySystem)
        {
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged += (state) => destroySystem.updateDelegate();
#else
            Application.quitting += () => destroySystem.updateDelegate();
#endif
        }
    }
}