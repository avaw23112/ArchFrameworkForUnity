using System;

namespace Arch
{
    internal static class AttributeSystemHelper
    {
        public static bool isMarkedSystem(Type derectType)
        {
            return derectType.GetCustomAttributes(typeof(SystemAttribute), false).Length > 0;
        }
    }
}