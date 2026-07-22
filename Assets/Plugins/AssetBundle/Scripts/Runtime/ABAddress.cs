using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ABSystem
{
    /// <summary>
    /// <see cref="ABAddress"/> 的运行时扩展：加载入口与按文件名反查 Address。
    /// </summary>
    public static partial class ABAddress
    {
        private static Dictionary<string, string> _byAddress;
        private static Dictionary<string, ABAddressInfo> _byAssetPath;
        private static Dictionary<string, ABAddressInfo> _byFileName;
        private static Dictionary<string, ABAddressInfo> _byFileNameWithoutExtension;
        private static Dictionary<string, ABAddressInfo> _byCodeName;
        private static HashSet<string> _ambiguousFileNames;
        private static HashSet<string> _ambiguousFileNamesWithoutExtension;
        private static bool _lookupReady;

        /// <summary>
        /// 使用地址常量异步加载资源，并返回可 Dispose 的句柄。
        /// </summary>
        public static UniTask<ABHandle<T>> LoadAsync<T>(string address) where T : UnityEngine.Object
        {
            return ABLoader.Instance.LoadAsync<T>(address);
        }

        /// <summary>
        /// 使用地址常量异步加载资源。
        /// </summary>
        public static UniTask<T> LoadAssetAsync<T>(string address) where T : UnityEngine.Object
        {
            return ABLoader.Instance.LoadAssetAsync<T>(address);
        }

        /// <summary>
        /// 使用地址常量异步实例化 Prefab。
        /// </summary>
        public static UniTask<GameObject> InstantiateAsync(string address, Transform parent = null)
        {
            return ABLoader.Instance.InstantiateAsync(address, parent);
        }

        /// <summary>
        /// 释放指定地址的一次引用。
        /// </summary>
        public static void Release(string address)
        {
            ABLoader.Instance.Release(address);
        }

        /// <summary>
        /// 地址是否已在常量库中登记。
        /// </summary>
        public static bool Contains(string address)
        {
            EnsureLookup();
            return !string.IsNullOrEmpty(address) && _byAddress.ContainsKey(address);
        }

        /// <summary>
        /// 通过文件名/资源路径/CodeName 获取 Address。
        /// <para>
        /// 支持：<c>BarUI</c>、<c>BarUI.prefab</c>、完整资产路径、CodeName。
        /// 同名冲突时返回 null（请改用完整路径）。
        /// </para>
        /// </summary>
        /// <param name="fileNameOrPath">文件名、资产路径或 CodeName。</param>
        /// <returns>对应 Address；未找到或冲突时返回 null。</returns>
        public static string GetByFileName(string fileNameOrPath)
        {
            return TryGetByFileName(fileNameOrPath, out var address) ? address : null;
        }

        /// <summary>
        /// 尝试通过文件名/资源路径/CodeName 获取 Address。
        /// </summary>
        /// <param name="fileNameOrPath">文件名、资产路径或 CodeName。</param>
        /// <param name="address">查到的 Address。</param>
        /// <returns>是否唯一匹配成功。</returns>
        public static bool TryGetByFileName(string fileNameOrPath, out string address)
        {
            address = null;
            if (string.IsNullOrEmpty(fileNameOrPath))
                return false;

            EnsureLookup();

            var key = NormalizeKey(fileNameOrPath);

            // 1) 完整资产路径
            if (_byAssetPath.TryGetValue(key, out var byPath))
            {
                address = byPath.Address;
                return true;
            }

            // 2) 已是 Address 本身
            if (_byAddress.ContainsKey(key))
            {
                address = key;
                return true;
            }

            // 3) 带扩展名文件名
            var fileName = Path.GetFileName(key);
            if (!string.IsNullOrEmpty(fileName) &&
                _byFileName.TryGetValue(fileName, out var byFileName))
            {
                if (_ambiguousFileNames.Contains(fileName))
                {
                    Debug.LogWarning($"[ABAddress] 文件名冲突: {fileName}，请改用完整资产路径查询。");
                    return false;
                }

                address = byFileName.Address;
                return true;
            }

            // 4) 不带扩展名 / CodeName
            var nameWithoutExt = Path.GetFileNameWithoutExtension(key);
            if (string.IsNullOrEmpty(nameWithoutExt))
                nameWithoutExt = key;

            if (_byCodeName.TryGetValue(nameWithoutExt, out var byCodeName))
            {
                address = byCodeName.Address;
                return true;
            }

            if (_byFileNameWithoutExtension.TryGetValue(nameWithoutExt, out var byName))
            {
                if (_ambiguousFileNamesWithoutExtension.Contains(nameWithoutExt))
                {
                    Debug.LogWarning($"[ABAddress] 文件名冲突: {nameWithoutExt}，请改用完整资产路径或带扩展名文件名查询。");
                    return false;
                }

                address = byName.Address;
                return true;
            }

            return false;
        }

        /// <summary>
        /// 尝试通过文件名获取完整登记信息。
        /// </summary>
        public static bool TryGetInfoByFileName(string fileNameOrPath, out ABAddressInfo info)
        {
            info = default;
            if (!TryGetByFileName(fileNameOrPath, out var address))
                return false;

            EnsureLookup();
            for (var i = 0; i < Infos.Length; i++)
            {
                if (Infos[i].Address != address)
                    continue;
                info = Infos[i];
                return true;
            }

            return false;
        }

        /// <summary>
        /// 通过文件名解析 Address 后直接异步加载。
        /// </summary>
        public static async UniTask<T> LoadAssetByFileNameAsync<T>(string fileNameOrPath) where T : UnityEngine.Object
        {
            if (!TryGetByFileName(fileNameOrPath, out var address))
            {
                Debug.LogError($"[ABAddress] 未找到文件对应 Address: {fileNameOrPath}");
                return null;
            }

            return await LoadAssetAsync<T>(address);
        }

        private static void EnsureLookup()
        {
            if (_lookupReady)
                return;

            _byAddress = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _byAssetPath = new Dictionary<string, ABAddressInfo>(StringComparer.OrdinalIgnoreCase);
            _byFileName = new Dictionary<string, ABAddressInfo>(StringComparer.OrdinalIgnoreCase);
            _byFileNameWithoutExtension = new Dictionary<string, ABAddressInfo>(StringComparer.OrdinalIgnoreCase);
            _byCodeName = new Dictionary<string, ABAddressInfo>(StringComparer.OrdinalIgnoreCase);
            _ambiguousFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _ambiguousFileNamesWithoutExtension = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var info in Infos)
            {
                if (!string.IsNullOrEmpty(info.Address))
                    _byAddress[info.Address] = info.Address;

                if (!string.IsNullOrEmpty(info.AssetPath))
                    _byAssetPath[NormalizeKey(info.AssetPath)] = info;

                if (!string.IsNullOrEmpty(info.CodeName))
                    _byCodeName[info.CodeName] = info;

                if (!string.IsNullOrEmpty(info.FileName))
                {
                    if (_byFileName.ContainsKey(info.FileName))
                        _ambiguousFileNames.Add(info.FileName);
                    else
                        _byFileName[info.FileName] = info;
                }

                if (!string.IsNullOrEmpty(info.FileNameWithoutExtension))
                {
                    if (_byFileNameWithoutExtension.ContainsKey(info.FileNameWithoutExtension))
                        _ambiguousFileNamesWithoutExtension.Add(info.FileNameWithoutExtension);
                    else
                        _byFileNameWithoutExtension[info.FileNameWithoutExtension] = info;
                }
            }

            _lookupReady = true;
        }

        private static string NormalizeKey(string value)
        {
            return value.Replace('\\', '/').Trim();
        }
    }
}
