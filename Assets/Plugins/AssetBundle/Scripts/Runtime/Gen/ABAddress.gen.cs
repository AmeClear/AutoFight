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
            /// Assets/GameUI/Res/Prefabs/BarUI.prefab
            /// <para>Address = Assets/GameUI/Res/Prefabs/BarUI.prefab</para>
            /// </summary>
            public const string BarUI = "Assets/GameUI/Res/Prefabs/BarUI.prefab";

            /// <summary>
            /// Assets/GameUI/Res/Prefabs/UISkill.prefab
            /// <para>Address = Assets/GameUI/Res/Prefabs/UISkill.prefab</para>
            /// </summary>
            public const string UISkill = "Assets/GameUI/Res/Prefabs/UISkill.prefab";

            /// <summary>
            /// Assets/Game/Res/Prefabs/3DUI/UnitHealthBar.prefab
            /// <para>Address = Assets/Game/Res/Prefabs/3DUI/UnitHealthBar.prefab</para>
            /// </summary>
            public const string UnitHealthBar = "Assets/Game/Res/Prefabs/3DUI/UnitHealthBar.prefab";

        }

        /// <summary>
        /// 全部已登记地址（可用于校验或遍历）。
        /// </summary>
        public static readonly string[] All =
        {
            Prefab.BarUI,
            Prefab.UISkill,
            Prefab.UnitHealthBar,
        };

        /// <summary>
        /// 全部地址登记信息（含文件名映射，供反查使用）。
        /// </summary>
        public static readonly ABAddressInfo[] Infos =
        {
            new ABAddressInfo("BarUI", "BarUI.prefab", "BarUI", "Assets/GameUI/Res/Prefabs/BarUI.prefab", Prefab.BarUI, "Prefab"),
            new ABAddressInfo("UISkill", "UISkill.prefab", "UISkill", "Assets/GameUI/Res/Prefabs/UISkill.prefab", Prefab.UISkill, "Prefab"),
            new ABAddressInfo("UnitHealthBar", "UnitHealthBar.prefab", "UnitHealthBar", "Assets/Game/Res/Prefabs/3DUI/UnitHealthBar.prefab", Prefab.UnitHealthBar, "Prefab"),
        };
    }
}
