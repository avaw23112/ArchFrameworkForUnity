using Arch.Core;
using System;

namespace Arch.Net
{
	[System]
	public sealed partial class SyncScanSystem : GlobalUpdateSystem<NetworkRuntime>
	{
		protected override void Run(Entity entity, ref NetworkRuntime runtime)
		{
			if (Arch.Net.NetworkSettings.Config.UseChunkScan) return;
			var session = NetworkSingleton.Session;
			if (session == null) return;

			int maxPerPacket = Arch.Net.NetworkSettings.Config.EntitiesPerPacket;
			int netIdType = ComponentRegistryExtensions.GetTypeId(typeof(NetworkEntityId));

			var entries = SyncTypeCache.GetAll();
			for (int i = 0; i < entries.Count; i++)
			{
				var eMeta = entries[i];
				var send = eMeta.BuildAndSend;
				if (send == null) continue;
				send(world, eMeta.ArchId, netIdType, maxPerPacket);
			}
		}

		private static void BuildAndSendGeneric<T>(World world, uint archId, int netIdType, int maxEntities) where T : struct, IComponent
		{
			int elemSize = System.Runtime.CompilerServices.Unsafe.SizeOf<T>();
			var q = new Arch.Core.QueryDescription().WithAll<T, NetworkEntityId, NetworkOwner>();
			int n = 0;
			world.Query(in q, (Entity e, ref T c, ref NetworkEntityId nid, ref NetworkOwner owner) =>
			{
				if (n >= maxEntities) return;
				if (OwnershipService.IsOwner(owner.OwnerClientId)) n++;
			});
			if (n <= 0) return;

			int idsLen = n * sizeof(ulong);
			int valsLen = n * elemSize;
			var segMeta = new (int typeId, int elemSize, byte flags, int length)[2];
			segMeta[0] = (netIdType, sizeof(ulong), 0, idsLen);
			segMeta[1] = (ComponentRegistryExtensions.GetTypeId(typeof(T)), elemSize, 0, valsLen);

			const int StackThreshold = 4096;
			if (idsLen + valsLen <= StackThreshold)
			{
				unsafe
				{
					byte* ids = stackalloc byte[idsLen];
					byte* vals = stackalloc byte[valsLen];
					int i = 0;
					world.Query(in q, (Entity e, ref T c, ref NetworkEntityId nid, ref NetworkOwner owner) =>
					{
						if (i >= n) return;
						if (!OwnershipService.IsOwner(owner.OwnerClientId)) return;
						ulong id = nid.Value;
						int off = i * sizeof(ulong);
						ids[off + 0] = (byte)(id);
						ids[off + 1] = (byte)(id >> 8);
						ids[off + 2] = (byte)(id >> 16);
						ids[off + 3] = (byte)(id >> 24);
						ids[off + 4] = (byte)(id >> 32);
						ids[off + 5] = (byte)(id >> 40);
						ids[off + 6] = (byte)(id >> 48);
						ids[off + 7] = (byte)(id >> 56);
						System.Runtime.CompilerServices.Unsafe.WriteUnaligned(vals + i * elemSize, c);
						i++;
					});
					var packet = PacketBuilder.BuildSyncSegments(0, archId, 0, 0, (ushort)n, segMeta, (segIndex, dst) =>
					{
						if (segIndex == 0) new ReadOnlySpan<byte>(ids, idsLen).CopyTo(dst);
						else new ReadOnlySpan<byte>(vals, valsLen).CopyTo(dst);
					});
					SyncRelayService.SendUp(packet);
				}
			}
			else
			{
				var idsBytes = System.Buffers.ArrayPool<byte>.Shared.Rent(idsLen);
				var valBytes = System.Buffers.ArrayPool<byte>.Shared.Rent(valsLen);
				int i = 0;
				world.Query(in q, (Entity e, ref T c, ref NetworkEntityId nid, ref NetworkOwner owner) =>
				{
					if (i >= n) return;
					if (!OwnershipService.IsOwner(owner.OwnerClientId)) return;
					ulong id = nid.Value;
					unchecked
					{
						int off = i * sizeof(ulong);
						idsBytes[off + 0] = (byte)(id);
						idsBytes[off + 1] = (byte)(id >> 8);
						idsBytes[off + 2] = (byte)(id >> 16);
						idsBytes[off + 3] = (byte)(id >> 24);
						idsBytes[off + 4] = (byte)(id >> 32);
						idsBytes[off + 5] = (byte)(id >> 40);
						idsBytes[off + 6] = (byte)(id >> 48);
						idsBytes[off + 7] = (byte)(id >> 56);
					}
					WriteUnaligned(valBytes, i * elemSize, c);
					i++;
				});
				var packet = PacketBuilder.BuildSyncSegments(0, archId, 0, 0, (ushort)n, segMeta, (segIndex, dst) =>
				{
					if (segIndex == 0) new ReadOnlySpan<byte>(idsBytes, 0, idsLen).CopyTo(dst);
					else new ReadOnlySpan<byte>(valBytes, 0, valsLen).CopyTo(dst);
				});
				SyncRelayService.SendUp(packet);
				try { System.Buffers.ArrayPool<byte>.Shared.Return(idsBytes); } catch { }
				try { System.Buffers.ArrayPool<byte>.Shared.Return(valBytes); } catch { }
			}
		}

		private static unsafe void WriteUnaligned<T>(byte[] dst, int offset, in T value) where T : struct
		{
			fixed (byte* pDst = &dst[offset])
			{
				System.Runtime.CompilerServices.Unsafe.WriteUnaligned(pDst, value);
			}
		}
	}
}