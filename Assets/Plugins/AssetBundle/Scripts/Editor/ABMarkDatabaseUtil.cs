using System.IO;
using UnityEditor;
using UnityEngine;

namespace ABSystem.Editor
{
    /// <summary>
    /// AB 标记数据库加载与创建辅助工具。
    /// </summary>
    public static class ABMarkDatabaseUtil
    {
        /// <summary>
        /// 加载默认路径下的标记数据库；若不存在则自动创建。
        /// </summary>
        /// <returns>可用的 <see cref="ABMarkDatabase"/> 实例。</returns>
            public static ABMarkDatabase LoadOrCreate()
            {
                var database = AssetDatabase.LoadAssetAtPath<ABMarkDatabase>(ABMarkDatabase.DefaultAssetPath);
                if (database != null)
                {
                    database.EnsureGroupsSynced();
                    return database;
                }

                var directory = Path.GetDirectoryName(ABMarkDatabase.DefaultAssetPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                database = ScriptableObject.CreateInstance<ABMarkDatabase>();
                database.EnsureGroupsSynced();
                AssetDatabase.CreateAsset(database, ABMarkDatabase.DefaultAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[AB] 已创建标记数据库: {ABMarkDatabase.DefaultAssetPath}");
                return database;
            }
    }
}
