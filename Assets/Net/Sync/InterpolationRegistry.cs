using System;
using System.Collections.Generic;
using System.Reflection;
using Arch.Core;

namespace Arch.Net
{
    /// <summary>
    /// Builds interpolation descriptors (float field offsets) for types marked [Interpolate].
    /// </summary>
    public static class InterpolationRegistry
    {
        public sealed class Descriptor
        {
            public int WindowMs;
            public int[] FloatOffsets; // byte offsets within struct
        }

        private static readonly Dictionary<int, Descriptor> s_map = new Dictionary<int, Descriptor>();
        private static bool s_built;

        public static void EnsureBuilt()
        {
            if (s_built) return;
            Build();
            s_built = true;
        }

        private static void Build()
        {
            var types = ComponentRegistry.Types;
            for (int typeId = 1; typeId < types.Length; typeId++)
            {
                var t = types[typeId];
                if (t == null) continue;
                var attr = t.GetCustomAttribute<InterpolateAttribute>();
                if (attr == null) continue;

                var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var offsets = new List<int>();
                foreach (var f in fields)
                {
                    if (f.FieldType == typeof(float))
                    {
                        // If RuntimeHelpers/Marshal offset unavailable, reflection-based offset is not trivial.
                        // We assume sequential layout and use unsafe trick via TypedReference (not allowed in AOT).
                        // Fallback: require source generator later. For now, approximate by field order times sizeof(float).
                        offsets.Add(offsets.Count * sizeof(float));
                    }
                }
                s_map[typeId] = new Descriptor { WindowMs = attr.WindowMs, FloatOffsets = offsets.ToArray() };
            }
        }

        public static bool TryGet(int typeId, out Descriptor d) => s_map.TryGetValue(typeId, out d);
    }
}

