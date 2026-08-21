///////////////////////////////////
//// This is a generated file. ////
////     Do not modify it.     ////
///////////////////////////////////

namespace ABSystem
{
    /// <summary>
    /// AB 资源地址常量库。
    /// <para>由标记数据库自动生成，业务侧通过 <c>ABAddress.分组.资源名</c> 引用。</para>
    /// </summary>
    public static partial class ABAddress
    {
        /// <summary>
        /// 分组：Prefab（对应 <see cref="ABGroup.Prefab"/>）
        /// </summary>
        public static class Prefab
        {
            /// <summary>
            /// Assets/GameUI/Res/Prefabs/UIAim.prefab
            /// <para>Address = UIAim</para>
            /// </summary>
            public const string UIAim = "UIAim";

        }

        /// <summary>
        /// 全部已登记地址（可用于校验或遍历）。
        /// </summary>
        public static readonly string[] All =
        {
            Prefab.UIAim,
        };

        /// <summary>
        /// 全部地址登记信息（含文件名映射，供反查使用）。
        /// </summary>
        public static readonly ABAddressInfo[] Infos =
        {
            new ABAddressInfo("UIAim", "UIAim.prefab", "UIAim", "Assets/GameUI/Res/Prefabs/UIAim.prefab", Prefab.UIAim, "Prefab"),
        };
    }
}
