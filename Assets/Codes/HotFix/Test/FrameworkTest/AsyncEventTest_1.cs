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
            EventBus.Publish<AEventTest_1Event>(new AEventTest_1Event() { num = 10 });
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

    public class AEventTest_2Listener : Event<AEventTest_1Event>
    {
        public override async void Run(AEventTest_1Event value)
        {
            Arch.Tools.ArchLog.Debug("AEventTest_1Listener: " + value.num);
            await UniTask.CompletedTask;
        }
    }
}