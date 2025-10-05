using Assets.src.AOT.ECS.SystemScheduler;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Arch
{
	public class DefaultSystemScheduler : ISystemScheduler
	{
		private CancellationTokenSource _cts;
		private Task _loop;

		public void Start(Action update, Action lateUpdate)
		{
			_cts = new CancellationTokenSource();
			_loop = Task.Run(async () =>
			{
				var token = _cts.Token;
				var sw = new System.Diagnostics.Stopwatch();
				while (!token.IsCancellationRequested)
				{
					sw.Restart();
					update?.Invoke();
					lateUpdate?.Invoke();
					int delay = Math.Max(1, 16 - (int)sw.ElapsedMilliseconds);
					await Task.Delay(delay, token); // ~60fps
				}
			}, _cts.Token);
		}

		public void Stop()
		{
			_cts?.Cancel();
		}
	}
}