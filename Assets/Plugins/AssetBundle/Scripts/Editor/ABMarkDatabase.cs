using System;
using System.Collections.Generic;
using UnityEngine;

namespace ABSystem.Editor
{
    /// <summary>
    /// 单条 AB 分组记录。
    /// </summary>
    [Serializable]
    public class ABGroupRecord
    {
        /// <summary>
        /// 分组运行时名称（写入 Catalog / 标记记录的 group 字段）。
        /// </summary>
        public string name = ABDefine.DefaultGroup;

        /// <summary>
        /// 生成到 <c>ABGroup</c> / <c>ABAddress</c> 中的常量名。留空时按 <see cref="name"/> 推导。
        /// </summary>
        public string codeName;

        /// <summary>
        /// 分组说明，仅用于编辑器展示。
        /// </summary>
        public string description;
    }

    /// <summary>
    /// 单条 AB 标记记录。
    /// <para>描述一个待打包资源的路径、加载地址、所属 Bundle 与分组。</para>
    /// </summary>
    [Serializable]
    public class ABMarkRecord
    {
        /// <summary>
        /// 资源在工程中的完整路径，例如 <c>Assets/GameUI/Res/Prefabs/BarUI.prefab</c>。
        /// </summary>
        public string assetPath;

        /// <summary>
        /// 运行时逻辑地址。留空时构建会回退为 <see cref="assetPath"/>。
        /// <para>建议使用短字符串（如 <c>BarUI</c>），便于常量化引用。</para>
        /// </summary>
        public string address;

        /// <summary>
        /// 生成到 <c>ABAddress</c> 中的常量名。留空时按文件名自动生成。
        /// </summary>
        public string codeName;

        /// <summary>
        /// 目标 Bundle 名（可含路径风格分段，构建时会规范化为小写且去除扩展名）。
        /// </summary>
        public string bundleName;

        /// <summary>
        /// 资源分组名，须对应 <see cref="ABMarkDatabase.groups"/> 中的某一项。
        /// </summary>
        public string group = ABSystem.ABDefine.DefaultGroup;
    }

    /// <summary>
    /// AB 标记数据库。
    /// <para>持久化保存分组定义与待打包资源标记，作为构建与常量生成的输入源。</para>
    /// </summary>
    [CreateAssetMenu(fileName = "ABMarkDatabase", menuName = "AB System/Mark Database", order = 0)]
    public class ABMarkDatabase : ScriptableObject
    {
        /// <summary>
        /// 默认数据库资产路径。
        /// </summary>
        public const string DefaultAssetPath = "Assets/Plugins/AssetBundle/Editor/ABMarkDatabase.asset";

        /// <summary>
        /// 已管理的分组列表。
        /// </summary>
        public List<ABGroupRecord> groups = new List<ABGroupRecord>();

        /// <summary>
        /// 全部标记记录列表。
        /// </summary>
        public List<ABMarkRecord> marks = new List<ABMarkRecord>();

        /// <summary>
        /// 确保至少包含默认分组，并把标记中出现过的分组同步进列表。
        /// </summary>
        public void EnsureGroupsSynced()
        {
            EnsureGroup(ABDefine.DefaultGroup, ABDefine.DefaultGroup, "默认分组");

            if (marks == null)
                return;

            foreach (var mark in marks)
            {
                if (mark == null || string.IsNullOrEmpty(mark.group))
                    continue;
                EnsureGroup(mark.group);
            }
        }

        /// <summary>
        /// 确保指定分组存在；不存在则追加。
        /// </summary>
        /// <param name="groupName">分组运行时名称。</param>
        /// <param name="codeName">可选常量名。</param>
        /// <param name="description">可选说明。</param>
        /// <returns>分组记录。</returns>
        public ABGroupRecord EnsureGroup(string groupName, string codeName = null, string description = null)
        {
            if (string.IsNullOrEmpty(groupName))
                groupName = ABDefine.DefaultGroup;

            if (TryGetGroup(groupName, out var existing))
            {
                if (!string.IsNullOrEmpty(codeName) && string.IsNullOrEmpty(existing.codeName))
                    existing.codeName = codeName;
                if (!string.IsNullOrEmpty(description) && string.IsNullOrEmpty(existing.description))
                    existing.description = description;
                return existing;
            }

            var record = new ABGroupRecord
            {
                name = groupName,
                codeName = string.IsNullOrEmpty(codeName) ? groupName : codeName,
                description = description ?? string.Empty
            };
            groups.Add(record);
            return record;
        }

        /// <summary>
        /// 按运行时分组名查找分组。
        /// </summary>
        public bool TryGetGroup(string groupName, out ABGroupRecord record)
        {
            for (var i = 0; i < groups.Count; i++)
            {
                if (groups[i] != null && groups[i].name == groupName)
                {
                    record = groups[i];
                    return true;
                }
            }

            record = null;
            return false;
        }

        /// <summary>
        /// 移除分组。若仍有标记引用该分组则失败。
        /// </summary>
        /// <param name="groupName">分组运行时名称。</param>
        /// <param name="error">失败原因。</param>
        /// <returns>是否移除成功。</returns>
        public bool RemoveGroup(string groupName, out string error)
        {
            error = null;
            if (groupName == ABDefine.DefaultGroup)
            {
                error = "不能删除默认分组。";
                return false;
            }

            for (var i = 0; i < marks.Count; i++)
            {
                if (marks[i] != null && marks[i].group == groupName)
                {
                    error = $"仍有标记使用分组 [{groupName}]，无法删除。";
                    return false;
                }
            }

            for (var i = groups.Count - 1; i >= 0; i--)
            {
                if (groups[i] == null || groups[i].name != groupName)
                    continue;
                groups.RemoveAt(i);
                return true;
            }

            error = $"未找到分组 [{groupName}]。";
            return false;
        }

        /// <summary>
        /// 获取全部分组运行时名称。
        /// </summary>
        public string[] GetGroupNames()
        {
            EnsureGroupsSynced();
            var names = new List<string>(groups.Count);
            foreach (var group in groups)
            {
                if (group != null && !string.IsNullOrEmpty(group.name) && !names.Contains(group.name))
                    names.Add(group.name);
            }

            if (names.Count == 0)
                names.Add(ABDefine.DefaultGroup);
            return names.ToArray();
        }

        /// <summary>
        /// 将分组名解析为代码常量标识符（用于 <c>ABGroup</c> / <c>ABAddress</c> 嵌套类）。
        /// </summary>
        public string ResolveGroupCodeName(string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
                groupName = ABDefine.DefaultGroup;

            if (TryGetGroup(groupName, out var record) && !string.IsNullOrEmpty(record.codeName))
                return ABCodeGenUtil.ToIdentifier(record.codeName, ABDefine.DefaultGroup);

            return ABCodeGenUtil.ToIdentifier(groupName, ABDefine.DefaultGroup);
        }

        /// <summary>
        /// 按资产路径查找标记记录。
        /// </summary>
        public bool TryGet(string assetPath, out ABMarkRecord record)
        {
            for (var i = 0; i < marks.Count; i++)
            {
                if (marks[i].assetPath == assetPath)
                {
                    record = marks[i];
                    return true;
                }
            }

            record = null;
            return false;
        }

        /// <summary>
        /// 新增或更新一条标记。
        /// <para>会自动将分组登记到 <see cref="groups"/>。</para>
        /// </summary>
        public ABMarkRecord AddOrUpdate(
            string assetPath,
            string address,
            string bundleName,
            string group,
            string codeName = null)
        {
            var resolvedGroup = string.IsNullOrEmpty(group) ? ABSystem.ABDefine.DefaultGroup : group;
            EnsureGroup(resolvedGroup);

            if (TryGet(assetPath, out var existing))
            {
                existing.address = address;
                existing.bundleName = bundleName;
                existing.group = resolvedGroup;
                if (!string.IsNullOrEmpty(codeName))
                    existing.codeName = codeName;
                return existing;
            }

            var record = new ABMarkRecord
            {
                assetPath = assetPath,
                address = address,
                codeName = codeName,
                bundleName = bundleName,
                group = resolvedGroup
            };
            marks.Add(record);
            return record;
        }

        /// <summary>
        /// 按资产路径移除标记。
        /// </summary>
        public bool Remove(string assetPath)
        {
            for (var i = marks.Count - 1; i >= 0; i--)
            {
                if (marks[i].assetPath != assetPath)
                    continue;
                marks.RemoveAt(i);
                return true;
            }

            return false;
        }
    }
}
