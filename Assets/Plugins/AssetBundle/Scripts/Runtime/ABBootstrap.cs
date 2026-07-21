using UnityEngine;

namespace ABSystem
{
    /// <summary>
    /// AB 系统启动引导组件。
    /// <para>挂到启动场景后，可在 Awake 时自动调用 <see cref="ABLoader.InitializeAsync"/>。</para>
    /// </summary>
    public class ABBootstrap : MonoBehaviour
    {
        /// <summary>
        /// 是否在 Awake 时自动初始化 AB 系统。
        /// </summary>
        [SerializeField]
        [Tooltip("Awake 时自动初始化 AB 系统")]
        private bool initializeOnAwake = true;

        /// <summary>
        /// 是否跨场景常驻。
        /// </summary>
        [SerializeField]
        [Tooltip("初始化后 DontDestroyOnLoad")]
        private bool dontDestroyOnLoad = true;

        /// <summary>
        /// 自定义 Bundle 根目录。留空则使用系统默认路径。
        /// </summary>
        [SerializeField]
        [Tooltip("自定义 Bundle 根目录，留空使用默认路径")]
        private string customBundleRoot;

        private async void Awake()
        {
            if (dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);

            if (!initializeOnAwake)
                return;

            await ABLoader.Instance.InitializeAsync(
                string.IsNullOrEmpty(customBundleRoot) ? null : customBundleRoot);

            if (!ABLoader.Instance.IsReady)
                Debug.LogWarning("[ABBootstrap] AB 系统初始化未完成，请检查 Catalog / Bundle 是否已构建。");
        }
    }
}
