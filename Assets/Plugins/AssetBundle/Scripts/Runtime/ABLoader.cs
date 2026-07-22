using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ABSystem
{
    /// <summary>
    /// AB 系统高层资源加载入口（推荐业务侧使用）。
    /// <para>
    /// 按逻辑地址加载资源，内置缓存与引用计数；
    /// 可通过 <see cref="ABHandle{T}"/> 或 <see cref="Release"/> 释放。
    /// </para>
    /// </summary>
    public class ABLoader
    {
        private class AssetCache
        {
            public UnityEngine.Object Asset;
            public string BundleName;
            public int RefCount;
        }

        private static ABLoader _instance;

        /// <summary>
        /// 全局单例。
        /// </summary>
        public static ABLoader Instance => _instance ??= new ABLoader();

        private readonly Dictionary<string, AssetCache> _cache = new Dictionary<string, AssetCache>();
        private readonly Dictionary<string, UniTask<UnityEngine.Object>> _loadingTasks = new Dictionary<string, UniTask<UnityEngine.Object>>();

        /// <summary>
        /// 底层 Bundle 管理器。
        /// </summary>
        public ABManager Manager => ABManager.Instance;

        /// <summary>
        /// AB 系统是否已初始化完成，可开始按 Catalog 加载。
        /// </summary>
        public bool IsReady => Manager.IsInitialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            _instance?._cache.Clear();
            _instance?._loadingTasks.Clear();
            _instance = null;
        }

        /// <summary>
        /// 初始化 AB 系统（转发至 <see cref="ABManager.InitializeAsync"/>）。
        /// </summary>
        /// <param name="bundleRootPath">自定义 Bundle 根目录；为 null 时使用默认路径。</param>
        public UniTask InitializeAsync(string bundleRootPath = null)
        {
            return Manager.InitializeAsync(bundleRootPath);
        }

        /// <summary>
        /// 异步加载资源，并返回可 Dispose 的句柄。
        /// <para>推荐配合 <c>using</c> / <c>Dispose</c> 管理生命周期。</para>
        /// </summary>
        /// <typeparam name="T">资源类型。</typeparam>
        /// <param name="address">逻辑地址（与标记时填写的 address 一致）。</param>
        /// <returns>资源句柄；加载失败返回 null。</returns>
        public async UniTask<ABHandle<T>> LoadAsync<T>(string address) where T : UnityEngine.Object
        {
            var asset = await LoadAssetAsync<T>(address);
            if (asset == null)
                return null;

            return new ABHandle<T>(address, asset, () => Release(address));
        }

        /// <summary>
        /// 异步加载资源（不返回句柄）。
        /// <para>调用方需在不再使用时主动调用 <see cref="Release"/>，否则引用计数不会下降。</para>
        /// </summary>
        /// <typeparam name="T">资源类型。</typeparam>
        /// <param name="address">逻辑地址。</param>
        /// <returns>资源实例；失败返回 null。</returns>
        public async UniTask<T> LoadAssetAsync<T>(string address) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(address))
                return null;

            if (!Manager.IsInitialized)
                await Manager.InitializeAsync();

            if (_cache.TryGetValue(address, out var cache))
            {
                cache.RefCount++;
                return cache.Asset as T;
            }

            if (_loadingTasks.TryGetValue(address, out var existingTask))
            {
                var loadingAsset = await existingTask;
                if (_cache.TryGetValue(address, out cache))
                    cache.RefCount++;
                return loadingAsset as T;
            }

            var task = LoadAssetInternalAsync(address);
            _loadingTasks[address] = task;

            try
            {
                var asset = await task;
                return asset as T;
            }
            finally
            {
                _loadingTasks.Remove(address);
            }
        }

        /// <summary>
        /// 同步加载资源。
        /// <para>
        /// 仅建议在 Editor 模拟模式，或目标 Bundle 已加载时使用；
        /// 正式异步流程请优先使用 <see cref="LoadAssetAsync{T}"/>。
        /// </para>
        /// </summary>
        /// <typeparam name="T">资源类型。</typeparam>
        /// <param name="address">逻辑地址。</param>
        /// <returns>资源实例；失败返回 null。</returns>
        public T LoadAssetSync<T>(string address) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(address))
                return null;

//#if UNITY_EDITOR
//            if (!Manager.IsInitialized || !Manager.Catalog.TryGetAsset(address, out _))
//            {
//                // Editor 模拟：直接按 address 当资产路径加载
//                var editorAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(address);
//                if (editorAsset != null)
//                    return editorAsset;
//            }
//#endif

            if (!Manager.IsInitialized)
            {
                Debug.LogError("[ABLoader] 同步加载前请先初始化 AB 系统。");
                return null;
            }

            if (_cache.TryGetValue(address, out var cache))
            {
                cache.RefCount++;
                return cache.Asset as T;
            }

            if (!Manager.Catalog.TryGetAsset(address, out var entry))
            {
                Debug.LogError($"[ABLoader] 未找到资源地址: {address}");
                return null;
            }

            var bundle = Manager.LoadBundleAsync(entry.bundleName).GetAwaiter().GetResult();
            if (bundle == null)
                return null;

            var asset = bundle.LoadAsset<T>(entry.assetPath);
            if (asset == null)
            {
                Manager.ReleaseBundle(entry.bundleName);
                Debug.LogError($"[ABLoader] Bundle 内未找到资源: {entry.assetPath}");
                return null;
            }

            _cache[address] = new AssetCache
            {
                Asset = asset,
                BundleName = entry.bundleName,
                RefCount = 1
            };

            return asset;
        }

        /// <summary>
        /// 按地址异步加载 Prefab 并实例化。
        /// <para>注意：该方法会持有 Prefab 的一次加载引用；销毁实例不会自动 Release Prefab。</para>
        /// </summary>
        /// <param name="address">逻辑地址。</param>
        /// <param name="parent">父节点；可为 null。</param>
        /// <returns>实例化后的 GameObject；失败返回 null。</returns>
        public async UniTask<GameObject> InstantiateAsync(string address, Transform parent = null)
        {
            var prefab = await LoadAssetAsync<GameObject>(address);
            if (prefab == null)
                return null;

            var instance = UnityEngine.Object.Instantiate(prefab, parent);
            instance.name = prefab.name;
            return instance;
        }

        /// <summary>
        /// 预加载指定分组内的全部资源到缓存。
        /// </summary>
        /// <param name="group">分组名，对应标记时的 group。</param>
        public async UniTask PreloadGroupAsync(string group)
        {
            if (!Manager.IsInitialized)
                await Manager.InitializeAsync();

            var assets = Manager.Catalog.GetGroupAssets(group);
            var tasks = new List<UniTask>(assets.Count);
            foreach (var asset in assets)
                tasks.Add(LoadAssetAsync<UnityEngine.Object>(asset.address));

            await UniTask.WhenAll(tasks);
        }

        /// <summary>
        /// 释放指定地址的一次资源引用。
        /// <para>引用归零后从缓存移除，并释放对应 Bundle 的一次引用。</para>
        /// </summary>
        /// <param name="address">逻辑地址。</param>
        public void Release(string address)
        {
            if (!_cache.TryGetValue(address, out var cache))
                return;

            cache.RefCount = Mathf.Max(0, cache.RefCount - 1);
            if (cache.RefCount > 0)
                return;

            _cache.Remove(address);
            Manager.ReleaseBundle(cache.BundleName);
        }

        /// <summary>
        /// 释放全部已缓存资源对应的 Bundle 引用，并清空资源缓存。
        /// </summary>
        public void ReleaseAll()
        {
            foreach (var pair in _cache)
                Manager.ReleaseBundle(pair.Value.BundleName);

            _cache.Clear();
        }

        /// <summary>
        /// 查询 Catalog 中是否包含指定逻辑地址。
        /// </summary>
        /// <param name="address">逻辑地址。</param>
        /// <returns>存在返回 true。</returns>
        public bool Contains(string address)
        {
            return Manager.Catalog.TryGetAsset(address, out _);
        }

        private async UniTask<UnityEngine.Object> LoadAssetInternalAsync(string address)
        {
#if UNITY_EDITOR
            if (TryLoadFromEditorDatabase(address, out var editorAsset))
                return editorAsset;
#endif

            if (!Manager.Catalog.TryGetAsset(address, out var entry))
            {
                Debug.LogError($"[ABLoader] 未找到资源地址: {address}");
                return null;
            }

            var bundle = await Manager.LoadBundleAsync(entry.bundleName);
            if (bundle == null)
                return null;

            var request = bundle.LoadAssetAsync(entry.assetPath);
            await request.ToUniTask();

            var asset = request.asset;
            if (asset == null)
            {
                Manager.ReleaseBundle(entry.bundleName);
                Debug.LogError($"[ABLoader] Bundle 内未找到资源: {entry.assetPath}");
                return null;
            }

            _cache[address] = new AssetCache
            {
                Asset = asset,
                BundleName = entry.bundleName,
                RefCount = 1
            };

            return asset;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor 下尝试通过 AssetDatabase 模拟加载。
        /// </summary>
        private bool TryLoadFromEditorDatabase(string address, out UnityEngine.Object asset)
        {
            asset = null;

            string assetPath = null;
            if (Manager.Catalog.TryGetAsset(address, out var entry))
            {
                assetPath = entry.assetPath;

                // 已有真实 Bundle 且关闭模拟时，走正式加载
                if (!Manager.EditorSimulateMode)
                    return false;

                var bundlePath = System.IO.Path.Combine(
                    Manager.BundleRootPath,
                    entry.bundleName + ABDefine.BundleExtension);
                if (System.IO.File.Exists(bundlePath) && Application.isPlaying)
                    return false;
            }
            else
            {
                assetPath = address;
            }

            asset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (asset == null)
                return false;

            _cache[address] = new AssetCache
            {
                Asset = asset,
                BundleName = string.Empty,
                RefCount = 1
            };
            return true;
        }
#endif
    }
}
