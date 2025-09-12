using System.Collections.Generic;

namespace Events
{
    public interface IAEvent
    {
        public void Handle();
    }

    public abstract class AEvent<T> : IAEvent where T : struct
    {
        public void Handle()
        {
            Queue<T> queue = MessagePipe.Instance.OutPipe<T>();
            if (queue == null)
            {
                throw new System.Exception("MessagePipe is not initialized.");
            }
            while (queue.Count > 0)
            {
                Run(queue.Dequeue());
            }
        }

        public abstract void Run(T value);
    }
}