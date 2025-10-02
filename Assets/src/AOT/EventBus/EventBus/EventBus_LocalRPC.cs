using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;

namespace Events
{
    public partial class EventBus
    {
        private static class ActorRPCHandlers<Actor, Request, Response>
        {
            public static readonly Dictionary<Actor, Dictionary<Type, UniTask<Response>>> dicActorRPCHandlers = new Dictionary<Actor, Dictionary<Type, UniTask<Response>>>();
        }

        private static class ActorMessageHandlers<Actor, Message>
        {
            public static readonly Dictionary<Actor, Dictionary<Type, UniTask<Message>>> dicActorMessageHandlers = new Dictionary<Actor, Dictionary<Type, UniTask<Message>>>();
        }

        private static class RPCHandlers<Request, Respone>
        {
            public static readonly Dictionary<Type, Dictionary<Type, UniTask<Respone>>>
                CompletionSources = new Dictionary<Type, Dictionary<Type, UniTask<Respone>>>();
        }

        private static class MessageHandlers<Message>
        {
            public static readonly Dictionary<Type, UniTaskCompletionSource<Message>>
                CompletionSources = new Dictionary<Type, UniTaskCompletionSource<Message>>();
        }

        public static UniTask<T> Call<T>()
        {
            var utcs = new UniTaskCompletionSource<T>();
            MessageHandlers<T>.CompletionSources[typeof(T)] = utcs;
            return utcs.Task;
        }

        public static void CallBack<T>(T message)
        {
            if (MessageHandlers<T>.CompletionSources.TryGetValue(typeof(T), out var utcs))
            {
                utcs.TrySetResult(message);
                MessageHandlers<T>.CompletionSources.Remove(typeof(T));
            }
        }
    }
}