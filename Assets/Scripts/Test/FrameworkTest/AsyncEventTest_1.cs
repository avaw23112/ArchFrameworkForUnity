using Attributes;
using Cysharp.Threading.Tasks;
using Events;
using UnityEngine;

namespace Assets.Scripts.Test
{
	public class AEventTest_2 : MonoBehaviour
	{
		// Use this for initialization
		private void Start()
		{
			EventBus.RegisterEvents();
			EventBus.PublishAsync<AEventTest_1Event>(new AEventTest_1Event() { num = 10 });
		}

		// Update is called once per frame
		private void Update()
		{
		}
	}

	public struct AEventTest_2Event
	{
		public int num;
	}

	public class AEventTest_2Listener : AsyncEvent<AEventTest_1Event>
	{
		public override async UniTask Run(AEventTest_1Event value)
		{
			Tools.Logger.Debug("AEventTest_1Listener: " + value.num);
			await UniTask.CompletedTask;
		}
	}
}