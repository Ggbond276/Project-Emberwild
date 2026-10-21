using System;
using System.Collections.Generic;
using System.Linq;
using Emberwild.AI.Enemy.Ability;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Emberwild.Editor.MonsterAI
{
    public partial class MonsterAIEditorWindow : EditorWindow
    {
        #region 界面常量 (Layout Constants)
        private const float SidebarWidth = 250f;
        private const float HeaderButtonWidth = 190f;
        private static readonly string[] Tabs =
        {
            "基础与躯体",
            "大脑决策树",
            "技能与打击盒"
        };
        #endregion

        #region 运行时数据 (Runtime Data)
        private MonsterAIEditorStore _store;
        private MonsterEditorProfile _selectedProfile;
        private ReorderableList _ruleList;
        private ReorderableList _conditionList;
        private MonsterBehaviorEditorRule _conditionListRule;
        private readonly List<MonsterAIEditorIssue> _issues =
            new List<MonsterAIEditorIssue>();
        #endregion

        #region 界面状态 (View State)
        private Vector2 _sidebarScroll;
        private Vector2 _mainScroll;
        private int _activeTab;
        private int _selectedRuleIndex = -1;
        private string _search = string.Empty;
        private string _statusMessage = string.Empty;
        private MessageType _statusType = MessageType.Info;
        #endregion

        #region Unity 窗口生命周期 (Window Lifecycle)
        [MenuItem("Emberwild/怪物 AI/怪物配置工作台")]
        private static void OpenWindow()
        {
            var window = GetWindow<MonsterAIEditorWindow>();
            window.titleContent = new GUIContent("怪物配置工作台");
            window.minSize = new Vector2(920f, 620f);
            window.Show();
        }

        /// <summary>窗口启用时读取七张配置表。</summary>
        private void OnEnable()
        {
            ReloadStore();
        }
        #endregion

        #region 主界面绘制 (Main Window)
        /// <summary>绘制左侧怪物列表与右侧工作区。</summary>
        private void OnGUI()
        {
            if (_store == null)
            {
                EditorGUILayout.HelpBox("配置尚未加载。", MessageType.Error);
                if (GUILayout.Button("重新读取配置"))
                    ReloadStore();
                return;
            }

            GUILayout.BeginHorizontal();
            DrawSidebar();
            GUILayout.BeginVertical();
            DrawMainHeader();
            DrawStatus();

            if (_selectedProfile == null)
            {
                EditorGUILayout.HelpBox("请在左侧选择一只怪物。", MessageType.Info);
            }
            else
            {
                _activeTab = GUILayout.Toolbar(_activeTab, Tabs, GUILayout.Height(28f));
                _mainScroll = EditorGUILayout.BeginScrollView(_mainScroll);
                DrawActiveTab();
                EditorGUILayout.EndScrollView();
            }

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        private void DrawSidebar()
        {
            GUILayout.BeginVertical(GUILayout.Width(SidebarWidth));
            EditorGUILayout.LabelField("怪物列表", EditorStyles.boldLabel);
            _search = EditorGUILayout.TextField(
                new GUIContent("搜索", "按 TID 或名称筛选"),
                _search);

            _sidebarScroll = EditorGUILayout.BeginScrollView(
                _sidebarScroll,
                GUILayout.ExpandHeight(true));
            foreach (var profile in FilteredProfiles())
            {
                string label = $"[{profile.TID}] {profile.DisplayName}";
                bool selected = profile == _selectedProfile;
                bool clicked = GUILayout.Toggle(
                    selected,
                    label,
                    "Button",
                    GUILayout.Height(28f));
                if (clicked && !selected)
                    SelectProfile(profile);
            }
            EditorGUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+ 新增怪物", GUILayout.Height(28f)))
            {
                var profile = _store.AddMonster();
                SelectProfile(profile);
                SetStatus("已创建最小合法怪物模板，请填写资源路径。", MessageType.Info);
            }

            using (new EditorGUI.DisabledScope(_selectedProfile == null))
            {
                if (GUILayout.Button("- 删除选中", GUILayout.Height(28f)))
                    DeleteSelectedMonster();
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("重新读取磁盘", GUILayout.Height(24f)))
            {
                if (!_store.IsDirty || EditorUtility.DisplayDialog(
                        "放弃未保存修改",
                        "重新读取会丢弃当前未保存的修改，是否继续？",
                        "重新读取",
                        "取消"))
                {
                    ReloadStore();
                }
            }

            EditorGUILayout.HelpBox(
                "此工具修改 Data/Data。再次运行 Excel2Json 时，"
                + "若检测到工作台保存后的 JSON 改动，将阻止覆盖。",
                MessageType.Warning);
            GUILayout.EndVertical();
        }

        private void DrawMainHeader()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            string title = _selectedProfile == null
                ? "怪物配置工作台"
                : $"[{_selectedProfile.TID}] {_selectedProfile.DisplayName}";
            GUILayout.Label(title, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(
                    "校验数据合法性",
                    EditorStyles.toolbarButton,
                    GUILayout.Width(130f)))
            {
                ValidateAndReport();
            }

            GUI.backgroundColor = new Color(0.55f, 0.9f, 0.55f);
            if (GUILayout.Button(
                    "保存全部表并刷新资源",
                    GUI.skin.button,
                    GUILayout.Width(HeaderButtonWidth)))
            {
                SaveAllTables();
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();
        }

        private void DrawStatus()
        {
            if (!string.IsNullOrEmpty(_statusMessage))
                EditorGUILayout.HelpBox(_statusMessage, _statusType);

            if (_issues.Count == 0) return;
            int errors = _issues.Count(issue =>
                issue.Level == MonsterAIEditorIssueLevel.Error);
            int warnings = _issues.Count - errors;
            EditorGUILayout.HelpBox(
                $"校验结果：{errors} 个错误，{warnings} 个警告。",
                errors > 0 ? MessageType.Error : MessageType.Warning);

            for (int i = 0; i < Mathf.Min(_issues.Count, 8); i++)
            {
                var issue = _issues[i];
                EditorGUILayout.HelpBox(
                    FormatIssue(issue),
                    issue.Level == MonsterAIEditorIssueLevel.Error
                        ? MessageType.Error
                        : MessageType.Warning);
            }
            if (_issues.Count > 8)
                EditorGUILayout.LabelField($"另有 {_issues.Count - 8} 条问题，详见 Console。");
        }

        private void DrawActiveTab()
        {
            switch (_activeTab)
            {
                case 0:
                    DrawBasicTab();
                    break;
                case 1:
                    DrawBehaviorTab();
                    break;
                case 2:
                    DrawAbilitiesTab();
                    break;
            }
        }
        #endregion

        #region 保存与校验 (Save & Validation)
        private void SaveAllTables()
        {
            if (!ValidateAndReport()) return;

            try
            {
                _store.SaveAll();
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                ReloadRuntimeData();
                SetStatus("七张配置表已保存，Assets 已刷新。", MessageType.Info);
            }
            catch (Exception exception)
            {
                SetStatus($"保存失败：{exception.Message}", MessageType.Error);
                Debug.LogException(exception);
            }
        }

        private bool ValidateAndReport()
        {
            _issues.Clear();
            _issues.AddRange(MonsterAIEditorValidator.Validate(_store));
            int errors = _issues.Count(issue =>
                issue.Level == MonsterAIEditorIssueLevel.Error);
            int warnings = _issues.Count - errors;

            foreach (var issue in _issues)
                Debug.Log(FormatIssue(issue));

            if (errors > 0)
            {
                SetStatus(
                    $"发现 {errors} 个错误、{warnings} 个警告，请修正后再保存。",
                    MessageType.Error);
                return false;
            }

            SetStatus(
                warnings == 0 ? "校验通过。" : $"校验通过，有 {warnings} 个警告。",
                warnings == 0 ? MessageType.Info : MessageType.Warning);
            return true;
        }

        private static void ReloadRuntimeData()
        {
            try
            {
                MonsterAIDefinitionRepository.Reload();
                if (EditorApplication.isPlaying)
                {
                    Debug.LogWarning(
                        "[MonsterAIEditor] 已刷新数据缓存；场上已有怪物需要重新出生后才会使用新配置。");
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    $"[MonsterAIEditor] 文件已保存，但运行时缓存刷新失败：{exception.Message}");
            }
        }

        private string FormatIssue(MonsterAIEditorIssue issue)
        {
            string prefix = issue.TID > 0 ? $"[TID {issue.TID}] " : string.Empty;
            string level = issue.Level == MonsterAIEditorIssueLevel.Error
                ? "错误"
                : "警告";
            return $"{level} {prefix}{issue.Message}";
        }
        #endregion

        #region 怪物选择与基础操作 (Selection & Actions)
        private IEnumerable<MonsterEditorProfile> FilteredProfiles()
        {
            string query = _search?.Trim() ?? string.Empty;
            IEnumerable<MonsterEditorProfile> profiles = _store.Monsters;
            if (string.IsNullOrEmpty(query)) return profiles;
            return profiles.Where(profile =>
                profile.TID.ToString().Contains(query)
                || (profile.DisplayName ?? string.Empty).IndexOf(
                    query,
                    StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private void SelectProfile(MonsterEditorProfile profile)
        {
            _selectedProfile = profile;
            _selectedRuleIndex = profile != null && profile.Rules.Count > 0 ? 0 : -1;
            _ruleList = null;
            _conditionList = null;
            _conditionListRule = null;
            _mainScroll = Vector2.zero;
            Repaint();
        }

        private void DeleteSelectedMonster()
        {
            var profile = _selectedProfile;
            if (profile == null) return;
            if (!EditorUtility.DisplayDialog(
                    "删除怪物",
                    $"确定删除 TID {profile.TID} 的所有能力、行为、条件和动画配置吗？\n\n"
                    + "共享的攻击组不会自动删除。",
                    "删除",
                    "取消"))
            {
                return;
            }

            _store.DeleteMonster(profile);
            SelectProfile(_store.Monsters.FirstOrDefault());
            SetStatus("怪物已从工作区删除，点击“保存全部表”后写入磁盘。", MessageType.Warning);
        }

        private void ReloadStore()
        {
            _store = new MonsterAIEditorStore();
            _issues.Clear();
            _ruleList = null;
            _conditionList = null;
            _conditionListRule = null;

            if (!_store.Load(out string error))
            {
                SetStatus(error, MessageType.Error);
                _selectedProfile = null;
                return;
            }

            SelectProfile(_store.Monsters.FirstOrDefault());
            SetStatus($"已读取 {_store.Monsters.Count} 只怪物。", MessageType.Info);
        }

        private void SetStatus(string message, MessageType type)
        {
            _statusMessage = message;
            _statusType = type;
            Repaint();
        }
        #endregion

        #region 下拉与布局工具 (Dropdown & Layout Helpers)
        /// <summary>绘制字符串选项下拉并返回选中值。</summary>
        private static string DrawOptionPopup(
            string label,
            string value,
            MonsterAIEditorOption[] options)
        {
            int index = MonsterAIEditorOptions.IndexOf(options, value);
            if (index < 0)
            {
                var values = new List<string> { value ?? string.Empty };
                var labels = new List<string>
                {
                    string.IsNullOrWhiteSpace(value) ? "请选择" : $"未知：{value}"
                };
                values.AddRange(options.Select(option => option.Value));
                labels.AddRange(options.Select(option => option.Label));
                int selected = EditorGUILayout.Popup(label, 0, labels.ToArray());
                return values[selected];
            }

            int next = EditorGUILayout.Popup(
                label,
                index,
                options.Select(option => option.Label).ToArray());
            return options[next].Value;
        }

        private static string DrawAbilityPopup(
            string label,
            string value,
            MonsterEditorProfile profile,
            bool allowEmpty)
        {
            var values = new List<string> { string.Empty };
            var labels = new List<string> { allowEmpty ? "无" : "请选择" };
            values.AddRange(profile.Abilities.Select(row => row.AbilityId));
            labels.AddRange(profile.Abilities.Select(row =>
            {
                string slotLabel = MonsterAIEditorOptions.LabelOf(
                    MonsterAIEditorOptions.Slots,
                    row.Slot);
                return $"{row.AbilityId} ({slotLabel})";
            }));

            int index = values.FindIndex(item => string.Equals(
                item,
                value,
                StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                values.Add(value ?? string.Empty);
                labels.Add($"缺失：{value}");
                index = values.Count - 1;
            }

            int next = EditorGUILayout.Popup(label, index, labels.ToArray());
            return values[next];
        }

        private static string DrawSlotPopup(
            string label,
            string value,
            MonsterEditorProfile profile)
        {
            var used = profile.Abilities
                .Where(row => !string.IsNullOrWhiteSpace(row.Slot))
                .Select(row => row.Slot)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var values = new List<string> { string.Empty };
            var labels = new List<string> { "无" };
            values.AddRange(used);
            labels.AddRange(used.Select(slot =>
                MonsterAIEditorOptions.LabelOf(MonsterAIEditorOptions.Slots, slot)));

            int index = values.FindIndex(item => string.Equals(
                item,
                value,
                StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                values.Add(value ?? string.Empty);
                labels.Add($"缺失：{value}");
                index = values.Count - 1;
            }

            int next = EditorGUILayout.Popup(label, index, labels.ToArray());
            return values[next];
        }

        private static void DrawSection(string title)
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical("box");
        }

        private static void EndSection()
        {
            EditorGUILayout.EndVertical();
        }
        #endregion
    }
}
