using Arch.Core;
using System.Collections.Generic;

namespace Arch.Net
{
	/// <summary>
	/// Holds mapping from ArchetypeId to ordered TypeId signature.
	/// Filled from ArchetypeManifest RPC.
	/// </summary>
	public static class ArchetypeRegistry
	{
		private static readonly Dictionary<uint, int[]> s_dicArchTypes = new Dictionary<uint, int[]>();
		private static readonly Dictionary<int, uint> s_dicSingleTypeToArch = new Dictionary<int, uint>();

		public static void Register(uint archId, int[] typeIds)
		{
			s_dicArchTypes[archId] = typeIds;
			if (typeIds != null && typeIds.Length == 1)
			{
				s_dicSingleTypeToArch[typeIds[0]] = archId;
			}
		}

		public static void RegisterRange(IEnumerable<(uint, int[])> entries)
		{
			foreach (var e in entries)
			{
				s_dicArchTypes[e.Item1] = e.Item2;
				if (e.Item2 != null && e.Item2.Length == 1)
				{
					s_dicSingleTypeToArch[e.Item2[0]] = e.Item1;
				}
			}
		}

		public static bool TryGet(uint archId, out int[] typeIds)
		{
			return s_dicArchTypes.TryGetValue(archId, out typeIds);
		}

		/// <summary>
		/// Resolve typeIds for an archetype id. If not registered and archId equals a known single typeId,
		/// returns that as a single-component signature.
		/// </summary>
		public static bool TryResolveTypeIds(uint archId, out int[] typeIds)
		{
			if (s_dicArchTypes.TryGetValue(archId, out typeIds)) return true;
			// Fallback: treat archId as single-component type id if it exists in ComponentRegistry
			int tId = (int)archId;
			if (tId > 0)
			{
				var types = ComponentRegistry.Types; // ReadOnlySpan<Type?>
				if ((uint)tId < (uint)types.Length && types[tId] != null)
				{
					typeIds = new int[] { tId };
					return true;
				}
			}
			typeIds = null;
			return false;
		}

		public static bool TryGetArchIdForSingleType(int typeId, out uint archId)
		{
			return s_dicSingleTypeToArch.TryGetValue(typeId, out archId);
		}
	}
}