using Cysharp.Threading.Tasks;

namespace Events
{
	public interface IEvent
	{
		public void Handle();
		public void Release();
	}

	public abstract class Event<T> : IEvent where T : struct
	{
		public void Handle()
		{
			EventBus.Subscribe<T>(Run);
		}
		public void Release()
		{
			EventBus.Unsubscribe<T>(Run);
		}
		public abstract void Run(T value);
	}

	public abstract class AsyncEvent<T> : IEvent where T : struct
	{
		public void Handle()
		{
			EventBus.SubscribeAsync<T>(Run);
		}
		public void Release()
		{
			EventBus.UnsubscribeAsync<T>(Run);
		}

		public abstract UniTask Run(T value);
	}
}