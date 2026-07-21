using System.Collections.Generic;
using UnityEngine;

namespace ABSystem
{
    /// <summary>
    /// Catalog 的 ScriptableObject 包装，便于在 Inspector 中查看或手工注入。
    /// <para>正式运行时默认仍读取 JSON Catalog；本资产为可选辅助。</para>
    /// </summary>
    [CreateAssetMenu(fileName = "ABCatalogAsset", menuName = "AB System/Catalog Asset", order = 1)]
    public class ABCatalogAsset : ScriptableObject
    {
        /// <summary>
        /// Catalog 数据本体。
        /// </summary>
        public ABCatalogData data = new ABCatalogData();

        /// <summary>
        /// 用外部 Catalog 数据覆盖当前内容。
        /// </summary>
        /// <param name="catalogData">源数据；为 null 时写入空 Catalog。</param>
        public void FromData(ABCatalogData catalogData)
        {
            data = catalogData ?? new ABCatalogData();
        }

        /// <summary>
        /// 根据当前 data 构建 address → entry 字典，便于快速查找。
        /// </summary>
        /// <returns>地址映射表；无数据时返回空字典。</returns>
        public Dictionary<string, ABAssetEntry> BuildAddressMap()
        {
            var map = new Dictionary<string, ABAssetEntry>();
            if (data?.assets == null)
                return map;

            foreach (var entry in data.assets)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.address))
                    map[entry.address] = entry;
            }

            return map;
        }
    }
}
