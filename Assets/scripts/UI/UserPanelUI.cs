// ============================================================
// 文件名：UserPanelUI.cs
// 功  能：普通用户面板
//         - 个人信息展示
//         - 我的实验记录（查看/开始新实验）
//         - 修改密码
// 作  者：化工虚拟仿真实验平台
// ============================================================

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using ChemLab.Managers;
using ChemLab.Models;
using ChemLab.Utils;

namespace ChemLab.UI
{
    public class UserPanelUI : MonoBehaviour
    {
        private static readonly Color TAB_LABEL_SELECTED = new Color(0x7F / 255f, 0xD6 / 255f, 0xFD / 255f, 1f);
        private static readonly Color TAB_LABEL_NORMAL = Color.white;

        // ── 顶部信息 ──────────────────────────────────────────
        [Header("=== 顶部信息 ===")]
        public Text welcomeText;
        public Text userInfoText;
        public Button logoutBtn;

        // ── 主面板切换（Home/Profile） ─────────────────────────
        [Header("=== 主面板按钮（Home/Profile） ===")]
        public Button homeBtn;
        public Button profileBtn;

        [Header("=== 主面板（Home/Profile） ===")]
        public GameObject homePanel;
        public GameObject profilePanel;

        // ── Profile 子面板（Info/Record/ResetPassword） ───────
        [Header("=== Profile 子面板 Toggle ===")]
        public Toggle profileInfoToggle;
        public Toggle profileRecordToggle;
        public Toggle profileResetPasswordToggle;

        [Header("=== Profile 子面板 ===")]
        public GameObject profileInfoPanel;
        public GameObject profileRecordPanel;
        public GameObject profileResetPasswordPanel;

        // ── 首页面板 ──────────────────────────────────────────
        [Header("=== 首页 ===")]
        public Text homeWelcomeText;
        public Text myRecordCountText;
        public Text myCompletedCountText;
        public Text myAvgScoreText;
        public Text lastLoginText;

        // ── 实验入口（动态生成） ─────────────────────────────
        [Header("=== 实验入口 ===")]
        public Transform experimentEntryContent;
        public GameObject experimentEntryPrefab;
        [Tooltip("默认实验图片（按顺序轮流显示；建议填 4 张）")]
        public Sprite[] defaultExperimentSprites;

        // ── Profile/Info 面板内容 ─────────────────────────────
        [Header("=== Profile/Info ===")]
        public InputField profileIdText;
        public InputField profileUsernameText;
        public InputField profileRealNameText;
        public InputField profileEmailText;
        public Text profileCompletedRecordCountText;
        public Text profileTotalAvgScoreText;
        public Text profileExperimentTypeCountText;

        // ── Profile/Record 面板内容 ───────────────────────────
        [Header("=== Profile/Record ===")]
        public Transform recordListContent;
        public GameObject recordItemPrefab;
        public TMP_InputField recordFilterStartTimeInput;
        public TMP_InputField recordFilterEndTimeInput;
        public Dropdown recordFilterExperimentDropdown;
        public Button recordFilterResetBtn;

        // 修改密码
        [Header("=== Profile/ResetPassword ===")]
        public InputField oldPasswordInput;
        public InputField newPasswordInput;
        public InputField confirmNewPasswordInput;
        public Button     changePasswordBtn;
        public Text       changePasswordMsg;

        // ── 私有变量 ──────────────────────────────────────────
        private List<ExperimentRecord> _myRecords = new List<ExperimentRecord>();
        private Coroutine _profileStatsAnimCoroutine;
        private int _defaultExperimentSpriteCursor;

        private static readonly Color COLOR_OK    = new Color(0.1f, 0.7f, 0.3f);
        private static readonly Color COLOR_ERROR = new Color(0.9f, 0.2f, 0.2f);

        // ─────────────────────────────────────────────────────
        #region Unity 生命周期
        // ─────────────────────────────────────────────────────

        private void Awake()
        {
            if (logoutBtn != null) logoutBtn.onClick.AddListener(OnLogout);

            // 主面板按钮：Home/Profile
            if (homeBtn != null) homeBtn.onClick.AddListener(() => SwitchMainPanel(0));
            if (profileBtn != null) profileBtn.onClick.AddListener(() => SwitchMainPanel(1));

            // Profile 子面板 Toggle + label 变色
            if (profileInfoToggle != null)
                profileInfoToggle.onValueChanged.AddListener(isOn =>
                {
                    UpdateToggleLabelColor(profileInfoToggle, isOn);
                    if (isOn) SwitchProfileTab(0);
                });
            if (profileRecordToggle != null)
                profileRecordToggle.onValueChanged.AddListener(isOn =>
                {
                    UpdateToggleLabelColor(profileRecordToggle, isOn);
                    if (isOn) SwitchProfileTab(1);
                });
            if (profileResetPasswordToggle != null)
                profileResetPasswordToggle.onValueChanged.AddListener(isOn =>
                {
                    UpdateToggleLabelColor(profileResetPasswordToggle, isOn);
                    if (isOn) SwitchProfileTab(2);
                });

            // 修改密码
            if (changePasswordBtn != null)
                changePasswordBtn.onClick.AddListener(OnChangePassword);

            // Record 筛选控件：变化时刷新列表
            if (recordFilterStartTimeInput != null)
                recordFilterStartTimeInput.onEndEdit.AddListener(_ => RefreshRecords());
            if (recordFilterEndTimeInput != null)
                recordFilterEndTimeInput.onEndEdit.AddListener(_ => RefreshRecords());
            if (recordFilterExperimentDropdown != null)
                recordFilterExperimentDropdown.onValueChanged.AddListener(_ => RefreshRecords());
            if (recordFilterResetBtn != null)
                recordFilterResetBtn.onClick.AddListener(OnResetRecordFilters);

            // 初始化 Profile 子 Toggle label 颜色
            UpdateAllProfileToggleLabelColors();

            // Profile/Info：只读展示（InputField 不可交互）
            SetReadonly(profileIdText);
            SetReadonly(profileUsernameText);
            SetReadonly(profileRealNameText);
            SetReadonly(profileEmailText);
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region 面板显示回调
        // ─────────────────────────────────────────────────────

        public void OnPanelShow()
        {
            var user = DataManager.Instance.CurrentUser;
            if (user == null) return;

            if (welcomeText  != null) welcomeText.text  = $"欢迎，{user.realName}";
            if (userInfoText != null) userInfoText.text = $"账号：{user.username}";

            // 默认主面板：Home
            SwitchMainPanel(0);
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region 主面板/子面板切换
        // ─────────────────────────────────────────────────────

        /// <summary>0=Home，1=Profile</summary>
        private void SwitchMainPanel(int index)
        {
            if (homePanel    != null) homePanel.SetActive(index == 0);
            if (profilePanel != null) profilePanel.SetActive(index == 1);

            if (index == 0)
            {
                RefreshHome();
                return;
            }

            // 进入 Profile 时默认 Info
            if (profileInfoToggle != null) profileInfoToggle.isOn = true;
            SwitchProfileTab(0);
        }

        /// <summary>0=Info，1=Record，2=ResetPassword</summary>
        private void SwitchProfileTab(int index)
        {
            if (profileInfoPanel != null) profileInfoPanel.SetActive(index == 0);
            if (profileRecordPanel != null) profileRecordPanel.SetActive(index == 1);
            if (profileResetPasswordPanel != null) profileResetPasswordPanel.SetActive(index == 2);

            if (profileInfoToggle != null && profileInfoToggle.isOn != (index == 0)) profileInfoToggle.isOn = (index == 0);
            if (profileRecordToggle != null && profileRecordToggle.isOn != (index == 1)) profileRecordToggle.isOn = (index == 1);
            if (profileResetPasswordToggle != null && profileResetPasswordToggle.isOn != (index == 2)) profileResetPasswordToggle.isOn = (index == 2);
            UpdateAllProfileToggleLabelColors();

            switch (index)
            {
                case 0: RefreshProfile(); break;
                case 1: RefreshRecords(); break;
                case 2: RefreshProfile(); break; // ResetPassword 也需要刷新/清空输入框
            }
        }

        private void UpdateAllProfileToggleLabelColors()
        {
            if (profileInfoToggle != null) UpdateToggleLabelColor(profileInfoToggle, profileInfoToggle.isOn);
            if (profileRecordToggle != null) UpdateToggleLabelColor(profileRecordToggle, profileRecordToggle.isOn);
            if (profileResetPasswordToggle != null) UpdateToggleLabelColor(profileResetPasswordToggle, profileResetPasswordToggle.isOn);
        }

        private static void UpdateToggleLabelColor(Toggle toggle, bool isOn)
        {
            if (toggle == null) return;
            var c = isOn ? TAB_LABEL_SELECTED : TAB_LABEL_NORMAL;

            var text = toggle.GetComponentInChildren<Text>(true);
            if (text != null) text.color = c;

            var tmp = toggle.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null) tmp.color = c;
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region 首页
        // ─────────────────────────────────────────────────────

        private void RefreshHome()
        {
            var user = DataManager.Instance.CurrentUser;
            if (user == null) return;

            _myRecords = DataManager.Instance.GetRecordsByUser(user.userId);

            int scoredCount = _myRecords.FindAll(r => r != null && r.score > 0f).Count;
            float avgScore = 0f;
            if (scoredCount > 0)
            {
                float total = 0f;
                _myRecords.ForEach(r => { if (r != null && r.score > 0f) total += r.score; });
                avgScore = total / scoredCount;
            }

            if (homeWelcomeText     != null) homeWelcomeText.text     = $"你好，{user.realName}！欢迎使用化工虚拟仿真实验平台";
            if (myRecordCountText   != null) myRecordCountText.text   = $"实验次数\n{_myRecords.Count}";
            if (myCompletedCountText!= null) myCompletedCountText.text= $"已评分\n{scoredCount}";
            if (myAvgScoreText      != null) myAvgScoreText.text      = $"平均分\n{avgScore:F1}";
            if (lastLoginText       != null) lastLoginText.text       = $"上次登录：{user.lastLoginTime}";

            RefreshExperimentEntries();
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region 实验入口（动态生成）
        // ─────────────────────────────────────────────────────

        private void RefreshExperimentEntries()
        {
            if (experimentEntryContent == null) return;

            // 清空旧条目
            foreach (Transform child in experimentEntryContent)
                Destroy(child.gameObject);

            _defaultExperimentSpriteCursor = 0;

            List<ExperimentModel> experiments = null;
            try
            {
                experiments = DataManager.Instance.GetAllExperiments();
            }
            catch
            {
                experiments = null;
            }

            if (experiments == null || experiments.Count == 0)
            {
                CreateEmptyHint(experimentEntryContent, "暂无实验，请联系管理员添加实验。");
                return;
            }

            for (int i = 0; i < experiments.Count; i++)
            {
                var e = experiments[i];
                if (e == null) continue;
                CreateExperimentEntryItem(e);
            }
        }

        private void CreateExperimentEntryItem(ExperimentModel exp)
        {
            if (experimentEntryContent == null) return;

            GameObject item;
            if (experimentEntryPrefab != null)
            {
                item = Instantiate(experimentEntryPrefab, experimentEntryContent);
                var ui = item.GetComponent<ExperimentEntryItemUI>();
                if (ui != null)
                {
                    ui.SetTexts(exp.experimentName, exp.experimentDescription);
                    ui.SetSprite(ResolveExperimentSprite(exp.experimentImage));
                    if (ui.button != null)
                    {
                        ui.button.onClick.RemoveAllListeners();
                        ui.button.onClick.AddListener(() => OnStartExperiment(exp));
                    }
                }
                else
                {
                    // 兜底：如果预制体没挂脚本，则尝试用子 Text/Image 填充
                    var texts = item.GetComponentsInChildren<Text>(true);
                    if (texts.Length > 0) texts[0].text = exp.experimentName ?? "";
                    if (texts.Length > 1) texts[1].text = exp.experimentDescription ?? "";
                    var img = item.GetComponentInChildren<Image>(true);
                    if (img != null)
                    {
                        var sp = ResolveExperimentSprite(exp.experimentImage);
                        img.sprite = sp;
                        img.enabled = sp != null;
                    }
                    var btn = item.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => OnStartExperiment(exp));
                    }
                }
                return;
            }

            // 兜底：未配置 prefab 时，运行时动态创建一个简单条目按钮
            item = new GameObject($"ExperimentEntry_{exp.experimentId}");
            item.transform.SetParent(experimentEntryContent, false);

            var rect = item.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 90);

            var imgBg = item.AddComponent<Image>();
            imgBg.color = new Color(0.95f, 0.97f, 1f, 1f);

            var btn2 = item.AddComponent<Button>();
            btn2.onClick.AddListener(() => OnStartExperiment(exp));

            var layout = item.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 10, 10);
            layout.spacing = 6;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandHeight = false;

            AddTextToItem(item.transform, exp.experimentName ?? "", 16, Color.black, FontStyle.Bold);
            AddTextToItem(item.transform, exp.experimentDescription ?? "", 12, new Color(0.25f, 0.25f, 0.25f), FontStyle.Normal);
        }

        private static Sprite LoadExperimentSprite(string pathOrKey)
        {
            if (string.IsNullOrWhiteSpace(pathOrKey)) return null;
            // 约定：存 Resources 路径（不带扩展名），例如 "Experiments/absorbance"
            return Resources.Load<Sprite>(pathOrKey.Trim());
        }

        private Sprite ResolveExperimentSprite(string pathOrKey)
        {
            var sp = LoadExperimentSprite(pathOrKey);
            if (sp != null) return sp;
            return GetNextDefaultExperimentSprite();
        }

        private Sprite GetNextDefaultExperimentSprite()
        {
            if (defaultExperimentSprites == null || defaultExperimentSprites.Length == 0)
                return null;

            // 轮流显示（循环）
            int start = _defaultExperimentSpriteCursor;
            for (int i = 0; i < defaultExperimentSprites.Length; i++)
            {
                int idx = (start + i) % defaultExperimentSprites.Length;
                var sp = defaultExperimentSprites[idx];
                if (sp != null)
                {
                    _defaultExperimentSpriteCursor = (idx + 1) % defaultExperimentSprites.Length;
                    return sp;
                }
            }

            return null;
        }

        private void OnStartExperiment(ExperimentModel exp)
        {
            if (exp == null) return;
            string name = exp.experimentName ?? "";

            // 吸光度检验：进入 Assets/Lab/2/实验场景.unity（场景名：实验场景）
            if (string.Equals(name, "吸光度检验", StringComparison.Ordinal))
            {
                StartAndLoadSceneForExperiment(name, "实验场景");
                return;
            }

            OnStartExperiment(name, "");
        }

        private void StartAndLoadSceneForExperiment(string experimentName, string sceneName)
        {
            var user = DataManager.Instance.CurrentUser;
            if (user == null) return;

            UIManager.Instance.ShowConfirm(
                "开始实验",
                $"确定要开始【{experimentName}】吗？",
                () =>
                {
                    // 创建实验记录
                    var record = new ExperimentRecord(user.userId, experimentName);
                    DataManager.Instance.AddRecord(record);

                    UIManager.Instance.ShowToast($"实验【{experimentName}】已开始，正在进入场景…");

                    if (!Application.CanStreamedLevelBeLoaded(sceneName))
                    {
                        UIManager.Instance.ShowMessage(
                            "无法进入场景",
                            $"未能加载场景：{sceneName}\n请确认该场景已加入 Build Settings 的 Scenes In Build。"
                        );
                        return;
                    }

                    SceneManager.LoadScene(sceneName);
                }
            );
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region 实验记录
        // ─────────────────────────────────────────────────────

        private void RefreshRecords()
        {
            var user = DataManager.Instance.CurrentUser;
            if (user == null) return;

            _myRecords = DataManager.Instance.GetRecordsByUser(user.userId);

            // 更新下拉选项（包含“全部”）
            UpdateRecordExperimentDropdownOptions(_myRecords);

            var filtered = FilterRecords(_myRecords);

            if (recordListContent != null)
            {
                foreach (Transform child in recordListContent)
                    Destroy(child.gameObject);
            }

            if (filtered.Count == 0)
            {
                CreateEmptyHint(recordListContent, "暂无实验记录，快去开始第一个实验吧！");
                return;
            }

            foreach (var record in filtered)
                CreateRecordItem(record);
        }

        private void OnResetRecordFilters()
        {
            if (recordFilterStartTimeInput != null) recordFilterStartTimeInput.text = "";
            if (recordFilterEndTimeInput != null) recordFilterEndTimeInput.text = "";
            if (recordFilterExperimentDropdown != null)
            {
                // 约定：0=全部
                recordFilterExperimentDropdown.SetValueWithoutNotify(0);
                recordFilterExperimentDropdown.RefreshShownValue();
            }

            RefreshRecords();
        }

        private void UpdateRecordExperimentDropdownOptions(List<ExperimentRecord> records)
        {
            if (recordFilterExperimentDropdown == null) return;

            string previous = null;
            if (recordFilterExperimentDropdown.options != null &&
                recordFilterExperimentDropdown.value >= 0 &&
                recordFilterExperimentDropdown.value < recordFilterExperimentDropdown.options.Count)
            {
                previous = recordFilterExperimentDropdown.options[recordFilterExperimentDropdown.value].text;
            }

            // 实验名称：直接从 experiments 表读取（而不是从 records 去重）
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var experiments = DataManager.Instance.GetAllExperiments();
                if (experiments != null)
                {
                    foreach (var e in experiments)
                    {
                        if (e == null) continue;
                        string name = (e.experimentName ?? "").Trim();
                        if (!string.IsNullOrEmpty(name))
                            set.Add(name);
                    }
                }
            }
            catch
            {
                // 兜底：若实验表暂不可用，则回退到 records 去重，保证下拉不为空
                if (records != null)
                {
                    foreach (var r in records)
                    {
                        if (r == null) continue;
                        string name = (r.experimentName ?? "").Trim();
                        if (!string.IsNullOrEmpty(name))
                            set.Add(name);
                    }
                }
            }

            var options = new List<Dropdown.OptionData>();
            options.Add(new Dropdown.OptionData("全部"));
            var names = new List<string>(set);
            names.Sort(StringComparer.OrdinalIgnoreCase);
            foreach (var name in names)
                options.Add(new Dropdown.OptionData(name));

            recordFilterExperimentDropdown.options = options;

            int idx = 0;
            if (!string.IsNullOrEmpty(previous))
            {
                for (int i = 0; i < options.Count; i++)
                {
                    if (options[i].text == previous) { idx = i; break; }
                }
            }

            recordFilterExperimentDropdown.SetValueWithoutNotify(idx);
            recordFilterExperimentDropdown.RefreshShownValue();
        }

        private List<ExperimentRecord> FilterRecords(List<ExperimentRecord> records)
        {
            var result = new List<ExperimentRecord>();
            if (records == null) return result;

            DateTime? start = TryParseFilterTime(recordFilterStartTimeInput != null ? recordFilterStartTimeInput.text : null);
            DateTime? end = TryParseFilterTime(recordFilterEndTimeInput != null ? recordFilterEndTimeInput.text : null);

            string selectedExperiment = null;
            if (recordFilterExperimentDropdown != null &&
                recordFilterExperimentDropdown.options != null &&
                recordFilterExperimentDropdown.value >= 0 &&
                recordFilterExperimentDropdown.value < recordFilterExperimentDropdown.options.Count)
            {
                selectedExperiment = recordFilterExperimentDropdown.options[recordFilterExperimentDropdown.value].text;
            }
            bool filterByExperiment = !string.IsNullOrEmpty(selectedExperiment) && selectedExperiment != "全部";

            foreach (var r in records)
            {
                if (r == null) continue;

                if (filterByExperiment && r.experimentName != selectedExperiment)
                    continue;

                if ((start.HasValue || end.HasValue) && !string.IsNullOrEmpty(r.recordTime))
                {
                    if (TryParseRecordTime(r.recordTime, out var rt))
                    {
                        if (start.HasValue && rt < start.Value) continue;
                        if (end.HasValue && rt > end.Value) continue;
                    }
                    // 解析失败：不做时间过滤（保留该条）
                }

                result.Add(r);
            }

            return result;
        }

        private static DateTime? TryParseFilterTime(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;

            if (DateTime.TryParse(input, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out var dt))
                return dt;

            var formats = new[]
            {
                "yyyy-MM-dd",
                "yyyy/MM/dd",
                "yyyy-MM-dd HH:mm",
                "yyyy-MM-dd HH:mm:ss",
                "yyyy/MM/dd HH:mm",
                "yyyy/MM/dd HH:mm:ss",
            };

            if (DateTime.TryParseExact(input.Trim(), formats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out dt))
                return dt;

            return null;
        }

        private static bool TryParseRecordTime(string input, out DateTime dt)
        {
            // 记录时间在项目里通常是 "yyyy-MM-dd HH:mm:ss"
            var formats = new[] { "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm", "yyyy-MM-dd" };
            if (DateTime.TryParseExact(input, formats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out dt))
                return true;

            return DateTime.TryParse(input, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out dt);
        }

        private void CreateRecordItem(ExperimentRecord record)
        {
            if (recordListContent == null) return;

            GameObject item;

            if (recordItemPrefab != null)
            {
                item = Instantiate(recordItemPrefab, recordListContent);
                var texts = item.GetComponentsInChildren<Text>();
                if (texts.Length >= 4)
                {
                    texts[0].text = record.recordId;
                    texts[1].text = record.experimentName;
                    texts[2].text = record.recordTime;
                    texts[3].text = record.score > 0f ? $"{record.score:F1}" : "未评分";
                }
            }
            else
            {
                // 动态创建
                item = new GameObject($"RecordItem_{record.recordId}");
                item.transform.SetParent(recordListContent, false);

                var rect = item.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(0, 80);

                var layout = item.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(15, 15, 8, 8);
                layout.spacing = 4;
                layout.childForceExpandWidth  = true;
                layout.childForceExpandHeight = false;

                var bg = item.AddComponent<Image>();
                bg.color = record.score > 0f
                    ? new Color(0.93f, 0.97f, 0.93f, 1f)
                    : new Color(0.97f, 0.97f, 0.93f, 1f);

                // 第一行：实验名称 + 得分
                string scoreStr = record.score > 0f ? $"得分：{record.score:F1}" : "【未评分】";
                AddTextToItem(item.transform, $"📋 {record.experimentName}  {scoreStr}",
                    15, Color.black, FontStyle.Bold);

                // 第二行：时间
                AddTextToItem(item.transform, $"记录时间：{record.recordTime}", 12, Color.gray, FontStyle.Normal);
            }
        }

        private void AddTextToItem(Transform parent, string content, int fontSize,
                                    Color color, FontStyle style)
        {
            var obj = new GameObject("Text");
            obj.transform.SetParent(parent, false);
            var text = obj.AddComponent<Text>();
            text.text      = content;
            text.font      = UIFont.Get();
            text.fontSize  = fontSize;
            text.color     = color;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleLeft;

            var rect = obj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, fontSize + 8);
        }

        private void CreateEmptyHint(Transform parent, string hint)
        {
            if (parent == null) return;
            var obj = new GameObject("EmptyHint");
            obj.transform.SetParent(parent, false);
            var text = obj.AddComponent<Text>();
            text.text      = hint;
            text.font      = UIFont.Get();
            text.fontSize  = 16;
            text.color     = Color.gray;
            text.alignment = TextAnchor.MiddleCenter;
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region 个人信息
        // ─────────────────────────────────────────────────────

        private void RefreshProfile()
        {
            var user = DataManager.Instance.CurrentUser;
            if (user == null) return;

            _myRecords = DataManager.Instance.GetRecordsByUser(user.userId);

            int completedCount = _myRecords != null ? _myRecords.Count : 0;
            int scoredCount = 0;
            float avgScore = 0f;
            var experimentTypes = new HashSet<string>();

            if (_myRecords != null)
            {
                foreach (var r in _myRecords)
                {
                    if (r == null) continue;

                    if (!string.IsNullOrEmpty(r.experimentName))
                        experimentTypes.Add(r.experimentName);

                    if (r.score > 0f)
                    {
                        scoredCount++;
                        avgScore += r.score;
                    }
                }
            }

            if (scoredCount > 0) avgScore /= scoredCount;

            if (profileIdText        != null) profileIdText.text        = $"ID：{user.userId}";
            if (profileUsernameText  != null) profileUsernameText.text  = $"用户名：{user.username}";
            if (profileRealNameText  != null) profileRealNameText.text  = $"真实姓名：{user.realName}";
            if (profileEmailText     != null) profileEmailText.text     = $"邮箱：{(string.IsNullOrEmpty(user.email) ? "未填写" : user.email)}";
            PlayProfileStatsAnimation(completedCount, avgScore, experimentTypes.Count, 1f);

            // 清空密码修改框
            if (oldPasswordInput        != null) oldPasswordInput.text        = "";
            if (newPasswordInput        != null) newPasswordInput.text        = "";
            if (confirmNewPasswordInput != null) confirmNewPasswordInput.text = "";
            if (changePasswordMsg       != null) changePasswordMsg.text       = "";
        }

        private void PlayProfileStatsAnimation(int completedCount, float avgScore, int experimentTypeCount, float durationSeconds)
        {
            if (_profileStatsAnimCoroutine != null)
            {
                StopCoroutine(_profileStatsAnimCoroutine);
                _profileStatsAnimCoroutine = null;
            }

            // 先置 0，保证“从 0 增长”的视觉效果
            if (profileCompletedRecordCountText != null) profileCompletedRecordCountText.text = "0";
            if (profileTotalAvgScoreText        != null) profileTotalAvgScoreText.text        = "0.0";
            if (profileExperimentTypeCountText  != null) profileExperimentTypeCountText.text  = "0";

            _profileStatsAnimCoroutine = StartCoroutine(AnimateProfileStats(completedCount, avgScore, experimentTypeCount, durationSeconds));
        }

        private IEnumerator AnimateProfileStats(int targetCompletedCount, float targetAvgScore, int targetExperimentTypeCount, float durationSeconds)
        {
            durationSeconds = Mathf.Max(0.0001f, durationSeconds);

            float t = 0f;
            while (t < durationSeconds)
            {
                t += Time.deltaTime;
                float p = Mathf.Clamp01(t / durationSeconds);

                int completedNow = Mathf.RoundToInt(Mathf.Lerp(0f, targetCompletedCount, p));
                float avgNow = Mathf.Lerp(0f, targetAvgScore, p);
                int typeNow = Mathf.RoundToInt(Mathf.Lerp(0f, targetExperimentTypeCount, p));

                if (profileCompletedRecordCountText != null) profileCompletedRecordCountText.text = $"{completedNow}";
                if (profileTotalAvgScoreText        != null) profileTotalAvgScoreText.text        = $"{avgNow:F1}";
                if (profileExperimentTypeCountText  != null) profileExperimentTypeCountText.text  = $"{typeNow}";

                yield return null;
            }

            // 收尾：确保最终值准确
            if (profileCompletedRecordCountText != null) profileCompletedRecordCountText.text = $"{targetCompletedCount}";
            if (profileTotalAvgScoreText        != null) profileTotalAvgScoreText.text        = $"{targetAvgScore:F1}";
            if (profileExperimentTypeCountText  != null) profileExperimentTypeCountText.text  = $"{targetExperimentTypeCount}";

            _profileStatsAnimCoroutine = null;
        }

        private void OnChangePassword()
        {
            if (changePasswordMsg != null) changePasswordMsg.text = "";

            var user = DataManager.Instance.CurrentUser;
            if (user == null) return;

            string oldPwd     = oldPasswordInput        != null ? oldPasswordInput.text        : "";
            string newPwd     = newPasswordInput        != null ? newPasswordInput.text        : "";
            string confirmPwd = confirmNewPasswordInput != null ? confirmNewPasswordInput.text : "";

            // 验证旧密码
            if (DataManager.MD5Encrypt(oldPwd) != user.password)
            {
                SetChangeMsg("原密码错误！", COLOR_ERROR);
                return;
            }

            if (string.IsNullOrEmpty(newPwd) || newPwd.Length < 6)
            {
                SetChangeMsg("新密码长度不能少于6位！", COLOR_ERROR);
                return;
            }

            if (newPwd != confirmPwd)
            {
                SetChangeMsg("两次新密码输入不一致！", COLOR_ERROR);
                return;
            }

            if (newPwd == oldPwd)
            {
                SetChangeMsg("新密码不能与原密码相同！", COLOR_ERROR);
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            UIManager.Instance.ShowLoading("正在修改密码...");
            StartCoroutine(DataManager.Instance.ResetPasswordAsync(user.userId, newPwd, (ok, err) =>
            {
                UIManager.Instance.HideLoading();
                if (ok)
                {
                    SetChangeMsg("✓ 密码修改成功！", COLOR_OK);
                    if (oldPasswordInput        != null) oldPasswordInput.text        = "";
                    if (newPasswordInput        != null) newPasswordInput.text        = "";
                    if (confirmNewPasswordInput != null) confirmNewPasswordInput.text = "";
                }
                else
                {
                    SetChangeMsg(err, COLOR_ERROR);
                }
            }));
#else
            bool ok = DataManager.Instance.ResetPassword(user.userId, newPwd, out string err);
            if (ok)
            {
                SetChangeMsg("✓ 密码修改成功！", COLOR_OK);
                if (oldPasswordInput        != null) oldPasswordInput.text        = "";
                if (newPasswordInput        != null) newPasswordInput.text        = "";
                if (confirmNewPasswordInput != null) confirmNewPasswordInput.text = "";
            }
            else
            {
                SetChangeMsg(err, COLOR_ERROR);
            }
#endif
        }

        private void SetChangeMsg(string msg, Color color)
        {
            if (changePasswordMsg == null) return;
            changePasswordMsg.text  = msg;
            changePasswordMsg.color = color;
        }

        private static void SetReadonly(InputField input)
        {
            if (input == null) return;
            input.interactable = false;
            // 仍然允许显示/选择文本（不同 Unity 版本表现不同，至少禁用编辑）
            input.readOnly = true;
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region 开始实验
        // ─────────────────────────────────────────────────────

        private void OnStartExperiment(string experimentName, string experimentType)
        {
            var user = DataManager.Instance.CurrentUser;
            if (user == null) return;

            UIManager.Instance.ShowConfirm(
                "开始实验",
                $"确定要开始【{experimentName}】吗？",
                () =>
                {
                    // 创建实验记录
                    var record = new ExperimentRecord(user.userId, experimentName);
#if UNITY_WEBGL && !UNITY_EDITOR
                    UIManager.Instance.ShowLoading("正在创建记录...");
                    StartCoroutine(DataManager.Instance.AddRecordAsync(record, (ok, err) =>
                    {
                        UIManager.Instance.HideLoading();
                        if (!ok)
                        {
                            UIManager.Instance.ShowMessage("创建记录失败", err);
                            return;
                        }
                        UIManager.Instance.ShowToast($"实验【{experimentName}】已开始，祝实验顺利！");
                        SimulateExperimentComplete(record.recordId, experimentName);
                    }));
                    return;
#else
                    DataManager.Instance.AddRecord(record);
#endif

                    UIManager.Instance.ShowToast($"实验【{experimentName}】已开始，祝实验顺利！");

                    // TODO: 此处可加载对应实验场景
                    // SceneManager.LoadScene(experimentName);

                    // 模拟实验完成（演示用，实际应在实验场景中完成后回调）
                    SimulateExperimentComplete(record.recordId, experimentName);
                }
            );
        }

        /// <summary>模拟实验完成（演示用）</summary>
        private void SimulateExperimentComplete(string recordId, string experimentName)
        {
            StartCoroutine(SimulateCoroutine(recordId, experimentName));
        }

        private System.Collections.IEnumerator SimulateCoroutine(string recordId, string experimentName)
        {
            yield return new UnityEngine.WaitForSeconds(2f);

            float score  = UnityEngine.Random.Range(60f, 100f);
            string result = score >= 90 ? "实验操作规范，结果优秀！"
                          : score >= 75 ? "实验基本完成，有少量误差。"
                          : "实验完成，建议复习相关知识点。";

#if UNITY_WEBGL && !UNITY_EDITOR
            UIManager.Instance.ShowLoading("正在提交成绩...");
            bool ok2 = false;
            string err2 = "";
            yield return StartCoroutine(DataManager.Instance.CompleteRecordAsync(recordId, score, (ok, err) =>
            {
                ok2 = ok;
                err2 = err;
            }));
            UIManager.Instance.HideLoading();
            if (!ok2)
            {
                UIManager.Instance.ShowMessage("提交失败", err2);
                yield break;
            }
            UIManager.Instance.ShowMessage(
                "实验完成",
                $"【{experimentName}】实验完成！\n得分：{score:F1} 分\n{result}",
                () => RefreshRecords()
            );
#else
            DataManager.Instance.CompleteRecord(recordId, score, result);
            UIManager.Instance.ShowMessage(
                "实验完成",
                $"【{experimentName}】实验完成！\n得分：{score:F1} 分\n{result}",
                () => RefreshRecords()
            );
#endif
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region 登出
        // ─────────────────────────────────────────────────────

        private void OnLogout()
        {
            UIManager.Instance.ShowConfirm(
                "确认退出",
                "确定要退出登录吗？",
                () =>
                {
                    StartCoroutine(DataManager.Instance.LogoutAsync((ok, err) =>
                    {
                        UIManager.Instance.ShowLoginPanel();
                        UIManager.Instance.ShowToast(ok ? "已安全退出登录" : ("退出失败：" + err));
                    }));
                }
            );
        }

        #endregion
    }
}
