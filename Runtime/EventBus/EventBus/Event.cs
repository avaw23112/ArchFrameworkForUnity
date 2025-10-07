
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

	/// <summary>
	/// AOT事件
	/// </summary>
	public struct GameStartEvent
	{
	}
}