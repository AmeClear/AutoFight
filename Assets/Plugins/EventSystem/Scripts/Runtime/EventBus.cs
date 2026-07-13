using System;
using System.Collections.Generic;

namespace GameEvent
{
    /// <summary>
    /// 全局类型安全事件总线，用于模块间解耦通信。
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> Handlers = new Dictionary<Type, Delegate>();

        /// <summary>
        /// 订阅指定类型事件。
        /// </summary>
        public static EventSubscription Subscribe<T>(Action<T> handler) where T : IGameEvent
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var eventType = typeof(T);
            if (Handlers.TryGetValue(eventType, out var existing))
                Handlers[eventType] = Delegate.Combine(existing, handler);
            else
                Handlers[eventType] = handler;

            return new EventSubscription(() => Unsubscribe(handler));
        }

        /// <summary>
        /// 取消订阅指定类型事件。
        /// </summary>
        public static void Unsubscribe<T>(Action<T> handler) where T : IGameEvent
        {
            if (handler == null)
                return;

            var eventType = typeof(T);
            if (!Handlers.TryGetValue(eventType, out var existing))
                return;

            var updated = Delegate.Remove(existing, handler);
            if (updated == null)
                Handlers.Remove(eventType);
            else
                Handlers[eventType] = updated;
        }

        /// <summary>
        /// 发布事件，按订阅顺序同步调用所有监听者。
        /// </summary>
        public static void Publish<T>(T gameEvent) where T : IGameEvent
        {
            if (gameEvent == null)
                throw new ArgumentNullException(nameof(gameEvent));

            if (!Handlers.TryGetValue(typeof(T), out var handlers) || handlers == null)
                return;

            foreach (Action<T> handler in handlers.GetInvocationList())
                handler.Invoke(gameEvent);
        }

        /// <summary>
        /// 清除指定类型或全部事件订阅。
        /// </summary>
        public static void Clear<T>() where T : IGameEvent
        {
            Handlers.Remove(typeof(T));
        }

        public static void ClearAll()
        {
            Handlers.Clear();
        }

        /// <summary>
        /// 当前是否存在指定类型事件的订阅者。
        /// </summary>
        public static bool HasSubscriber<T>() where T : IGameEvent
        {
            return Handlers.ContainsKey(typeof(T));
        }
    }
}
