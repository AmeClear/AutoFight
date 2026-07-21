using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ABSystem
{
    /// <summary>
    /// AssetBundle 底层管理器。
    /// <para>
    /// 负责 Catalog 初始化、Bundle 文件加载/卸载、依赖解析与引用计数。
    /// 业务侧通常优先使用 <see cref="ABLoader"/>，仅在需要直接操作 Bundle 时调用本类。
    /// </para>
    /// </summary>
    public class ABManager
    {
        private class LoadedBundle
        {
            public AssetBundle Bundle;
            public int RefCount;
            public string BundleName;
        }

        private static ABManager _instance;

        /// <summary>
        /// 全局单例。
        /// </summary>
        public static ABManager Instance => _instance ??= new ABManager();

        private readonly Dictionary<string, LoadedBundle> _loadedBundles = new Dictionary<string, LoadedBundle>();
        private readonly Dictionary<string, UniTask<AssetBundle>> _loadingTasks = new Dictionary<string, UniTask<AssetBundle>>();

        /// <summary>
        /// 当前已加载的 Catalog 查询表。
        /// </summary>
        public ABCatalog Catalog { get; } = new ABCatalog();

        /// <summary>
        /// 是否已完成初始化（Catalog 就绪）。
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Bundle 文件根目录。默认为 StreamingAssets/AssetBundles，Editor 下可回退到构建输出目录。
        /// </summary>
        public string BundleRootPath { get; private set; }

        /// <summary>
        /// Editor 模拟开关。
        /// <para>为 true 且本地 Bundle 文件不存在时，由 <see cref="ABLoader"/> 走 AssetDatabase 加载。</para>
        /// </summary>
        public bool EditorSimulateMode { get; set; } = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            if (_instance != null)
            {
                _instance.UnloadAll(true);
                _instance = null;
            }
        }

        /// <summary>
        /// 异步初始化：读取 Catalog 并设置 Bundle 根目录。
        /// <para>查找顺序：Resources → StreamingAssets →（仅 Editor）本地构建输出目录。</para>
        /// </summary>
        /// <param name="bundleRootPath">自定义 Bundle 根目录；为 null 时使用默认路径。</param>
        public async UniTask InitializeAsync(string bundleRootPath = null)
        {
            if (IsInitialized)
                return;

            BundleRootPath = string.IsNullOrEmpty(bundleRootPath)
                ? GetDefaultBundleRoot()
                : bundleRootPath;

            var catalogData = await LoadCatalogDataAsync();
            if (catalogData == null)
            {
                Debug.LogError("[ABManager] Catalog 加载失败，AB 系统未初始化。");
                return;
            }

            Catalog.Load(catalogData);
            IsInitialized = Catalog.IsReady;
        }

        /// <summary>
        /// 同步初始化，直接注入已有 Catalog 数据。
        /// <para>适用于单元测试、自定义热更下载完成后的本地注入等场景。</para>
        /// </summary>
        /// <param name="catalogData">Catalog 数据。</param>
        /// <param name="bundleRootPath">自定义 Bundle 根目录；为 null 时使用默认路径。</param>
        public void Initialize(ABCatalogData catalogData, string bundleRootPath = null)
        {
            BundleRootPath = string.IsNullOrEmpty(bundleRootPath)
                ? GetDefaultBundleRoot()
                : bundleRootPath;

            Catalog.Load(catalogData);
            IsInitialized = Catalog.IsReady;
        }

        /// <summary>
        /// 异步加载指定 Bundle（含依赖），并增加一次引用计数。
        /// </summary>
        /// <param name="bundleName">Bundle 名（不含扩展名）。</param>
        /// <returns>已加载的 AssetBundle；失败时返回 null。</returns>
        /// <exception cref="InvalidOperationException">未初始化时抛出。</exception>
        public async UniTask<AssetBundle> LoadBundleAsync(string bundleName)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(bundleName))
                return null;

            if (_loadedBundles.TryGetValue(bundleName, out var loaded))
            {
                loaded.RefCount++;
                return loaded.Bundle;
            }

            if (_loadingTasks.TryGetValue(bundleName, out var existingTask))
            {
                var bundle = await existingTask;
                if (_loadedBundles.TryGetValue(bundleName, out loaded))
                    loaded.RefCount++;
                return bundle;
            }

            var task = LoadBundleInternalAsync(bundleName);
            _loadingTasks[bundleName] = task;

            try
            {
                return await task;
            }
            finally
            {
                _loadingTasks.Remove(bundleName);
            }
        }

        /// <summary>
        /// 对已加载 Bundle 增加一次引用计数（不触发加载）。
        /// </summary>
        /// <param name="bundleName">Bundle 名。</param>
        public void RetainBundle(string bundleName)
        {
            if (_loadedBundles.TryGetValue(bundleName, out var loaded))
                loaded.RefCount++;
        }

        /// <summary>
        /// 释放 Bundle 的一次引用。引用归零时卸载自身，并递归释放依赖。
        /// </summary>
        /// <param name="bundleName">Bundle 名。</param>
        /// <param name="unloadAllLoadedObjects">
        /// 是否同时销毁从该 Bundle 加载出的全部对象。
        /// 为 false 时仅卸载 Bundle 壳，已实例化对象仍可继续使用。
        /// </param>
        public void ReleaseBundle(string bundleName, bool unloadAllLoadedObjects = false)
        {
            if (!_loadedBundles.TryGetValue(bundleName, out var loaded))
                return;

            loaded.RefCount = Mathf.Max(0, loaded.RefCount - 1);
            if (loaded.RefCount > 0)
                return;

            if (loaded.Bundle != null)
                loaded.Bundle.Unload(unloadAllLoadedObjects);

            _loadedBundles.Remove(bundleName);

            if (!Catalog.TryGetBundle(bundleName, out var entry) || entry.dependencies == null)
                return;

            foreach (var dependency in entry.dependencies)
                ReleaseBundle(dependency, unloadAllLoadedObjects);
        }

        /// <summary>
        /// 强制卸载全部已加载 Bundle，并清空加载任务缓存。
        /// </summary>
        /// <param name="unloadAllLoadedObjects">是否销毁已加载出的对象。</param>
        public void UnloadAll(bool unloadAllLoadedObjects = true)
        {
            foreach (var pair in _loadedBundles)
            {
                if (pair.Value.Bundle != null)
                    pair.Value.Bundle.Unload(unloadAllLoadedObjects);
            }

            _loadedBundles.Clear();
            _loadingTasks.Clear();
        }

        /// <summary>
        /// 查询指定 Bundle 当前是否处于已加载状态。
        /// </summary>
        /// <param name="bundleName">Bundle 名。</param>
        /// <returns>已加载返回 true。</returns>
        public bool IsBundleLoaded(string bundleName)
        {
            return _loadedBundles.ContainsKey(bundleName);
        }

        /// <summary>
        /// 获取指定 Bundle 的当前引用计数。
        /// </summary>
        /// <param name="bundleName">Bundle 名。</param>
        /// <returns>引用次数；未加载时返回 0。</returns>
        public int GetBundleRefCount(string bundleName)
        {
            return _loadedBundles.TryGetValue(bundleName, out var loaded) ? loaded.RefCount : 0;
        }

        private async UniTask<AssetBundle> LoadBundleInternalAsync(string bundleName)
        {
            var dependencies = Catalog.GetBundleDependencies(bundleName);
            foreach (var dependency in dependencies)
                await LoadBundleAsync(dependency);

            var path = GetBundleFilePath(bundleName);
            if (!File.Exists(path))
            {
                Debug.LogError($"[ABManager] Bundle 文件不存在: {path}");
                return null;
            }

            var request = AssetBundle.LoadFromFileAsync(path);
            await request.ToUniTask();

            var bundle = request.assetBundle;
            if (bundle == null)
            {
                Debug.LogError($"[ABManager] Bundle 加载失败: {bundleName}");
                return null;
            }

            _loadedBundles[bundleName] = new LoadedBundle
            {
                Bundle = bundle,
                RefCount = 1,
                BundleName = bundleName
            };

            return bundle;
        }

        private async UniTask<ABCatalogData> LoadCatalogDataAsync()
        {
            // 优先从 Resources 读取（便于 Editor 模拟与首包内置）
            var resourceCatalog = Resources.Load<TextAsset>(ABDefine.CatalogResourcePath);
            if (resourceCatalog != null)
                return JsonUtility.FromJson<ABCatalogData>(resourceCatalog.text);

            var streamingPath = Path.Combine(Application.streamingAssetsPath, ABDefine.StreamingBundleFolder, ABDefine.CatalogFileName);
            if (File.Exists(streamingPath))
            {
                var json = File.ReadAllText(streamingPath);
                await UniTask.Yield();
                return JsonUtility.FromJson<ABCatalogData>(json);
            }

#if UNITY_EDITOR
            // Editor 下允许从本地构建输出目录读取
            var editorPath = Path.Combine(GetEditorBundleOutputRoot(), ABDefine.CatalogFileName);
            if (File.Exists(editorPath))
            {
                var json = File.ReadAllText(editorPath);
                await UniTask.Yield();
                return JsonUtility.FromJson<ABCatalogData>(json);
            }
#endif

            Debug.LogWarning("[ABManager] 未找到 Catalog，请先执行 AB 构建。");
            return null;
        }

        private string GetBundleFilePath(string bundleName)
        {
            var fileName = bundleName.EndsWith(ABDefine.BundleExtension, StringComparison.OrdinalIgnoreCase)
                ? bundleName
                : bundleName + ABDefine.BundleExtension;

            return Path.Combine(BundleRootPath, fileName);
        }

        private static string GetDefaultBundleRoot()
        {
#if UNITY_EDITOR
            var editorRoot = GetEditorBundleOutputRoot();
            if (Directory.Exists(editorRoot))
                return editorRoot;
#endif
            return Path.Combine(Application.streamingAssetsPath, ABDefine.StreamingBundleFolder);
        }

#if UNITY_EDITOR
        private static string GetEditorBundleOutputRoot()
        {
            return Path.Combine(
                Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath,
                "Bundles",
                UnityEditor.EditorUserBuildSettings.activeBuildTarget.ToString());
        }
#endif

        private void EnsureInitialized()
        {
            if (!IsInitialized)
                throw new InvalidOperationException("[ABManager] 尚未初始化，请先调用 InitializeAsync。");
        }
    }
}
