using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ABSystem.Editor
{
    /// <summary>
    /// AB 标记与分组管理窗口。
    /// <para>
    /// 提供分组管理、标记编辑、地址/分组常量生成与一键构建入口。
    /// 菜单：<c>AB System/Mark Window</c>。
    /// </para>
    /// </summary>
    public class ABMarkWindow : EditorWindow
    {
        private ABMarkDatabase _database;
        private Vector2 _markScroll;
        private Vector2 _groupScroll;
        private string _filter = string.Empty;
        private string _defaultBundleName = "default";
        private string _defaultGroup = ABDefine.DefaultGroup;
        private string _newGroupName = string.Empty;
        private string _newGroupCodeName = string.Empty;
        private string _newGroupDescription = string.Empty;

        /// <summary>
        /// 打开标记管理窗口。
        /// </summary>
        [MenuItem("AB System/Mark Window", priority = 0)]
        public static void Open()
        {
            var window = GetWindow<ABMarkWindow>("AB Mark");
            window.minSize = new Vector2(980, 520);
            window.Show();
        }

        /// <summary>
        /// 将 Project 窗口当前选中的资源写入标记数据库。
        /// </summary>
        [MenuItem("Assets/AB System/Mark Selected", priority = 2000)]
        public static void MarkSelected()
        {
            var database = ABMarkDatabaseUtil.LoadOrCreate();
            database.EnsureGroupsSynced();
            var guids = Selection.assetGUIDs;
            if (guids == null || guids.Length == 0)
            {
                EditorUtility.DisplayDialog("AB Mark", "请先选中要标记的资源。", "OK");
                return;
            }

            var existingAddresses = database.marks
                .Where(m => m != null && !string.IsNullOrEmpty(m.address))
                .Select(m => m.address)
                .ToList();

            var defaultGroup = database.TryGetGroup(ABDefine.DefaultGroup, out _)
                ? ABDefine.DefaultGroup
                : database.GetGroupNames().FirstOrDefault() ?? ABDefine.DefaultGroup;

            var marked = 0;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path) || AssetDatabase.IsValidFolder(path))
                    continue;

                if (database.TryGet(path, out var existing))
                {
                    existingAddresses.Add(existing.address);
                    marked++;
                    continue;
                }

                var address = ABAddressCodeGen.SuggestAddress(path, existingAddresses);
                var codeName = ABAddressCodeGen.SuggestCodeName(path);
                var bundleName = GuessBundleName(path);
                database.AddOrUpdate(path, address, bundleName, defaultGroup, codeName);
                existingAddresses.Add(address);
                marked++;
            }

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            ABAddressCodeGen.Generate(database);
            Debug.Log($"[AB] 已标记 {marked} 个资源，并刷新地址/分组常量库。");
        }

        /// <summary>
        /// 取消 Project 窗口当前选中资源的 AB 标记。
        /// </summary>
        [MenuItem("Assets/AB System/Unmark Selected", priority = 2001)]
        public static void UnmarkSelected()
        {
            var database = ABMarkDatabaseUtil.LoadOrCreate();
            var guids = Selection.assetGUIDs;
            if (guids == null || guids.Length == 0)
                return;

            var removed = 0;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (database.Remove(path))
                    removed++;
            }

            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            ABAddressCodeGen.Generate(database);
            Debug.Log($"[AB] 已取消标记 {removed} 个资源，并刷新地址/分组常量库。");
        }

        private void OnEnable()
        {
            _database = ABMarkDatabaseUtil.LoadOrCreate();
            _database.EnsureGroupsSynced();
            SyncDefaultGroupPopup();
        }

        private void OnGUI()
        {
            if (_database == null)
            {
                _database = ABMarkDatabaseUtil.LoadOrCreate();
                _database.EnsureGroupsSynced();
            }

            DrawToolbar();
            EditorGUILayout.Space(4);
            DrawGroupPanel();
            EditorGUILayout.Space(6);
            DrawMarkList();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(60)))
                {
                    _database = ABMarkDatabaseUtil.LoadOrCreate();
                    _database.EnsureGroupsSynced();
                    SyncDefaultGroupPopup();
                }

                if (GUILayout.Button("标记选中", EditorStyles.toolbarButton, GUILayout.Width(80)))
                    MarkSelected();

                if (GUILayout.Button("取消选中标记", EditorStyles.toolbarButton, GUILayout.Width(100)))
                    UnmarkSelected();

                if (GUILayout.Button("生成常量库", EditorStyles.toolbarButton, GUILayout.Width(90)))
                    ABAddressCodeGen.Generate(_database);

                if (GUILayout.Button("构建 AB", EditorStyles.toolbarButton, GUILayout.Width(80)))
                    ABBuilder.BuildAll();

                GUILayout.FlexibleSpace();
                _filter = GUILayout.TextField(_filter, EditorStyles.toolbarSearchField, GUILayout.Width(220));
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                _defaultBundleName = EditorGUILayout.TextField("默认 Bundle", _defaultBundleName);

                var groupNames = _database.GetGroupNames();
                var groupIndex = Mathf.Max(0, Array.IndexOf(groupNames, _defaultGroup));
                var newIndex = EditorGUILayout.Popup("默认 Group", groupIndex, groupNames);
                if (newIndex >= 0 && newIndex < groupNames.Length)
                    _defaultGroup = groupNames[newIndex];

                if (GUILayout.Button("应用到过滤行", GUILayout.Width(110)))
                    ApplyDefaultsToFiltered();
                if (GUILayout.Button("建议短地址", GUILayout.Width(90)))
                    ApplySuggestedAddressesToFiltered();
            }

            EditorGUILayout.LabelField(
                $"分组数: {_database.groups.Count}    标记数: {_database.marks.Count}",
                EditorStyles.miniLabel);
        }

        /// <summary>
        /// 绘制分组管理面板。
        /// </summary>
        private void DrawGroupPanel()
        {
            EditorGUILayout.LabelField("分组管理", EditorStyles.boldLabel);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Name", GUILayout.Width(120));
                    GUILayout.Label("CodeName", GUILayout.Width(120));
                    GUILayout.Label("Description", GUILayout.Width(220));
                    GUILayout.Label("", GUILayout.Width(50));
                }

                using (var scroll = new EditorGUILayout.ScrollViewScope(_groupScroll, GUILayout.Height(110)))
                {
                    _groupScroll = scroll.scrollPosition;
                    var dirty = false;

                    for (var i = 0; i < _database.groups.Count; i++)
                    {
                        var group = _database.groups[i];
                        if (group == null)
                            continue;

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            EditorGUI.BeginDisabledGroup(group.name == ABDefine.DefaultGroup);
                            var name = EditorGUILayout.TextField(group.name ?? string.Empty, GUILayout.Width(120));
                            EditorGUI.EndDisabledGroup();

                            var codeName = EditorGUILayout.TextField(group.codeName ?? string.Empty, GUILayout.Width(120));
                            var description = EditorGUILayout.TextField(group.description ?? string.Empty, GUILayout.Width(220));

                            if (name != group.name ||
                                codeName != (group.codeName ?? string.Empty) ||
                                description != (group.description ?? string.Empty))
                            {
                                var oldName = group.name;
                                group.name = string.IsNullOrEmpty(name) ? oldName : name;
                                group.codeName = codeName;
                                group.description = description;

                                if (oldName != group.name)
                                    RenameGroupReferences(oldName, group.name);

                                dirty = true;
                            }

                            EditorGUI.BeginDisabledGroup(group.name == ABDefine.DefaultGroup);
                            if (GUILayout.Button("X", GUILayout.Width(24)))
                            {
                                if (_database.RemoveGroup(group.name, out var error))
                                {
                                    dirty = true;
                                    GUIUtility.ExitGUI();
                                }
                                else
                                {
                                    EditorUtility.DisplayDialog("AB Group", error, "OK");
                                }
                            }

                            EditorGUI.EndDisabledGroup();
                        }
                    }

                    if (dirty)
                        SaveAndGenerate();
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    _newGroupName = EditorGUILayout.TextField(_newGroupName, GUILayout.Width(120));
                    _newGroupCodeName = EditorGUILayout.TextField(_newGroupCodeName, GUILayout.Width(120));
                    _newGroupDescription = EditorGUILayout.TextField(_newGroupDescription, GUILayout.Width(220));
                    if (GUILayout.Button("新增分组", GUILayout.Width(80)))
                        AddNewGroup();
                }
            }
        }

        private void DrawMarkList()
        {
            EditorGUILayout.LabelField("资源标记", EditorStyles.boldLabel);
            var groupNames = _database.GetGroupNames();

            using (var scroll = new EditorGUILayout.ScrollViewScope(_markScroll))
            {
                _markScroll = scroll.scrollPosition;

                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    GUILayout.Label("Asset Path", GUILayout.Width(250));
                    GUILayout.Label("Address", GUILayout.Width(150));
                    GUILayout.Label("CodeName", GUILayout.Width(110));
                    GUILayout.Label("Bundle", GUILayout.Width(90));
                    GUILayout.Label("Group", GUILayout.Width(110));
                    GUILayout.Label("", GUILayout.Width(80));
                }

                var marks = string.IsNullOrEmpty(_filter)
                    ? _database.marks
                    : _database.marks.Where(m =>
                        m.assetPath.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (!string.IsNullOrEmpty(m.address) &&
                         m.address.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (!string.IsNullOrEmpty(m.codeName) &&
                         m.codeName.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (!string.IsNullOrEmpty(m.group) &&
                         m.group.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0)).ToList();

                var dirty = false;
                for (var i = 0; i < marks.Count; i++)
                {
                    var mark = marks[i];
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.SelectableLabel(mark.assetPath, GUILayout.Width(250), GUILayout.Height(18));

                        var address = EditorGUILayout.TextField(mark.address ?? string.Empty, GUILayout.Width(150));
                        var codeName = EditorGUILayout.TextField(mark.codeName ?? string.Empty, GUILayout.Width(110));
                        var bundleName = EditorGUILayout.TextField(mark.bundleName ?? string.Empty, GUILayout.Width(90));

                        var groupIndex = Mathf.Max(0, Array.IndexOf(groupNames, mark.group));
                        var newGroupIndex = EditorGUILayout.Popup(groupIndex, groupNames, GUILayout.Width(110));
                        var group = groupNames[Mathf.Clamp(newGroupIndex, 0, groupNames.Length - 1)];

                        if (address != mark.address ||
                            codeName != (mark.codeName ?? string.Empty) ||
                            bundleName != mark.bundleName ||
                            group != mark.group)
                        {
                            mark.address = address;
                            mark.codeName = codeName;
                            mark.bundleName = bundleName;
                            mark.group = group;
                            dirty = true;
                        }

                        if (GUILayout.Button("定位", GUILayout.Width(50)))
                        {
                            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(mark.assetPath);
                            if (asset != null)
                            {
                                Selection.activeObject = asset;
                                EditorGUIUtility.PingObject(asset);
                            }
                        }

                        if (GUILayout.Button("X", GUILayout.Width(24)))
                        {
                            _database.Remove(mark.assetPath);
                            dirty = true;
                            GUIUtility.ExitGUI();
                        }
                    }
                }

                if (dirty)
                    SaveAndGenerate();
            }
        }

        private void AddNewGroup()
        {
            if (string.IsNullOrWhiteSpace(_newGroupName))
            {
                EditorUtility.DisplayDialog("AB Group", "请输入分组 Name。", "OK");
                return;
            }

            if (_database.TryGetGroup(_newGroupName.Trim(), out _))
            {
                EditorUtility.DisplayDialog("AB Group", $"分组 [{_newGroupName}] 已存在。", "OK");
                return;
            }

            _database.EnsureGroup(
                _newGroupName.Trim(),
                string.IsNullOrWhiteSpace(_newGroupCodeName) ? _newGroupName.Trim() : _newGroupCodeName.Trim(),
                _newGroupDescription);
            _newGroupName = string.Empty;
            _newGroupCodeName = string.Empty;
            _newGroupDescription = string.Empty;
            SaveAndGenerate();
            SyncDefaultGroupPopup();
        }

        private void RenameGroupReferences(string oldName, string newName)
        {
            foreach (var mark in _database.marks)
            {
                if (mark != null && mark.group == oldName)
                    mark.group = newName;
            }

            if (_defaultGroup == oldName)
                _defaultGroup = newName;
        }

        private void ApplyDefaultsToFiltered()
        {
            _database.EnsureGroup(_defaultGroup);
            foreach (var mark in _database.marks)
            {
                if (!string.IsNullOrEmpty(_filter) &&
                    mark.assetPath.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                mark.bundleName = _defaultBundleName;
                mark.group = _defaultGroup;
            }

            SaveAndGenerate();
        }

        private void ApplySuggestedAddressesToFiltered()
        {
            var existing = new List<string>();
            foreach (var mark in _database.marks)
            {
                if (mark == null)
                    continue;

                var isFiltered = string.IsNullOrEmpty(_filter) ||
                                 mark.assetPath.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0;
                if (!isFiltered && !string.IsNullOrEmpty(mark.address))
                    existing.Add(mark.address);
            }

            foreach (var mark in _database.marks)
            {
                if (mark == null)
                    continue;

                if (!string.IsNullOrEmpty(_filter) &&
                    mark.assetPath.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                mark.address = ABAddressCodeGen.SuggestAddress(mark.assetPath, existing);
                mark.codeName = ABAddressCodeGen.SuggestCodeName(mark.assetPath);
                existing.Add(mark.address);
            }

            SaveAndGenerate();
        }

        private void SaveAndGenerate()
        {
            _database.EnsureGroupsSynced();
            EditorUtility.SetDirty(_database);
            AssetDatabase.SaveAssets();
            ABAddressCodeGen.Generate(_database);
        }

        private void SyncDefaultGroupPopup()
        {
            var names = _database.GetGroupNames();
            if (Array.IndexOf(names, _defaultGroup) < 0)
                _defaultGroup = names.Length > 0 ? names[0] : ABDefine.DefaultGroup;
        }

        private static string GuessBundleName(string assetPath)
        {
            var parts = assetPath.Replace('\\', '/').Split('/');
            if (parts.Length >= 2)
                return parts[1].ToLowerInvariant();
            return Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();
        }
    }
}
