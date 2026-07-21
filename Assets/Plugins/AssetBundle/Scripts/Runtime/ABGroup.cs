using Cysharp.Threading.Tasks;

namespace ABSystem
{
    /// <summary>
    /// <see cref="ABGroup"/> 的运行时扩展：提供基于分组常量的便捷操作。
    /// </summary>
    public static partial class ABGroup
    {
        /// <summary>
        /// 预加载指定分组内的全部资源。
        /// </summary>
        /// <param name="group">分组名，推荐传入 <c>ABGroup.xxx</c>。</param>
        public static UniTask PreloadAsync(string group)
        {
            return ABLoader.Instance.PreloadGroupAsync(group);
        }
    }
}
