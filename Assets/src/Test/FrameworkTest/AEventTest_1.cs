using Events;
using UnityEngine;

namespace Assets.Scripts.Test
{
	public class ArchSystem_Test : MonoBehaviour
	{
		// Use this for initialization
		private void Start()
		{
			EventBus.RegisterEvents();
			EventBus.Publish<AEventTest_1Event>(new AEventTest_1Event() { num = 10 });
		}

		// Update is called once per frame
		private void Update()
		{
		}
	}

	public struct AEventTest_1Event
	{
		public int num;
	}

	public class AEventTest_1Listener : Event<AEventTest_1Event>
	{
		public override void Run(AEventTest_1Event value)
		{
			Arch.Tools.ArchLog.LogDebug("AEventTest_1Listener: " + value.num);
		}
	}
}