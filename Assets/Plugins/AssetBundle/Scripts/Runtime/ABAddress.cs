using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ABSystem
{
    /// <summary>
    /// <see cref="ABAddress"/> 的运行时扩展：提供基于地址常量的便捷加载入口。
    /// </summary>
    public static partial class ABAddress
    {
        /// <summary>
        /// 使用地址常量异步加载资源，并返回可 Dispose 的句柄。
        /// </summary>
        /// <typeparam name="T">资源类型。</typeparam>
        /// <param name="address">逻辑地址，推荐传入 <c>ABAddress.分组.资源名</c>（分组对应 <see cref="ABGroup"/>）。</param>
        /// <returns>资源句柄；失败返回 null。</returns>
        public static UniTask<ABHandle<T>> LoadAsync<T>(string address) where T : Object
        {
            return ABLoader.Instance.LoadAsync<T>(address);
        }

        /// <summary>
        /// 使用地址常量异步加载资源。
        /// </summary>
        /// <typeparam name="T">资源类型。</typeparam>
        /// <param name="address">逻辑地址，推荐传入 <c>ABAddress.分组.资源名</c>。</param>
        /// <returns>资源实例；失败返回 null。</returns>
        public static UniTask<T> LoadAssetAsync<T>(string address) where T : Object
        {
            return ABLoader.Instance.LoadAssetAsync<T>(address);
        }

        /// <summary>
        /// 使用地址常量异步实例化 Prefab。
        /// </summary>
        /// <param name="address">逻辑地址，推荐传入 <c>ABAddress.分组.资源名</c>。</param>
        /// <param name="parent">父节点；可为 null。</param>
        /// <returns>实例化对象；失败返回 null。</returns>
        public static UniTask<GameObject> InstantiateAsync(string address, Transform parent = null)
        {
            return ABLoader.Instance.InstantiateAsync(address, parent);
        }

        /// <summary>
        /// 释放指定地址的一次引用。
        /// </summary>
        /// <param name="address">逻辑地址。</param>
        public static void Release(string address)
        {
            ABLoader.Instance.Release(address);
        }
    }
}
