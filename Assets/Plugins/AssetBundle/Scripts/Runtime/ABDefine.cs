namespace ABSystem
{
    /// <summary>
    /// AB 系统全局常量定义。
    /// <para>构建管线与运行时加载共用这些路径/命名约定，修改时需保持两端一致。</para>
    /// </summary>
    public static class ABDefine
    {
        /// <summary>
        /// Catalog 文件名（JSON）。
        /// </summary>
        public const string CatalogFileName = "ab_catalog.json";

        /// <summary>
        /// Catalog 在 Resources 下的加载路径（不含扩展名）。
        /// <para>对应资源：<c>Resources/AB/ab_catalog.json</c>。</para>
        /// </summary>
        public const string CatalogResourcePath = "AB/ab_catalog";

        /// <summary>
        /// AssetBundle 文件扩展名。
        /// </summary>
        public const string BundleExtension = ".ab";

        /// <summary>
        /// 默认资源分组名。未指定 group 时使用该值。
        /// </summary>
        public const string DefaultGroup = "Default";

        /// <summary>
        /// StreamingAssets 下存放 Bundle 与 Catalog 的子目录名。
        /// </summary>
        public const string StreamingBundleFolder = "AssetBundles";
    }
}
