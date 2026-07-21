///////////////////////////////////
//// This is a generated file. ////
////     Do not modify it.     ////
///////////////////////////////////

namespace ABSystem
{
    /// <summary>
    /// AB 资源分组常量库。
    /// <para>由标记数据库自动生成，业务侧通过 <c>ABGroup.分组名</c> 引用。</para>
    /// </summary>
    public static partial class ABGroup
    {
        /// <summary>
        /// 默认分组
        /// <para>Group = Default</para>
        /// </summary>
        public const string Default = "Default";

        /// <summary>
        /// 预制体
        /// <para>Group = Prefab</para>
        /// </summary>
        public const string Prefab = "Prefab";

        /// <summary>
        /// 全部已登记分组。
        /// </summary>
        public static readonly string[] All =
        {
            Default,
            Prefab,
        };

        /// <summary>
        /// 分组是否已在常量库中登记。
        /// </summary>
        public static bool Contains(string group)
        {
            if (string.IsNullOrEmpty(group))
                return false;
            for (var i = 0; i < All.Length; i++)
            {
                if (All[i] == group)
                    return true;
            }
            return false;
        }
    }
}
