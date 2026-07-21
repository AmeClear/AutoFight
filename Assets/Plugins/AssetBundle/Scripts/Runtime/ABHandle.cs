using System;
using UnityEngine;

namespace ABSystem
{
    /// <summary>
    /// 已加载资源的引用句柄。
    /// <para>
    /// 通过 <see cref="ABLoader.LoadAsync{T}"/> 获取。
    /// 调用 <see cref="Dispose"/>（或 using）时会减少该地址的引用计数；计数归零后释放对应 Bundle。
    /// </para>
    /// </summary>
    /// <typeparam name="T">资源类型，须为 UnityEngine.Object 子类。</typeparam>
    public sealed class ABHandle<T> : IDisposable where T : UnityEngine.Object
    {
        private Action _release;
        private bool _disposed;

        /// <summary>
        /// 资源逻辑地址。
        /// </summary>
        public string Address { get; }

        /// <summary>
        /// 已加载的资源实例。句柄 Dispose 后仍可能被缓存持有，请勿长期强引用并假设其生命周期。
        /// </summary>
        public T Asset { get; }

        /// <summary>
        /// 创建资源句柄。
        /// </summary>
        /// <param name="address">逻辑地址。</param>
        /// <param name="asset">已加载资源。</param>
        /// <param name="release">释放回调，通常对应 <see cref="ABLoader.Release"/>。</param>
        internal ABHandle(string address, T asset, Action release)
        {
            Address = address;
            Asset = asset;
            _release = release;
        }

        /// <summary>
        /// 释放句柄持有的一次引用。可安全重复调用。
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _release?.Invoke();
            _release = null;
        }
    }
}
