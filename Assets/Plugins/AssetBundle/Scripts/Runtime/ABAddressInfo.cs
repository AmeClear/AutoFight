namespace ABSystem
{
    /// <summary>
    /// 单条 AB 地址登记信息，用于按文件名/路径反查 Address。
    /// </summary>
    public readonly struct ABAddressInfo
    {
        /// <summary>
        /// 代码常量名（如 <c>BarUI</c>）。
        /// </summary>
        public readonly string CodeName;

        /// <summary>
        /// 带扩展名的文件名（如 <c>BarUI.prefab</c>）。
        /// </summary>
        public readonly string FileName;

        /// <summary>
        /// 不带扩展名的文件名（如 <c>BarUI</c>）。
        /// </summary>
        public readonly string FileNameWithoutExtension;

        /// <summary>
        /// 工程内资产路径。
        /// </summary>
        public readonly string AssetPath;

        /// <summary>
        /// 运行时逻辑地址。
        /// </summary>
        public readonly string Address;

        /// <summary>
        /// 所属分组运行时名称。
        /// </summary>
        public readonly string Group;

        /// <summary>
        /// 创建地址登记信息。
        /// </summary>
        public ABAddressInfo(
            string codeName,
            string fileName,
            string fileNameWithoutExtension,
            string assetPath,
            string address,
            string group)
        {
            CodeName = codeName;
            FileName = fileName;
            FileNameWithoutExtension = fileNameWithoutExtension;
            AssetPath = assetPath;
            Address = address;
            Group = group;
        }
    }
}
