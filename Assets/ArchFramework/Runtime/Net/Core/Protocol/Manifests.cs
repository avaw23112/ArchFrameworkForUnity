using Arch.Core;
using Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Arch.Net
{
	/// <summary>
	/// Simple binary (varint + UTF8) serializer for manifest messages to avoid generator dependency.
	/// Layout: [RpcId][count][entries...], entry = [nameLen][nameUtf8][typeId]
	/// </summary>
	public static class ManifestSerializer
	{
		public struct TypeEntry
		{
			public string Name;
			public int Id;
		}

		public struct ArchetypeEntry
		{
			public int[] TypeIds; // ordered signature
		}

		public struct ArchetypeEntryV2
		{
			public uint ArchId;
			public int[] TypeIds; // ordered
		}

		/// <summary>
		/// Build payload for RpcIds.TypeManifest using current ComponentRegistry mapping.
		/// </summary>
		public static byte[] BuildTypeManifest()
		{
			// Collect IComponent types and sort by FullName to mirror registration order
			var list = new List<Type>();
			Collector.CollectTypes<IComponent>(list);
			list.Sort((a, b) => string.CompareOrdinal(a.FullName, b.FullName));

			// First pass to estimate size
			int size = 1 /* RpcId */ + VarIntSize(list.Count);
			var utf8 = Encoding.UTF8;
			var entries = new List<TypeEntry>(list.Count);
			foreach (var t in list)
			{
				if (!ComponentRegistry.TryGet(t, out var ct)) continue;
				string name = t.FullName;
				int nLen = utf8.GetByteCount(name);
				size += VarIntSize(nLen) + nLen + VarIntSize(ct.Id);
				entries.Add(new TypeEntry { Name = name, Id = ct.Id });
			}

			var buf = new byte[size];
			int p = 0;
			buf[p++] = (byte)RpcIds.TypeManifest;
			WriteVarUInt(buf, ref p, (uint)entries.Count);
			foreach (var e in entries)
			{
				int nLen = utf8.GetByteCount(e.Name);
				WriteVarUInt(buf, ref p, (uint)nLen);
				p += utf8.GetBytes(e.Name, 0, e.Name.Length, buf, p);
				WriteVarUInt(buf, ref p, (uint)e.Id);
			}
			return buf;
		}

		/// <summary>
		/// Parse TypeManifest payload into entries.
		/// </summary>
		public static List<TypeEntry> ParseTypeManifest(byte[] payload, int offset, int length)
		{
			var res = new List<TypeEntry>();
			int p = offset;
			int end = offset + length;
			if (p >= end) return res;
			// payload[0] should be RpcIds.TypeManifest; caller ensures skipping header
			if (payload[p++] != (byte)RpcIds.TypeManifest) return res;
			uint count = ReadVarUInt(payload, ref p, end);
			var utf8 = Encoding.UTF8;
			for (uint i = 0; i < count; i++)
			{
				uint nLen = ReadVarUInt(payload, ref p, end);
				string name = nLen == 0 ? string.Empty : utf8.GetString(payload, p, (int)nLen);
				p += (int)nLen;
				uint id = ReadVarUInt(payload, ref p, end);
				res.Add(new TypeEntry { Name = name, Id = (int)id });
				if (p > end) break;
			}
			return res;
		}

		/// <summary>
		/// Build a minimal ArchetypeManifest: include single-type signatures for all components marked [NetworkSync].
		/// Layout: [RpcId][count][entry...], entry = [typeCount VarInt][TypeId VarInt repeated]
		/// </summary>
		public static byte[] BuildArchetypeManifest()
		{
			var list = new List<Type>();
			Attributes.Collector.CollectTypes<IComponent>(list);
			// filter by [NetworkSync]
			list.RemoveAll(t => t.GetCustomAttributes(typeof(Arch.Net.NetworkSyncAttribute), false).Length == 0);

			var entries = new List<ArchetypeEntry>();
			foreach (var t in list)
			{
				if (!ComponentRegistry.TryGet(t, out var ct)) continue;
				entries.Add(new ArchetypeEntry { TypeIds = new[] { ct.Id } });
			}

			int size = 1 + VarIntSize(entries.Count);
			foreach (var e in entries)
			{
				size += VarIntSize(e.TypeIds.Length);
				foreach (var id in e.TypeIds) size += VarIntSize(id);
			}
			var buf = new byte[size];
			int p = 0;
			buf[p++] = (byte)RpcIds.ArchetypeManifest;
			WriteVarUInt(buf, ref p, (uint)entries.Count);
			foreach (var e in entries)
			{
				WriteVarUInt(buf, ref p, (uint)e.TypeIds.Length);
				foreach (var id in e.TypeIds) WriteVarUInt(buf, ref p, (uint)id);
			}
			return buf;
		}

		/// <summary>
		/// Parse ArchetypeManifest into signatures of ordered TypeIds.
		/// </summary>
		public static List<ArchetypeEntry> ParseArchetypeManifest(byte[] payload, int offset, int length)
		{
			var res = new List<ArchetypeEntry>();
			int p = offset;
			int end = offset + length;
			if (p >= end) return res;
			if (payload[p++] != (byte)RpcIds.ArchetypeManifest) return res;
			uint count = ReadVarUInt(payload, ref p, end);
			for (uint i = 0; i < count && p < end; i++)
			{
				uint tcount = ReadVarUInt(payload, ref p, end);
				var ids = new int[tcount];
				for (int k = 0; k < tcount && p < end; k++)
				{
					ids[k] = (int)ReadVarUInt(payload, ref p, end);
				}
				res.Add(new ArchetypeEntry { TypeIds = ids });
			}
			return res;
		}

		/// <summary>
		/// Build ArchetypeManifestV2 from a world: include archId + ordered typeIds for each archetype.
		/// </summary>
		public static byte[] BuildArchetypeManifestV2(World world)
		{
			// Get all archIds in world
			// Assume world exposes GetArchetypeIds(Span<uint> ids)
			// Prepare buffer
			var archIds = new List<uint>();
			// Try estimate; if no count API, use increasing array sizes
			int cap = 1024;
			while (true)
			{
				var tmp = new uint[cap];
				world.GetArchetypeIds(tmp); // expected to fill existing count; excess zeros ignored
											// Collect non-zero ids
				archIds.Clear();
				for (int i = 0; i < tmp.Length; i++)
				{
					if (tmp[i] == 0)
						continue; // assume 0 is not a valid archId
					archIds.Add(tmp[i]);
				}
				break;
			}

			var entries = new List<ArchetypeEntryV2>(archIds.Count);
			foreach (var archId in archIds)
			{
				if (!world.TryGetArchetype(archId, out var archetype))
					continue;
				// Collect typeIds by probing ComponentRegistry types and sorting by typeIndex
				var pairs = new List<(int idx, int typeId)>();
				var types = ComponentRegistry.Types; // ReadOnlySpan<Type?>, index = typeId
				for (int typeId = 1; typeId < types.Length; typeId++)
				{
					if (types[typeId] == null) continue;
					if (archetype.TryGetTypeIndex(typeId, out var typeIndex))
						pairs.Add((typeIndex, typeId));
				}
				if (pairs.Count == 0) continue;
				pairs.Sort((a, b) => a.idx.CompareTo(b.idx));
				var typeIds = new int[pairs.Count];
				for (int i = 0; i < pairs.Count; i++)
					typeIds[i] = pairs[i].typeId;
				entries.Add(new ArchetypeEntryV2 { ArchId = archId, TypeIds = typeIds });
			}

			// Serialize: [RpcId][count VarInt] then entries of [archId VarInt][typeCount VarInt][typeId VarInt repeated]
			int size = 1 + VarIntSize(entries.Count);
			foreach (var e in entries)
			{
				size += VarIntSize((int)e.ArchId) + VarIntSize(e.TypeIds.Length);
				for (int i = 0; i < e.TypeIds.Length; i++) size += VarIntSize(e.TypeIds[i]);
			}
			var buf = new byte[size];
			int p = 0;
			buf[p++] = (byte)RpcIds.ArchetypeManifestV2;
			WriteVarUInt(buf, ref p, (uint)entries.Count);
			foreach (var e in entries)
			{
				WriteVarUInt(buf, ref p, (uint)e.ArchId);
				WriteVarUInt(buf, ref p, (uint)e.TypeIds.Length);
				foreach (var id in e.TypeIds) WriteVarUInt(buf, ref p, (uint)id);
			}
			return buf;
		}

		/// <summary>
		/// Parse ArchetypeManifestV2 payload with archId and ordered typeIds.
		/// </summary>
		public static List<ArchetypeEntryV2> ParseArchetypeManifestV2(byte[] payload, int offset, int length)
		{
			var res = new List<ArchetypeEntryV2>();
			int p = offset;
			int end = offset + length;
			if (p >= end) return res;
			if (payload[p++] != (byte)RpcIds.ArchetypeManifestV2) return res;
			uint count = ReadVarUInt(payload, ref p, end);
			for (uint i = 0; i < count && p < end; i++)
			{
				uint archId = ReadVarUInt(payload, ref p, end);
				uint tcount = ReadVarUInt(payload, ref p, end);
				var ids = new int[tcount];
				for (int k = 0; k < tcount && p < end; k++) ids[k] = (int)ReadVarUInt(payload, ref p, end);
				res.Add(new ArchetypeEntryV2 { ArchId = archId, TypeIds = ids });
			}
			return res;
		}

		// Helpers (reuse varint from existing methods)
		private static int VarIntSize(int v)
		{
			uint u = (uint)v; int n = 0; do { n++; u >>= 7; } while (u != 0); return n;
		}

		private static void WriteVarUInt(byte[] buf, ref int p, uint v)
		{
			while (v >= 0x80)
			{
				buf[p++] = (byte)(v | 0x80);
				v >>= 7;
			}
			buf[p++] = (byte)v;
		}

		private static uint ReadVarUInt(byte[] buf, ref int p, int end)
		{
			uint val = 0; int shift = 0;
			while (p < end)
			{
				byte b = buf[p++];
				val |= (uint)(b & 0x7F) << shift;
				if ((b & 0x80) == 0) break;
				shift += 7;
			}
			return val;
		}
	}
}