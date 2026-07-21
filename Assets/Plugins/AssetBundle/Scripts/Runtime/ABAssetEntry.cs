using System;

namespace ABSystem
{
    /// <summary>
    /// Catalog 中的单条资源条目。
    /// <para>描述「逻辑地址 → Bundle 内真实路径」的映射关系。</para>
    /// </summary>
    [Serializable]
    public class ABAssetEntry
    {
        /// <summary>
        /// 逻辑地址。运行时通过该地址请求资源，可与 <see cref="assetPath"/> 相同或自定义别名。
        /// </summary>
        public string address;

        /// <summary>
        /// 所属 Bundle 名（不含扩展名，小写）。
        /// </summary>
        public string bundleName;

        /// <summary>
        /// 资源在工程/Bundle 内的完整资产路径，例如 <c>Assets/GameUI/Res/Prefabs/BarUI.prefab</c>。
        /// </summary>
        public string assetPath;

        /// <summary>
        /// 资源分组名，便于按组预加载或批量管理。默认为 <see cref="ABDefine.DefaultGroup"/>。
        /// </summary>
        public string group = ABDefine.DefaultGroup;
    }

    /// <summary>
    /// Catalog 中的单条 Bundle 条目。
    /// <para>记录 Bundle 依赖、哈希与体积，供加载与热更校验使用。</para>
    /// </summary>
    [Serializable]
    public class ABBundleEntry
    {
        /// <summary>
        /// Bundle 名（不含扩展名，小写）。
        /// </summary>
        public string bundleName;

        /// <summary>
        /// 直接依赖的其他 Bundle 名列表。加载本 Bundle 前会先加载依赖。
        /// </summary>
        public string[] dependencies = Array.Empty<string>();

        /// <summary>
        /// Bundle 文件内容哈希（MD5），可用于完整性校验或差分更新。
        /// </summary>
        public string hash;

        /// <summary>
        /// Bundle 文件字节大小。
        /// </summary>
        public long size;
    }

    /// <summary>
    /// Catalog 根数据结构。
    /// <para>由构建管线生成，运行时经 JSON 反序列化后交给 <see cref="ABCatalog"/> 使用。</para>
    /// </summary>
    [Serializable]
    public class ABCatalogData
    {
        /// <summary>
        /// Catalog 版本号。构建时通常写入时间戳字符串。
        /// </summary>
        public string version = "1.0.0";

        /// <summary>
        /// 构建时的目标平台名称，例如 <c>StandaloneWindows64</c>。
        /// </summary>
        public string buildTarget;

        /// <summary>
        /// 全部资源条目。
        /// </summary>
        public ABAssetEntry[] assets = Array.Empty<ABAssetEntry>();

        /// <summary>
        /// 全部 Bundle 条目。
        /// </summary>
        public ABBundleEntry[] bundles = Array.Empty<ABBundleEntry>();
    }
}
