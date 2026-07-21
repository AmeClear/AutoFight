using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ABSystem.Editor
{
    /// <summary>
    /// AB 构建管线。
    /// <para>
    /// 读取标记数据库 → 设置 AssetBundleName → 调用 Unity BuildPipeline →
    /// 生成 Catalog → 输出到 Bundles / Resources / StreamingAssets。
    /// </para>
    /// </summary>
    public static class ABBuilder
    {
        private const string MenuRoot = "AB System/";

        /// <summary>
        /// 构建当前平台全部已标记资源的 AssetBundle，并生成 Catalog。
        /// <para>菜单：<c>AB System/Build AssetBundles</c>。</para>
        /// </summary>
        [MenuItem(MenuRoot + "Build AssetBundles", priority = 100)]
        public static void BuildAll()
        {
            var database = ABMarkDatabaseUtil.LoadOrCreate();
            if (database.marks == null || database.marks.Count == 0)
            {
                EditorUtility.DisplayDialog("AB Build", "标记数据库为空，请先标记资源。", "OK");
                return;
            }

            var buildTarget = EditorUserBuildSettings.activeBuildTarget;
            var outputRoot = GetOutputRoot(buildTarget);
            if (!Directory.Exists(outputRoot))
                Directory.CreateDirectory(outputRoot);

            ClearAssetBundleNames();
            ApplyAssetBundleNames(database);

            var manifest = BuildPipeline.BuildAssetBundles(
                outputRoot,
                BuildAssetBundleOptions.ChunkBasedCompression,
                buildTarget);

            if (manifest == null)
            {
                EditorUtility.DisplayDialog("AB Build", "构建失败，请检查 Console。", "OK");
                return;
            }

            var catalog = GenerateCatalog(database, outputRoot, buildTarget, manifest);
            WriteCatalog(catalog, outputRoot);
            CopyCatalogToResources(catalog);
            CopyBundlesToStreamingAssets(outputRoot, buildTarget);
            ABAddressCodeGen.Generate(database);

            ClearAssetBundleNames();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "AB Build",
                $"构建完成\n输出: {outputRoot}\n资源数: {catalog.assets.Length}\nBundle数: {catalog.bundles.Length}",
                "OK");
        }

        /// <summary>
        /// 清理工程内全部 AssetBundleName 标记。
        /// <para>菜单：<c>AB System/Clear AssetBundle Names</c>。</para>
        /// </summary>
        [MenuItem(MenuRoot + "Clear AssetBundle Names", priority = 110)]
        public static void ClearAssetBundleNamesMenu()
        {
            ClearAssetBundleNames();
            AssetDatabase.Refresh();
            Debug.Log("[AB] 已清理全部 AssetBundle 名称。");
        }

        /// <summary>
        /// 获取指定构建目标的 Bundle 输出根目录：<c>项目根/Bundles/{BuildTarget}</c>。
        /// </summary>
        /// <param name="buildTarget">Unity 构建目标。</param>
        /// <returns>绝对路径。</returns>
        public static string GetOutputRoot(BuildTarget buildTarget)
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            return Path.Combine(projectRoot, "Bundles", buildTarget.ToString());
        }

        /// <summary>
        /// 将标记数据库中的 Bundle 名写入各资源的 AssetImporter.assetBundleName。
        /// </summary>
        private static void ApplyAssetBundleNames(ABMarkDatabase database)
        {
            foreach (var mark in database.marks)
            {
                if (mark == null || string.IsNullOrEmpty(mark.assetPath) || string.IsNullOrEmpty(mark.bundleName))
                    continue;

                var importer = AssetImporter.GetAtPath(mark.assetPath);
                if (importer == null)
                {
                    Debug.LogWarning($"[AB] 无法设置 Bundle 名，资源不存在: {mark.assetPath}");
                    continue;
                }

                importer.assetBundleName = NormalizeBundleName(mark.bundleName);
            }

            AssetDatabase.RemoveUnusedAssetBundleNames();
            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// 移除工程中全部 AssetBundleName。
        /// </summary>
        private static void ClearAssetBundleNames()
        {
            foreach (var bundleName in AssetDatabase.GetAllAssetBundleNames())
                AssetDatabase.RemoveAssetBundleName(bundleName, true);
        }

        /// <summary>
        /// 根据标记与构建 Manifest 生成运行时 Catalog。
        /// </summary>
        private static ABCatalogData GenerateCatalog(
            ABMarkDatabase database,
            string outputRoot,
            BuildTarget buildTarget,
            AssetBundleManifest manifest)
        {
            var assetEntries = new List<ABAssetEntry>();
            var addressSet = new HashSet<string>();

            foreach (var mark in database.marks)
            {
                if (mark == null || string.IsNullOrEmpty(mark.assetPath))
                    continue;

                var address = string.IsNullOrEmpty(mark.address) ? mark.assetPath : mark.address;
                if (!addressSet.Add(address))
                {
                    Debug.LogError($"[AB] 地址重复: {address}，已跳过 {mark.assetPath}");
                    continue;
                }

                assetEntries.Add(new ABAssetEntry
                {
                    address = address,
                    bundleName = NormalizeBundleName(mark.bundleName),
                    assetPath = mark.assetPath,
                    group = string.IsNullOrEmpty(mark.group) ? ABDefine.DefaultGroup : mark.group
                });
            }

            var bundleEntries = new List<ABBundleEntry>();
            var allBundleNames = manifest.GetAllAssetBundles();
            foreach (var bundleName in allBundleNames)
            {
                var filePath = Path.Combine(outputRoot, bundleName);
                if (!File.Exists(filePath))
                    filePath = Path.Combine(outputRoot, bundleName + ABDefine.BundleExtension);

                var dependencies = manifest.GetAllDependencies(bundleName)
                    .Select(NormalizeBundleName)
                    .ToArray();

                bundleEntries.Add(new ABBundleEntry
                {
                    bundleName = NormalizeBundleName(bundleName),
                    dependencies = dependencies,
                    hash = File.Exists(filePath) ? ComputeFileHash(filePath) : string.Empty,
                    size = File.Exists(filePath) ? new FileInfo(filePath).Length : 0
                });
            }

            // Unity BuildPipeline 输出的文件名通常不带自定义扩展名，按需重命名为 .ab
            RenameBundlesWithExtension(outputRoot, allBundleNames);

            return new ABCatalogData
            {
                version = DateTime.Now.ToString("yyyyMMddHHmmss"),
                buildTarget = buildTarget.ToString(),
                assets = assetEntries.ToArray(),
                bundles = bundleEntries.ToArray()
            };
        }

        /// <summary>
        /// 将构建产物重命名为统一的 <c>.ab</c> 扩展名。
        /// </summary>
        private static void RenameBundlesWithExtension(string outputRoot, string[] bundleNames)
        {
            foreach (var bundleName in bundleNames)
            {
                var src = Path.Combine(outputRoot, bundleName);
                var dst = Path.Combine(outputRoot, NormalizeBundleName(bundleName) + ABDefine.BundleExtension);
                if (!File.Exists(src))
                    continue;

                if (File.Exists(dst))
                    File.Delete(dst);

                File.Move(src, dst);

                var srcManifest = src + ".manifest";
                var dstManifest = dst + ".manifest";
                if (File.Exists(srcManifest))
                {
                    if (File.Exists(dstManifest))
                        File.Delete(dstManifest);
                    File.Move(srcManifest, dstManifest);
                }
            }
        }

        /// <summary>
        /// 将 Catalog 写入构建输出目录。
        /// </summary>
        private static void WriteCatalog(ABCatalogData catalog, string outputRoot)
        {
            var json = JsonUtility.ToJson(catalog, true);
            var path = Path.Combine(outputRoot, ABDefine.CatalogFileName);
            File.WriteAllText(path, json, Encoding.UTF8);
            Debug.Log($"[AB] Catalog 已写入: {path}");
        }

        /// <summary>
        /// 将 Catalog 复制到 Resources，供运行时优先加载。
        /// </summary>
        private static void CopyCatalogToResources(ABCatalogData catalog)
        {
            var resourceDir = Path.Combine(Application.dataPath, "Plugins/AssetBundle/Resources/AB");
            if (!Directory.Exists(resourceDir))
                Directory.CreateDirectory(resourceDir);

            var json = JsonUtility.ToJson(catalog, true);
            var path = Path.Combine(resourceDir, ABDefine.CatalogFileName);
            File.WriteAllText(path, json, Encoding.UTF8);
            Debug.Log($"[AB] Catalog 已复制到 Resources: {path}");
        }

        /// <summary>
        /// 将 Bundle 与 Catalog 复制到 StreamingAssets，供真机包内加载。
        /// </summary>
        private static void CopyBundlesToStreamingAssets(string outputRoot, BuildTarget buildTarget)
        {
            var streamingRoot = Path.Combine(Application.streamingAssetsPath, ABDefine.StreamingBundleFolder);
            if (Directory.Exists(streamingRoot))
                Directory.Delete(streamingRoot, true);
            Directory.CreateDirectory(streamingRoot);

            foreach (var file in Directory.GetFiles(outputRoot, "*" + ABDefine.BundleExtension))
            {
                var dest = Path.Combine(streamingRoot, Path.GetFileName(file));
                File.Copy(file, dest, true);
            }

            var catalogSrc = Path.Combine(outputRoot, ABDefine.CatalogFileName);
            if (File.Exists(catalogSrc))
                File.Copy(catalogSrc, Path.Combine(streamingRoot, ABDefine.CatalogFileName), true);

            Debug.Log($"[AB] Bundle 已复制到 StreamingAssets ({buildTarget}): {streamingRoot}");
        }

        /// <summary>
        /// 规范化 Bundle 名：统一小写、斜杠，并去掉 <c>.ab</c> 扩展名。
        /// </summary>
        private static string NormalizeBundleName(string bundleName)
        {
            if (string.IsNullOrEmpty(bundleName))
                return "default";

            bundleName = bundleName.Replace('\\', '/').ToLowerInvariant();
            if (bundleName.EndsWith(ABDefine.BundleExtension, StringComparison.OrdinalIgnoreCase))
                bundleName = bundleName.Substring(0, bundleName.Length - ABDefine.BundleExtension.Length);

            return bundleName;
        }

        /// <summary>
        /// 计算文件 MD5 哈希。
        /// </summary>
        private static string ComputeFileHash(string filePath)
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filePath);
            var hash = md5.ComputeHash(stream);
            var builder = new StringBuilder(hash.Length * 2);
            foreach (var b in hash)
                builder.Append(b.ToString("x2"));
            return builder.ToString();
        }
    }
}
