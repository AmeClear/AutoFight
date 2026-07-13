using System;

namespace GameEvent
{
    /// <summary>
    /// 事件订阅句柄，Dispose 时自动取消订阅。
    /// </summary>
    public sealed class EventSubscription : IDisposable
    {
        private Action _unsubscribe;

        internal EventSubscription(Action unsubscribe)
        {
            _unsubscribe = unsubscribe;
        }

        public void Dispose()
        {
            _unsubscribe?.Invoke();
            _unsubscribe = null;
        }
    }
}
