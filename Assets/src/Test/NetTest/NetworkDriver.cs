using Arch;
using UnityEngine;

namespace Arch.Net
{
	/// <summary>
	/// Optional scene bootstrapper to spawn echo server and seed NetworkRuntime.
	/// </summary>
	public sealed class NetworkDriver : MonoBehaviour
	{
		[SerializeField] private string m_szEndpoint = "loopback://local"; // or "lite://local" / "lite://127.0.0.1:9050"

		private void Awake()
		{
			if (m_szEndpoint == "lite://local")
			{
				var go = new GameObject("LiteNetLibEchoServer");
				go.AddComponent<LiteNetLibEchoServer>();
				m_szEndpoint = "lite://127.0.0.1:9050";
			}

			// Seed the unique NetworkRuntime via singleton to trigger Awake system
			Unique.Component<NetworkRuntime>.Set(new NetworkRuntime { Endpoint = m_szEndpoint });
		}
	}
}