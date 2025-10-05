using System.Collections.Generic;

namespace Arch.Net
{
	/// <summary>
	/// Maintains a peer graph and computes a minimum spanning tree (Prim) using FFIM-style costs.
	/// Nodes represent peers (peerId + endpoint). Edges represent measured P2P metrics.
	/// </summary>
	public static class TopologyGraph
	{
		private struct Peer
		{
			public string PeerId;
			public string Endpoint;
			public int Weight;
		}

		private struct Edge
		{
			public string A;
			public string B;
			public int Rtt;
			public float Loss;
			public float Jitter;
		}

		public struct TreeEdge
		{
			public string PeerId;
			public string ParentPeerId;
			public string Endpoint;
			public int Cost;
		}

		private static readonly Dictionary<string, Peer> s_peers = new Dictionary<string, Peer>();
		private static readonly Dictionary<string, Dictionary<string, Edge>> s_adj = new Dictionary<string, Dictionary<string, Edge>>();
		private static readonly Dictionary<string, string> s_peerByEndpoint = new Dictionary<string, string>();
		private static readonly object s_lock = new object();

		public static void RegisterPeer(string peerId, string endpoint, int weight = 0)
		{
			if (string.IsNullOrEmpty(peerId)) return;
			lock (s_lock)
			{
				s_peers[peerId] = new Peer { PeerId = peerId, Endpoint = endpoint, Weight = weight };
				if (!s_adj.ContainsKey(peerId)) s_adj[peerId] = new Dictionary<string, Edge>();
				if (!string.IsNullOrEmpty(endpoint)) s_peerByEndpoint[endpoint] = peerId;
			}
		}

		public static void UnregisterPeer(string peerId)
		{
			if (string.IsNullOrEmpty(peerId)) return;
			lock (s_lock)
			{
				if (s_peers.TryGetValue(peerId, out var peer))
				{
					if (!string.IsNullOrEmpty(peer.Endpoint)) s_peerByEndpoint.Remove(peer.Endpoint);
				}
				s_peers.Remove(peerId);
				if (s_adj.TryGetValue(peerId, out var neighbors))
				{
					foreach (var kv in neighbors)
					{
						if (s_adj.TryGetValue(kv.Key, out var back)) back.Remove(peerId);
					}
				}
				s_adj.Remove(peerId);
			}
		}

		public static void UpdatePeerWeight(string peerId, int weight)
		{
			lock (s_lock)
			{
				if (!s_peers.TryGetValue(peerId, out var peer)) return;
				peer.Weight = weight;
				s_peers[peerId] = peer;
			}
		}

		public static void UpdateEdge(string peerA, string peerB, int rtt, float loss, float jitter)
		{
			if (string.IsNullOrEmpty(peerA) || string.IsNullOrEmpty(peerB) || peerA == peerB) return;
			lock (s_lock)
			{
				if (!s_peers.ContainsKey(peerA) || !s_peers.ContainsKey(peerB)) return;
				var edge = new Edge { A = peerA, B = peerB, Rtt = rtt, Loss = loss, Jitter = jitter };
				if (!s_adj.TryGetValue(peerA, out var neighborsA)) s_adj[peerA] = neighborsA = new Dictionary<string, Edge>();
				neighborsA[peerB] = edge;
				if (!s_adj.TryGetValue(peerB, out var neighborsB)) s_adj[peerB] = neighborsB = new Dictionary<string, Edge>();
				neighborsB[peerA] = edge;
			}
		}

		public static List<TreeEdge> BuildMinimumSpanningTree(string rootPeerId)
		{
			lock (s_lock)
			{
				var result = new List<TreeEdge>();
				if (string.IsNullOrEmpty(rootPeerId) || !s_peers.ContainsKey(rootPeerId)) return result;
				var visited = new HashSet<string>();
				var pq = new MinQueue<(string peer, string parent, int cost)>();
				pq.Enqueue((rootPeerId, null, 0), 0);
				while (pq.TryDequeue(out var item, out _))
				{
					if (visited.Contains(item.peer)) continue;
					visited.Add(item.peer);
					if (item.parent != null)
					{
						var peer = s_peers[item.peer];
						result.Add(new TreeEdge
						{
							PeerId = item.peer,
							ParentPeerId = item.parent,
							Endpoint = peer.Endpoint,
							Cost = item.cost
						});
					}
					if (!s_adj.TryGetValue(item.peer, out var neighbors)) continue;
					foreach (var kv in neighbors)
					{
						var next = kv.Key;
						if (visited.Contains(next)) continue;
						var edge = kv.Value;
						int peerWeight = s_peers.TryGetValue(next, out var peer) ? peer.Weight : 0;
						int score = ComputeCompositeScore(edge, peerWeight);
						pq.Enqueue((next, item.peer, score), score);
					}
				}
				return result;
			}
		}

		public static IReadOnlyList<string> GetPeers()
		{
			lock (s_lock)
			{
				return new List<string>(s_peers.Keys);
			}
		}

		public static bool TryGetEndpoint(string peerId, out string endpoint)
		{
			lock (s_lock)
			{
				if (s_peers.TryGetValue(peerId, out var peer) && !string.IsNullOrEmpty(peer.Endpoint))
				{
					endpoint = peer.Endpoint;
					return true;
				}
				endpoint = null;
				return false;
			}
		}

		public static bool TryGetPeerIdByEndpoint(string endpoint, out string peerId)
		{
			lock (s_lock)
			{
				return s_peerByEndpoint.TryGetValue(endpoint, out peerId);
			}
		}

		public static bool TryGetEdgeScore(string a, string b, out int score)
		{
			lock (s_lock)
			{
				score = int.MaxValue;
				if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
				if (!s_adj.TryGetValue(a, out var n)) return false;
				if (!n.TryGetValue(b, out var e)) return false;
				int weight = 0;
				if (s_peers.TryGetValue(b, out var peer)) weight = peer.Weight;
				score = ComputeCompositeScore(e, weight);
				return true;
			}
		}

		public static bool TryGetBestNeighbor(string peerId, out string neighborPeerId, out int score)
		{
			lock (s_lock)
			{
				neighborPeerId = null; score = int.MaxValue;
				if (string.IsNullOrEmpty(peerId)) return false;
				if (!s_adj.TryGetValue(peerId, out var neighbors)) return false;
				foreach (var kv in neighbors)
				{
					var next = kv.Key;
					var e = kv.Value;
					int w = 0;
					if (s_peers.TryGetValue(next, out var peer)) w = peer.Weight;
					int s = ComputeCompositeScore(e, w);
					if (s < score && s_peers.TryGetValue(next, out var p) && !string.IsNullOrEmpty(p.Endpoint))
					{
						score = s;
						neighborPeerId = next;
					}
				}
				return neighborPeerId != null;
			}
		}

		private static int ComputeCompositeScore(Edge e, int endpointWeight)
		{
			var cfg = Arch.Net.NetworkSettings.Config;
			int rw = cfg?.FfimRttWeight > 0 ? cfg.FfimRttWeight : 1;
			int jw = cfg?.FfimJitterWeight > 0 ? cfg.FfimJitterWeight : 20;
			int lw = cfg?.FfimLossWeight > 0 ? cfg.FfimLossWeight : 2000;
			int wb = cfg?.FfimEndpointWeightBonus > 0 ? cfg.FfimEndpointWeightBonus : 5;
			int score = e.Rtt * rw + (int)(e.Jitter * jw) + (int)(e.Loss * lw) - endpointWeight * wb;
			return score < 0 ? 0 : score;
		}

		// Minimal compatibility queue: linear-time dequeue of minimal priority
		private sealed class MinQueue<T>
		{
			private readonly List<(T item, int priority)> _list = new List<(T, int)>();

			public void Enqueue(T item, int priority) => _list.Add((item, priority));

			public bool TryDequeue(out T item, out int priority)
			{
				if (_list.Count == 0) { item = default; priority = 0; return false; }
				int minIdx = 0; int minPr = _list[0].priority;
				for (int i = 1; i < _list.Count; i++)
				{
					if (_list[i].priority < minPr) { minPr = _list[i].priority; minIdx = i; }
				}
				var tuple = _list[minIdx];
				_list.RemoveAt(minIdx);
				item = tuple.item; priority = tuple.priority; return true;
			}
		}
	}
}