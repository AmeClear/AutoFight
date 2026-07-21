using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ABSystem.Editor
{
    /// <summary>
    /// 根据标记数据库生成地址常量库与分组常量库。
    /// </summary>
    public static class ABAddressCodeGen
    {
        /// <summary>
        /// 地址常量生成文件路径。
        /// </summary>
        public const string AddressGeneratedFilePath = "Assets/Plugins/AssetBundle/Scripts/Runtime/Gen/ABAddress.gen.cs";

        /// <summary>
        /// 分组常量生成文件路径。
        /// </summary>
        public const string GroupGeneratedFilePath = "Assets/Plugins/AssetBundle/Scripts/Runtime/Gen/ABGroup.gen.cs";

        private const string MenuRoot = "AB System/";

        /// <summary>
        /// 从默认标记数据库生成地址与分组常量库。
        /// <para>菜单：<c>AB System/Generate Address Lib</c>。</para>
        /// </summary>
        [MenuItem(MenuRoot + "Generate Address Lib", priority = 50)]
        public static void GenerateFromMenu()
        {
            var database = ABMarkDatabaseUtil.LoadOrCreate();
            Generate(database);
            EditorUtility.DisplayDialog(
                "AB Address",
                $"常量库已生成：\n{AddressGeneratedFilePath}\n{GroupGeneratedFilePath}",
                "OK");
        }

        /// <summary>
        /// 根据标记数据库生成 <c>ABAddress</c> 与 <c>ABGroup</c>。
        /// </summary>
        public static bool Generate(ABMarkDatabase database)
        {
            if (database == null)
            {
                Debug.LogError("[ABAddressCodeGen] 标记数据库为空。");
                return false;
            }

            database.EnsureGroupsSynced();
            var addressOk = WriteFile(AddressGeneratedFilePath, BuildAddressSource(database), "地址");
            var groupOk = WriteFile(GroupGeneratedFilePath, BuildGroupSource(database), "分组");
            return addressOk && groupOk;
        }

        private static bool WriteFile(string relativePath, string code, string label)
        {
            var absolutePath = Path.Combine(
                Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath,
                relativePath.Replace('/', Path.DirectorySeparatorChar));

            var directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var previous = File.Exists(absolutePath) ? File.ReadAllText(absolutePath, Encoding.UTF8) : null;
            if (previous == code)
            {
                Debug.Log($"[ABAddressCodeGen] {label}常量库无变化，跳过写入。");
                return true;
            }

            File.WriteAllText(absolutePath, code, new UTF8Encoding(false));
            AssetDatabase.ImportAsset(relativePath);
            Debug.Log($"[ABAddressCodeGen] 已生成{label}常量库 → {relativePath}");
            return true;
        }

        private sealed class AddressEntry
        {
            public string GroupCodeName;
            public string CodeName;
            public string Address;
            public string AssetPath;
        }

        private static List<AddressEntry> CollectAddressEntries(ABMarkDatabase database)
        {
            var result = new List<AddressEntry>();
            var usedNamesByGroup = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

            foreach (var mark in database.marks)
            {
                if (mark == null || string.IsNullOrEmpty(mark.assetPath))
                    continue;

                var address = string.IsNullOrEmpty(mark.address) ? mark.assetPath : mark.address;
                var groupName = string.IsNullOrEmpty(mark.group) ? ABDefine.DefaultGroup : mark.group;
                database.EnsureGroup(groupName);
                var groupCodeName = database.ResolveGroupCodeName(groupName);
                var preferredName = !string.IsNullOrEmpty(mark.codeName)
                    ? mark.codeName
                    : Path.GetFileNameWithoutExtension(mark.assetPath);

                if (!usedNamesByGroup.TryGetValue(groupCodeName, out var usedNames))
                {
                    usedNames = new HashSet<string>(StringComparer.Ordinal);
                    usedNamesByGroup[groupCodeName] = usedNames;
                }

                var codeName = MakeUniqueIdentifier(preferredName, mark.assetPath, usedNames);

                result.Add(new AddressEntry
                {
                    GroupCodeName = groupCodeName,
                    CodeName = codeName,
                    Address = address,
                    AssetPath = mark.assetPath
                });
            }

            return result
                .OrderBy(e => e.GroupCodeName, StringComparer.Ordinal)
                .ThenBy(e => e.CodeName, StringComparer.Ordinal)
                .ToList();
        }

        private static string BuildAddressSource(ABMarkDatabase database)
        {
            var entries = CollectAddressEntries(database);
            var sb = new StringBuilder();
            sb.AppendLine("///////////////////////////////////");
            sb.AppendLine("//// This is a generated file. ////");
            sb.AppendLine("////     Do not modify it.     ////");
            sb.AppendLine("///////////////////////////////////");
            sb.AppendLine();
            sb.AppendLine("namespace ABSystem");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// AB 资源地址常量库。");
            sb.AppendLine("    /// <para>由标记数据库自动生成，业务侧通过 <c>ABAddress.分组.资源名</c> 引用。</para>");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public static partial class ABAddress");
            sb.AppendLine("    {");

            foreach (var group in entries.GroupBy(e => e.GroupCodeName))
            {
                sb.AppendLine("        /// <summary>");
                sb.AppendLine($"        /// 分组：{group.Key}（对应 <see cref=\"ABGroup.{group.Key}\"/>）");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        public static class {group.Key}");
                sb.AppendLine("        {");

                foreach (var entry in group)
                {
                    sb.AppendLine("            /// <summary>");
                    sb.AppendLine($"            /// {ABCodeGenUtil.EscapeXml(entry.AssetPath)}");
                    sb.AppendLine($"            /// <para>Address = {ABCodeGenUtil.EscapeXml(entry.Address)}</para>");
                    sb.AppendLine("            /// </summary>");
                    sb.AppendLine($"            public const string {entry.CodeName} = \"{ABCodeGenUtil.EscapeCSharp(entry.Address)}\";");
                    sb.AppendLine();
                }

                sb.AppendLine("        }");
                sb.AppendLine();
            }

            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// 全部已登记地址（可用于校验或遍历）。");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public static readonly string[] All =");
            sb.AppendLine("        {");
            foreach (var entry in entries)
                sb.AppendLine($"            {entry.GroupCodeName}.{entry.CodeName},");
            sb.AppendLine("        };");
            sb.AppendLine();

            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// 地址是否已在常量库中登记。");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public static bool Contains(string address)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (string.IsNullOrEmpty(address))");
            sb.AppendLine("                return false;");
            sb.AppendLine("            for (var i = 0; i < All.Length; i++)");
            sb.AppendLine("            {");
            sb.AppendLine("                if (All[i] == address)");
            sb.AppendLine("                    return true;");
            sb.AppendLine("            }");
            sb.AppendLine("            return false;");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static string BuildGroupSource(ABMarkDatabase database)
        {
            database.EnsureGroupsSynced();
            var usedCodeNames = new HashSet<string>(StringComparer.Ordinal);
            var rows = new List<(string CodeName, string Name, string Description)>();

            foreach (var group in database.groups)
            {
                if (group == null || string.IsNullOrEmpty(group.name))
                    continue;

                var preferred = string.IsNullOrEmpty(group.codeName) ? group.name : group.codeName;
                var codeName = ABCodeGenUtil.ToIdentifier(preferred, ABDefine.DefaultGroup);
                if (!usedCodeNames.Add(codeName))
                {
                    var index = 2;
                    var unique = $"{codeName}_{index}";
                    while (!usedCodeNames.Add(unique))
                    {
                        index++;
                        unique = $"{codeName}_{index}";
                    }

                    codeName = unique;
                }

                rows.Add((codeName, group.name, group.description ?? string.Empty));
            }

            rows = rows.OrderBy(r => r.CodeName, StringComparer.Ordinal).ToList();

            var sb = new StringBuilder();
            sb.AppendLine("///////////////////////////////////");
            sb.AppendLine("//// This is a generated file. ////");
            sb.AppendLine("////     Do not modify it.     ////");
            sb.AppendLine("///////////////////////////////////");
            sb.AppendLine();
            sb.AppendLine("namespace ABSystem");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// AB 资源分组常量库。");
            sb.AppendLine("    /// <para>由标记数据库自动生成，业务侧通过 <c>ABGroup.分组名</c> 引用。</para>");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public static partial class ABGroup");
            sb.AppendLine("    {");

            foreach (var row in rows)
            {
                sb.AppendLine("        /// <summary>");
                if (!string.IsNullOrEmpty(row.Description))
                    sb.AppendLine($"        /// {ABCodeGenUtil.EscapeXml(row.Description)}");
                else
                    sb.AppendLine($"        /// 分组：{ABCodeGenUtil.EscapeXml(row.Name)}");
                sb.AppendLine($"        /// <para>Group = {ABCodeGenUtil.EscapeXml(row.Name)}</para>");
                sb.AppendLine("        /// </summary>");
                sb.AppendLine($"        public const string {row.CodeName} = \"{ABCodeGenUtil.EscapeCSharp(row.Name)}\";");
                sb.AppendLine();
            }

            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// 全部已登记分组。");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public static readonly string[] All =");
            sb.AppendLine("        {");
            foreach (var row in rows)
                sb.AppendLine($"            {row.CodeName},");
            sb.AppendLine("        };");
            sb.AppendLine();

            sb.AppendLine("        /// <summary>");
            sb.AppendLine("        /// 分组是否已在常量库中登记。");
            sb.AppendLine("        /// </summary>");
            sb.AppendLine("        public static bool Contains(string group)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (string.IsNullOrEmpty(group))");
            sb.AppendLine("                return false;");
            sb.AppendLine("            for (var i = 0; i < All.Length; i++)");
            sb.AppendLine("            {");
            sb.AppendLine("                if (All[i] == group)");
            sb.AppendLine("                    return true;");
            sb.AppendLine("            }");
            sb.AppendLine("            return false;");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }

        /// <summary>
        /// 为资源生成建议的逻辑地址（短字符串）。
        /// </summary>
        public static string SuggestAddress(string assetPath, IEnumerable<string> existingAddresses)
        {
            var fileName = Path.GetFileNameWithoutExtension(assetPath);
            if (string.IsNullOrEmpty(fileName))
                return assetPath;

            var existing = new HashSet<string>(existingAddresses ?? Array.Empty<string>(), StringComparer.Ordinal);
            if (!existing.Contains(fileName))
                return fileName;

            var parent = Path.GetFileName(Path.GetDirectoryName(assetPath) ?? string.Empty);
            var candidate = string.IsNullOrEmpty(parent) ? $"{fileName}_1" : $"{parent}/{fileName}";
            if (!existing.Contains(candidate))
                return candidate;

            var index = 2;
            while (existing.Contains($"{candidate}_{index}"))
                index++;
            return $"{candidate}_{index}";
        }

        /// <summary>
        /// 为资源生成建议的代码常量名。
        /// </summary>
        public static string SuggestCodeName(string assetPath)
        {
            return ABCodeGenUtil.ToIdentifier(Path.GetFileNameWithoutExtension(assetPath), "Asset");
        }

        private static string MakeUniqueIdentifier(string preferred, string assetPath, HashSet<string> usedNames)
        {
            var baseName = ABCodeGenUtil.ToIdentifier(preferred, "Asset");
            if (usedNames.Add(baseName))
                return baseName;

            var parent = Path.GetFileName(Path.GetDirectoryName(assetPath) ?? string.Empty);
            var withParent = ABCodeGenUtil.ToIdentifier($"{parent}_{baseName}", baseName);
            if (usedNames.Add(withParent))
                return withParent;

            var index = 2;
            while (!usedNames.Add($"{baseName}_{index}"))
                index++;
            return $"{baseName}_{index}";
        }
    }
}
