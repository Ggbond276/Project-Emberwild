using System;
using System.Collections.Generic;

namespace Emberwild.Core.Events
{
    /// <summary>
    /// 进程内事件总线(静态)。
    /// 用法:
    ///   EventBus.Subscribe<PlayerAttackedEvent>(OnAttacked);
    ///   EventBus.Publish(new PlayerAttackedEvent(...));
    ///   EventBus.Unsubscribe<PlayerAttackedEvent>(OnAttacked);
    /// 事件 struct 必须实现 IEvent。
    /// 线程不安全,仅在主线程使用。
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> _handlers = new Dictionary<Type, Delegate>();

        public static void Subscribe<T>(Action<T> handler) where T : struct, IEvent
        {
            if (handler == null) return;
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var existing))
            {
                _handlers[type] = Delegate.Combine(existing, handler);
            }
            else
            {
                _handlers[type] = handler;
            }
        }

        public static void Unsubscribe<T>(Action<T> handler) where T : struct, IEvent
        {
            if (handler == null) return;
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var existing))
            {
                var combined = Delegate.Remove(existing, handler);
                if (combined == null) _handlers.Remove(type);
                else _handlers[type] = combined;
            }
        }

        public static void Publish<T>(T evt) where T : struct, IEvent
        {
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var existing))
            {
                ((Action<T>)existing).Invoke(evt);
            }
        }

        /// <summary>清空所有订阅(场景切换时调用,防止悬挂引用)。</summary>
        public static void Clear()
        {
            _handlers.Clear();
        }
    }
}
