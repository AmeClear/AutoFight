using System.Collections.Generic;
using UnityEngine;

namespace ABSystem
{
    /// <summary>
    /// 运行时 Catalog 查询表。
    /// <para>将 <see cref="ABCatalogData"/> 展开为 address / bundle / group 索引，供管理器与加载器查询。</para>
    /// </summary>
    public class ABCatalog
    {
        private readonly Dictionary<string, ABAssetEntry> _addressMap = new Dictionary<string, ABAssetEntry>();
        private readonly Dictionary<string, ABBundleEntry> _bundleMap = new Dictionary<string, ABBundleEntry>();
        private readonly Dictionary<string, List<ABAssetEntry>> _groupMap = new Dictionary<string, List<ABAssetEntry>>();

        /// <summary>
        /// Catalog 版本号。
        /// </summary>
        public string Version { get; private set; } = "0.0.0";

        /// <summary>
        /// 构建目标平台。
        /// </summary>
        public string BuildTarget { get; private set; }

        /// <summary>
        /// Catalog 是否已成功加载并可用于查询。
        /// </summary>
        public bool IsReady { get; private set; }

        /// <summary>
        /// 逻辑地址 → 资源条目 映射（只读）。
        /// </summary>
        public IReadOnlyDictionary<string, ABAssetEntry> AddressMap => _addressMap;

        /// <summary>
        /// Bundle 名 → Bundle 条目 映射（只读）。
        /// </summary>
        public IReadOnlyDictionary<string, ABBundleEntry> BundleMap => _bundleMap;

        /// <summary>
        /// 从 Catalog 数据构建查询索引。
        /// </summary>
        /// <param name="data">构建管线生成的 Catalog 数据；为 null 时记录错误并保持未就绪。</param>
        public void Load(ABCatalogData data)
        {
            Clear();
            if (data == null)
            {
                Debug.LogError("[ABCatalog] Catalog 数据为空。");
                return;
            }

            Version = data.version;
            BuildTarget = data.buildTarget;

            if (data.bundles != null)
            {
                foreach (var bundle in data.bundles)
                {
                    if (bundle == null || string.IsNullOrEmpty(bundle.bundleName))
                        continue;
                    _bundleMap[bundle.bundleName] = bundle;
                }
            }

            if (data.assets != null)
            {
                foreach (var asset in data.assets)
                {
                    if (asset == null || string.IsNullOrEmpty(asset.address))
                        continue;

                    _addressMap[asset.address] = asset;

                    var group = string.IsNullOrEmpty(asset.group) ? ABDefine.DefaultGroup : asset.group;
                    if (!_groupMap.TryGetValue(group, out var list))
                    {
                        list = new List<ABAssetEntry>();
                        _groupMap[group] = list;
                    }

                    list.Add(asset);
                }
            }

            IsReady = true;
            Debug.Log($"[ABCatalog] 加载完成，资源 {_addressMap.Count}，Bundle {_bundleMap.Count}，版本 {Version}");
        }

        /// <summary>
        /// 清空全部索引并重置就绪状态。
        /// </summary>
        public void Clear()
        {
            _addressMap.Clear();
            _bundleMap.Clear();
            _groupMap.Clear();
            Version = "0.0.0";
            BuildTarget = null;
            IsReady = false;
        }

        /// <summary>
        /// 按逻辑地址查找资源条目。
        /// </summary>
        /// <param name="address">逻辑地址。</param>
        /// <param name="entry">查找到的条目；失败时为 null。</param>
        /// <returns>是否找到。</returns>
        public bool TryGetAsset(string address, out ABAssetEntry entry)
        {
            return _addressMap.TryGetValue(address, out entry);
        }

        /// <summary>
        /// 按 Bundle 名查找 Bundle 条目。
        /// </summary>
        /// <param name="bundleName">Bundle 名（不含扩展名）。</param>
        /// <param name="entry">查找到的条目；失败时为 null。</param>
        /// <returns>是否找到。</returns>
        public bool TryGetBundle(string bundleName, out ABBundleEntry entry)
        {
            return _bundleMap.TryGetValue(bundleName, out entry);
        }

        /// <summary>
        /// 获取指定分组下的全部资源条目。
        /// </summary>
        /// <param name="group">分组名。</param>
        /// <returns>分组资源列表；分组不存在时返回空数组。</returns>
        public IReadOnlyList<ABAssetEntry> GetGroupAssets(string group)
        {
            if (_groupMap.TryGetValue(group, out var list))
                return list;
            return System.Array.Empty<ABAssetEntry>();
        }

        /// <summary>
        /// 获取指定 Bundle 的依赖列表。
        /// </summary>
        /// <param name="bundleName">Bundle 名。</param>
        /// <returns>依赖 Bundle 名数组；无依赖时返回空数组。</returns>
        public string[] GetBundleDependencies(string bundleName)
        {
            if (_bundleMap.TryGetValue(bundleName, out var entry) && entry.dependencies != null)
                return entry.dependencies;
            return System.Array.Empty<string>();
        }
    }
}
