// ============================================================
// 文件名：AdminPanelUI.cs
// 功  能：管理员面板
//         - 用户管理（查看/添加/删除/禁用/重置密码）
//         - 实验记录管理（查看全部/删除）
//         - 数据统计概览
// 作  者：化工虚拟仿真实验平台
// ============================================================

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using TMPro;
using ChemLab.Managers;
using ChemLab.Models;
using ChemLab.Labs;
using ChemLab.Utils;

namespace ChemLab.UI
{
    public class AdminPanelUI : MonoBehaviour
    {
        private static readonly Color TOGGLE_LABEL_SELECTED = new Color(0x7F / 255f, 0xD6 / 255f, 0xFD / 255f, 1f);
        private static readonly Color TOGGLE_LABEL_NORMAL = Color.white;

        // ── 顶部信息 ──────────────────────────────────────────
        [Header("=== 顶部信息 ===")]
        public Text welcomeText;
        public Text loginTimeText;
        public Button logoutBtn;

        // ── 页面切换（Toggle） ─────────────────────────────────
        [Header("=== 页面切换（Toggle） ===")]
        public Toggle toggleUser;
        public Toggle toggleRecord;

        // ── 各页面面板 ─────────────────────────────────────────
        [Header("=== 页面面板 ===")]
        public GameObject userManagePanel;
        public GameObject recordManagePanel;

        // ── 用户管理面板 ──────────────────────────────────────
        [Header("=== 用户管理 ===")]
        public Transform userListContent;       // ScrollView Content
        public GameObject userItemPrefab;       // 用户列表Item预制体
        public InputField searchUserIdInput;    // 用户ID
        public InputField searchRealNameInput;  // 真实姓名（模糊）
        public Dropdown   searchRoleDropdown;   // 角色：全部/管理员/普通用户
        public TMP_InputField searchCreateStartInput; // 创建时间起（可输入日期或日期时间）
        public TMP_InputField searchCreateEndInput;   // 创建时间止（可输入日期或日期时间）
        public Button searchUserBtn;
        public Button resetUserFilterBtn;
        public Button addUserBtn;

        // 添加用户子面板
        [Header("=== 添加用户子面板 ===")]
        public GameObject addUserSubPanel;
        public InputField addUsernameInput;
        public InputField addPasswordInput;
        public InputField addRealNameInput;
        public InputField addEmailInput;
        public Dropdown   addRoleDropdown;
        public Button     confirmAddUserBtn;
        public Button     cancelAddUserBtn;
        public Text       addUserErrorText;

        // 重置密码子面板
        [Header("=== 重置密码子面板 ===")]
        public GameObject resetPasswordSubPanel;
        public Text       resetUserIdText;      // 显示ID
        [FormerlySerializedAs("resetRealNameText")]
        public InputField resetRealNameInput;   // 可修改真实姓名（与注册校验一致）
        public InputField resetEmailInput;      // 可修改邮箱
        public Dropdown   resetRoleDropdown;    // 可修改角色（0=管理员，1=普通用户）
        public InputField newPasswordInput;
        public Button     confirmResetBtn;
        public Button     cancelResetBtn;
        public Text       resetErrorText;

        // ── 实验记录面板 ──────────────────────────────────────
        [Header("=== 实验记录管理 ===")]
        public Transform recordListContent;
        public GameObject recordItemPrefab;
        [Tooltip("按 userId 筛选（可精确或包含）")]
        public InputField searchRecordUserIdInput;

        [Tooltip("实验名称下拉框（自动从记录中生成选项）")]
        public Dropdown experimentNameDropdown;

        [Tooltip("起始日期/时间（TMP_InputField，支持 yyyy-MM-dd 或 yyyy-MM-dd HH:mm:ss）")]
        public TMP_InputField recordStartInput;

        [Tooltip("结束日期/时间（TMP_InputField，支持 yyyy-MM-dd 或 yyyy-MM-dd HH:mm:ss）")]
        public TMP_InputField recordEndInput;

        [Tooltip("（可选）通用关键词，匹配 recordId / userId / 实验名")]
        public InputField searchRecordInput;
        public Button     searchRecordBtn;
        public Button     resetRecordFilterBtn;
        public Text       recordCountText;

        // ── 私有变量 ──────────────────────────────────────────
        private List<UserModel>       _allUsers   = new List<UserModel>();
        private List<ExperimentRecord> _allRecords = new List<ExperimentRecord>();
        private string _pendingResetUserId = "";
        private LabManifest _labManifest;

        // ─────────────────────────────────────────────────────
        #region Unity 生命周期
        // ─────────────────────────────────────────────────────

        private void Awake()
        {
            // 顶部按钮
            if (logoutBtn != null) logoutBtn.onClick.AddListener(OnLogout);

            // 角色下拉框选项（用代码生成）
            InitRoleDropdownOptions();
            InitEditRoleDropdownOptions();
            InitAddRoleDropdownOptions();

            // 页面切换（Toggle）
            if (toggleUser != null)
                toggleUser.onValueChanged.AddListener(isOn =>
                {
                    UpdateToggleLabelColor(toggleUser, isOn);
                    if (isOn) SwitchPage(0);
                });
            if (toggleRecord != null)
                toggleRecord.onValueChanged.AddListener(isOn =>
                {
                    UpdateToggleLabelColor(toggleRecord, isOn);
                    if (isOn) SwitchPage(1);
                });

            // 用户管理
            if (searchUserBtn != null) searchUserBtn.onClick.AddListener(OnSearchUser);
            if (resetUserFilterBtn != null) resetUserFilterBtn.onClick.AddListener(OnResetUserFilters);
            if (addUserBtn    != null) addUserBtn.onClick.AddListener(OnShowAddUserPanel);

            // 添加用户子面板
            if (confirmAddUserBtn != null) confirmAddUserBtn.onClick.AddListener(OnConfirmAddUser);
            if (cancelAddUserBtn  != null) cancelAddUserBtn.onClick.AddListener(OnCancelAddUser);

            // 重置密码子面板
            if (confirmResetBtn != null) confirmResetBtn.onClick.AddListener(OnConfirmReset);
            if (cancelResetBtn  != null) cancelResetBtn.onClick.AddListener(OnCancelReset);

            // 实验记录
            if (searchRecordBtn != null) searchRecordBtn.onClick.AddListener(OnSearchRecord);
            if (resetRecordFilterBtn != null) resetRecordFilterBtn.onClick.AddListener(OnResetRecordFilters);

            // 初始隐藏子面板
            if (addUserSubPanel       != null) addUserSubPanel.SetActive(false);
            if (resetPasswordSubPanel != null) resetPasswordSubPanel.SetActive(false);

            // 初始化 Toggle Label 颜色（避免默认状态颜色不正确）
            if (toggleUser != null) UpdateToggleLabelColor(toggleUser, toggleUser.isOn);
            if (toggleRecord != null) UpdateToggleLabelColor(toggleRecord, toggleRecord.isOn);

            // 实验清单（用于实验名称下拉框配置）
            _labManifest = Resources.Load<LabManifest>("LabManifest");
        }

        private void InitAddRoleDropdownOptions()
        {
            if (addRoleDropdown == null) return;

            addRoleDropdown.ClearOptions();
            // 约定：0=普通用户，1=管理员（与 OnConfirmAddUser 的 value 判断一致）
            addRoleDropdown.AddOptions(new List<string> { "普通用户", "管理员" });
            addRoleDropdown.value = 0;
            addRoleDropdown.RefreshShownValue();
        }

        private void InitRoleDropdownOptions()
        {
            if (searchRoleDropdown == null) return;

            searchRoleDropdown.ClearOptions();
            searchRoleDropdown.AddOptions(new List<string>
            {
                "全部",
                "管理员",
                "普通用户"
            });
            searchRoleDropdown.value = 0;
            searchRoleDropdown.RefreshShownValue();
        }

        private void InitEditRoleDropdownOptions()
        {
            if (resetRoleDropdown == null) return;

            resetRoleDropdown.ClearOptions();
            resetRoleDropdown.AddOptions(new List<string> { "管理员", "普通用户" });
            resetRoleDropdown.value = 1;
            resetRoleDropdown.RefreshShownValue();
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region 面板显示回调
        // ─────────────────────────────────────────────────────

        public void OnPanelShow()
        {
            var user = DataManager.Instance.CurrentUser;
            if (user == null) return;

            if (welcomeText  != null) welcomeText.text  = $"管理员：{user.realName}（{user.username}）";
            if (loginTimeText != null) loginTimeText.text = $"登录时间：{user.lastLoginTime}";

            // 默认显示“用户数据”
            if (toggleUser != null) toggleUser.isOn = true;
            if (toggleUser != null) UpdateToggleLabelColor(toggleUser, toggleUser.isOn);
            if (toggleRecord != null) UpdateToggleLabelColor(toggleRecord, toggleRecord.isOn);
            SwitchPage(0);
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region 页面切换（Toggle）
        // ─────────────────────────────────────────────────────

        /// <summary>
        /// 0=用户数据，1=实验记录
        /// </summary>
        private void SwitchPage(int index)
        {
            if (userManagePanel   != null) userManagePanel.SetActive(index == 0);
            if (recordManagePanel != null) recordManagePanel.SetActive(index == 1);

            // 同步 Toggle 状态（避免外部直接调用时状态不同步）
            if (toggleUser != null && toggleUser.isOn != (index == 0)) toggleUser.isOn = (index == 0);
            if (toggleRecord != null && toggleRecord.isOn != (index == 1)) toggleRecord.isOn = (index == 1);

            if (toggleUser != null) UpdateToggleLabelColor(toggleUser, toggleUser.isOn);
            if (toggleRecord != null) UpdateToggleLabelColor(toggleRecord, toggleRecord.isOn);

            if (index == 0) RefreshUserList();
            else RefreshRecordList("");
        }

        private static void UpdateToggleLabelColor(Toggle toggle, bool isOn)
        {
            if (toggle == null) return;
            var c = isOn ? TOGGLE_LABEL_SELECTED : TOGGLE_LABEL_NORMAL;

            // 兼容 UGUI Text
            var text = toggle.GetComponentInChildren<Text>(true);
            if (text != null) text.color = c;

            // 兼容 TMP_Text
            var tmp = toggle.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null) tmp.color = c;
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region 用户管理
        // ─────────────────────────────────────────────────────

        private void RefreshUserList()
        {
            _allUsers = DataManager.Instance.GetAllUsers();

            // 清空列表
            if (userListContent != null)
            {
                foreach (Transform child in userListContent)
                    Destroy(child.gameObject);
            }

            foreach (var user in _allUsers)
            {
                if (!MatchesUserFilter(user)) continue;

                CreateUserItem(user);
            }
        }

        private bool MatchesUserFilter(UserModel user)
        {
            if (user == null) return false;

            // ID 精确（优先）/包含（兜底）
            string idKw = searchUserIdInput != null ? searchUserIdInput.text.Trim() : "";
            if (!string.IsNullOrEmpty(idKw))
            {
                string uid = user.userId ?? "";
                bool idMatch = uid.Equals(idKw, System.StringComparison.OrdinalIgnoreCase) ||
                               uid.Contains(idKw);
                if (!idMatch) return false;
            }

            // realName 模糊
            string realNameKw = searchRealNameInput != null ? searchRealNameInput.text.Trim() : "";
            if (!string.IsNullOrEmpty(realNameKw))
            {
                if (!(user.realName ?? "").Contains(realNameKw)) return false;
            }

            // role 下拉
            if (searchRoleDropdown != null && searchRoleDropdown.value > 0)
            {
                // 约定：0=全部，1=管理员，2=普通用户
                var want = searchRoleDropdown.value == 1 ? UserRole.Admin : UserRole.User;
                if (user.role != want) return false;
            }

            // 创建时间区间（包含边界）
            string startStr = searchCreateStartInput != null ? searchCreateStartInput.text.Trim() : "";
            string endStr   = searchCreateEndInput   != null ? searchCreateEndInput.text.Trim()   : "";

            bool hasStart = TryParseDateTimeFlexible(startStr, out var startDt);
            bool hasEnd   = TryParseDateTimeFlexible(endStr, out var endDt);

            if (hasStart || hasEnd)
            {
                if (!TryParseDateTimeFlexible(user.createTime, out var createDt))
                    return false;

                // 若只输入日期（无时间），则起始按 00:00:00，结束按 23:59:59.9999999
                if (hasStart && !startStr.Contains(":")) startDt = startDt.Date;
                if (hasEnd   && !endStr.Contains(":"))   endDt   = endDt.Date.AddDays(1).AddTicks(-1);

                if (hasStart && createDt < startDt) return false;
                if (hasEnd   && createDt > endDt)   return false;
            }

            return true;
        }

        private static string FormatDateYMD(string s)
        {
            if (TryParseDateTimeFlexible(s, out var dt))
                return dt.ToString("yyyy-MM-dd");
            return s ?? "";
        }

        private static bool TryParseDateTimeFlexible(string s, out System.DateTime dt)
        {
            dt = default;
            if (string.IsNullOrWhiteSpace(s)) return false;

            // 常见格式 + 兜底 TryParse
            string[] fmts =
            {
                "yyyy-MM-dd",
                "yyyy/M/d",
                "yyyy/MM/dd",
                "yyyy-MM-dd HH:mm",
                "yyyy-MM-dd HH:mm:ss",
                "yyyy/MM/dd HH:mm",
                "yyyy/MM/dd HH:mm:ss",
                "yyyy-M-d HH:mm",
                "yyyy-M-d HH:mm:ss"
            };

            var styles = DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal;
            if (System.DateTime.TryParseExact(s, fmts, CultureInfo.InvariantCulture, styles, out dt))
                return true;

            return System.DateTime.TryParse(s, CultureInfo.CurrentCulture, styles, out dt);
        }

        private void CreateUserItem(UserModel user)
        {
            if (userListContent == null) return;

            GameObject item;

            if (userItemPrefab != null)
            {
                item = Instantiate(userItemPrefab, userListContent);
            }
            else
            {
                // 动态创建简单Item（无预制体时的备用方案）
                item = CreateDefaultUserItem(user);
                return;
            }

            // 填充数据（假设预制体有对应子组件）
            var texts = item.GetComponentsInChildren<Text>();
            if (texts.Length >= 5)
            {
                texts[0].text = user.userId;
                texts[1].text = user.realName;
                texts[2].text = user.role == UserRole.Admin ? "管理员" : "普通用户";
                texts[3].text = user.email ?? "";
                texts[4].text = FormatDateYMD(user.createTime);
            }

            // 绑定按钮
            var buttons = item.GetComponentsInChildren<Button>();
            string uid = user.userId;

            foreach (var btn in buttons)
            {
                string btnName = btn.name.ToLower();
                if (btnName.Contains("delete") || btnName.Contains("删除"))
                    btn.onClick.AddListener(() => OnDeleteUser(uid));
                else if (btnName.Contains("reset") || btnName.Contains("重置"))
                    btn.onClick.AddListener(() => OnShowResetPassword(uid));
                else
                    Debug.Log($"Unknown button name: {btnName}");
            }
        }

        /// <summary>无预制体时动态创建用户Item</summary>
        private GameObject CreateDefaultUserItem(UserModel user)
        {
            var item = new GameObject($"UserItem_{user.userId}");
            item.transform.SetParent(userListContent, false);

            var rect = item.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 60);

            var layout = item.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.padding = new RectOffset(10, 10, 5, 5);
            layout.childForceExpandWidth  = true;
            layout.childForceExpandHeight = true;

            // 背景
            var bg = item.AddComponent<Image>();
            bg.color = user.isActive
                ? new Color(0.95f, 0.95f, 0.95f)
                : new Color(0.85f, 0.85f, 0.85f);

            // 信息文本
            string roleStr = user.role == UserRole.Admin ? "[管理员]" : "[用户]";
            string emailStr = string.IsNullOrEmpty(user.email) ? "-" : user.email;
            string info = $"{roleStr} {user.userId} | {user.realName} | {emailStr} | 注册:{FormatDateYMD(user.createTime)}";

            var textObj = new GameObject("InfoText");
            textObj.transform.SetParent(item.transform, false);
            var text = textObj.AddComponent<Text>();
            text.text      = info;
            text.font      = UIFont.Get();
            text.fontSize  = 14;
            text.color     = user.isActive ? Color.black : Color.gray;
            text.alignment = TextAnchor.MiddleLeft;

            // 操作按钮区
            string uid      = user.userId;
            bool   isAdmin  = user.role == UserRole.Admin;

            if (!isAdmin)
            {
                AddSmallButton(item.transform, "重置密码",
                    new Color(0.2f, 0.6f, 0.9f), () => OnShowResetPassword(uid));

                AddSmallButton(item.transform, "删除",
                    new Color(0.9f, 0.2f, 0.2f), () => OnDeleteUser(uid));
            }
            else
            {
                var adminTag = new GameObject("AdminTag");
                adminTag.transform.SetParent(item.transform, false);
                var tagText = adminTag.AddComponent<Text>();
                tagText.text      = "（内置管理员）";
                tagText.font      = UIFont.Get();
                tagText.fontSize  = 12;
                tagText.color     = new Color(0.2f, 0.5f, 0.9f);
                tagText.alignment = TextAnchor.MiddleCenter;
            }

            return item;
        }

        private void AddSmallButton(Transform parent, string label, Color color, UnityEngine.Events.UnityAction action)
        {
            var btnObj = new GameObject($"Btn_{label}");
            btnObj.transform.SetParent(parent, false);

            var rect = btnObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(80, 40);

            var img = btnObj.AddComponent<Image>();
            img.color = color;

            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(action);

            var textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            var text = textObj.AddComponent<Text>();
            text.text      = label;
            text.font      = UIFont.Get();
            text.fontSize  = 13;
            text.color     = Color.white;
            text.alignment = TextAnchor.MiddleCenter;

            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
        }

        private void OnSearchUser()
        {
            RefreshUserList();
        }

        private void OnResetUserFilters()
        {
            if (searchUserIdInput != null) searchUserIdInput.text = "";
            if (searchRealNameInput != null) searchRealNameInput.text = "";
            if (searchRoleDropdown != null)
            {
                searchRoleDropdown.value = 0;
                searchRoleDropdown.RefreshShownValue();
            }
            if (searchCreateStartInput != null) searchCreateStartInput.text = "";
            if (searchCreateEndInput != null) searchCreateEndInput.text = "";

            RefreshUserList();
        }

        private void OnDeleteUser(string userId)
        {
            var user = DataManager.Instance.FindUserById(userId);
            if (user == null) return;

            UIManager.Instance.ShowConfirm(
                "确认删除",
                $"确定要删除用户 [{user.username}] 吗？\n该用户的所有实验记录也将被删除！",
                () =>
                {
#if UNITY_WEBGL && !UNITY_EDITOR
                    UIManager.Instance.ShowLoading("正在删除用户...");
                    StartCoroutine(DataManager.Instance.DeleteUserAsync(userId, (ok, err) =>
                    {
                        UIManager.Instance.HideLoading();
                        if (ok)
                        {
                            UIManager.Instance.ShowToast($"用户 [{user.username}] 已删除");
                            RefreshUserList();
                        }
                        else
                        {
                            UIManager.Instance.ShowMessage("删除失败", err);
                        }
                    }));
#else
                    bool ok = DataManager.Instance.DeleteUser(userId, out string err);
                    if (ok)
                    {
                        UIManager.Instance.ShowToast($"用户 [{user.username}] 已删除");
                        RefreshUserList();
                    }
                    else
                    {
                        UIManager.Instance.ShowMessage("删除失败", err);
                    }
#endif
                }
            );
        }

        private void OnToggleUser(string userId)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var user = DataManager.Instance.FindUserById(userId);
            if (user == null) return;
            bool newActive = !user.isActive;
            UIManager.Instance.ShowLoading("正在更新状态...");
            StartCoroutine(DataManager.Instance.ToggleUserActiveAsync(userId, newActive, (ok, err) =>
            {
                UIManager.Instance.HideLoading();
                if (ok)
                {
                    string status = newActive ? "已启用" : "已禁用";
                    UIManager.Instance.ShowToast($"账号状态已更新：{status}");
                    RefreshUserList();
                }
                else
                {
                    UIManager.Instance.ShowMessage("操作失败", err);
                }
            }));
#else
            bool ok = DataManager.Instance.ToggleUserActive(userId, out string err);
            if (ok)
            {
                var user = DataManager.Instance.FindUserById(userId);
                string status = user != null && user.isActive ? "已启用" : "已禁用";
                UIManager.Instance.ShowToast($"账号状态已更新：{status}");
                RefreshUserList();
            }
            else
            {
                UIManager.Instance.ShowMessage("操作失败", err);
            }
#endif
        }

        // ── 添加用户 ──────────────────────────────────────────

        private void OnShowAddUserPanel()
        {
            if (addUserSubPanel == null) return;
            if (addUsernameInput != null) addUsernameInput.text = "";
            if (addPasswordInput != null) addPasswordInput.text = "";
            if (addRealNameInput != null) addRealNameInput.text = "";
            if (addEmailInput    != null) addEmailInput.text    = "";
            if (addUserErrorText != null) addUserErrorText.text = "";
            addUserSubPanel.SetActive(true);
        }

        private void OnConfirmAddUser()
        {
            string username = addUsernameInput != null ? addUsernameInput.text.Trim() : "";
            string password = addPasswordInput != null ? addPasswordInput.text        : "";
            string realName = addRealNameInput != null ? addRealNameInput.text.Trim() : "";
            string email    = addEmailInput    != null ? addEmailInput.text.Trim()    : "";
            UserRole role   = (addRoleDropdown != null && addRoleDropdown.value == 0)
                              ? UserRole.User : UserRole.Admin;

#if UNITY_WEBGL && !UNITY_EDITOR
            UIManager.Instance.ShowLoading("正在添加用户...");
            StartCoroutine(DataManager.Instance.AdminAddUserAsync(username, password, realName, email, role, (ok, err) =>
            {
                UIManager.Instance.HideLoading();
                if (!ok)
                {
                    if (addUserErrorText != null) addUserErrorText.text = err;
                    return;
                }
                if (addUserSubPanel != null) addUserSubPanel.SetActive(false);
                UIManager.Instance.ShowToast($"用户 [{username}] 添加成功！");
                RefreshUserList();
            }));
#else
            bool ok = DataManager.Instance.AdminAddUser(
                username, password, realName, email, role, out string err);

            if (!ok)
            {
                if (addUserErrorText != null) addUserErrorText.text = err;
                return;
            }

            if (addUserSubPanel != null) addUserSubPanel.SetActive(false);
            UIManager.Instance.ShowToast($"用户 [{username}] 添加成功！");
            RefreshUserList();
#endif
        }

        private void OnCancelAddUser()
        {
            if (addUserSubPanel != null) addUserSubPanel.SetActive(false);
        }

        // ── 重置密码 ──────────────────────────────────────────

        private void OnShowResetPassword(string userId)
        {
            _pendingResetUserId = userId;
            var user = DataManager.Instance.FindUserById(userId);

            if (resetPasswordSubPanel != null)
                resetPasswordSubPanel.SetActive(true);

            if (resetUserIdText != null) resetUserIdText.text = userId ?? "";
            if (resetRealNameInput != null) resetRealNameInput.text = user?.realName ?? "";

            // 邮箱、角色：显示数据库当前值
            if (resetEmailInput != null) resetEmailInput.text = user?.email ?? "";
            if (resetRoleDropdown != null)
            {
                resetRoleDropdown.value = (user != null && user.role == UserRole.Admin) ? 0 : 1;
                resetRoleDropdown.RefreshShownValue();
            }

            if (newPasswordInput        != null) newPasswordInput.text        = "";
            if (resetErrorText          != null) resetErrorText.text          = "";
        }

        private void OnConfirmReset()
        {
            string realName = resetRealNameInput != null ? resetRealNameInput.text.Trim() : "";
            string email    = resetEmailInput != null ? resetEmailInput.text.Trim() : "";
            // 新密码留空（含仅空格）表示不修改密码，由 DataManager.AdminUpdateUser 处理
            string newPwd = newPasswordInput != null ? newPasswordInput.text.Trim() : "";
            UserRole role = UserRole.User;
            if (resetRoleDropdown != null && resetRoleDropdown.value == 0) role = UserRole.Admin;

#if UNITY_WEBGL && !UNITY_EDITOR
            UIManager.Instance.ShowLoading("正在更新用户...");
            StartCoroutine(DataManager.Instance.AdminUpdateUserAsync(
                _pendingResetUserId, realName, email, role, newPwd, (ok, err) =>
                {
                    UIManager.Instance.HideLoading();
                    if (!ok)
                    {
                        if (resetErrorText != null) resetErrorText.text = err;
                        return;
                    }
                    if (resetPasswordSubPanel != null) resetPasswordSubPanel.SetActive(false);
                    UIManager.Instance.ShowToast("用户信息已更新！");
                    RefreshUserList();
                }));
#else
            bool ok = DataManager.Instance.AdminUpdateUser(
                _pendingResetUserId, realName, email, role, newPwd, out string err);

            if (!ok)
            {
                if (resetErrorText != null) resetErrorText.text = err;
                return;
            }

            if (resetPasswordSubPanel != null) resetPasswordSubPanel.SetActive(false);
            UIManager.Instance.ShowToast("用户信息已更新！");
            RefreshUserList();
#endif
        }

        private void OnCancelReset()
        {
            if (resetPasswordSubPanel != null) resetPasswordSubPanel.SetActive(false);
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region 实验记录管理
        // ─────────────────────────────────────────────────────

        private void RefreshRecordList(string keyword)
        {
            _allRecords = DataManager.Instance.GetAllRecords();
            RefreshExperimentNameDropdownOptions();

            if (recordListContent != null)
            {
                foreach (Transform child in recordListContent)
                    Destroy(child.gameObject);
            }

            string userIdKw = searchRecordUserIdInput != null ? searchRecordUserIdInput.text.Trim() : "";
            string selectedExperiment = GetSelectedExperimentName();

            bool hasStart = TryParseDateTimeFlexible(recordStartInput != null ? recordStartInput.text.Trim() : "", out var startDt);
            bool hasEnd = TryParseDateTimeFlexible(recordEndInput != null ? recordEndInput.text.Trim() : "", out var endDt);
            string startStr = recordStartInput != null ? recordStartInput.text.Trim() : "";
            string endStr = recordEndInput != null ? recordEndInput.text.Trim() : "";
            if (hasStart && !startStr.Contains(":")) startDt = startDt.Date;
            if (hasEnd && !endStr.Contains(":")) endDt = endDt.Date.AddDays(1).AddTicks(-1);

            int count = 0;
            foreach (var record in _allRecords)
            {
                if (!string.IsNullOrEmpty(keyword))
                {
                    bool match = (record.userId ?? "").Contains(keyword) ||
                                 (record.experimentName ?? "").Contains(keyword) ||
                                 (record.recordId ?? "").Contains(keyword) ||
                                 (record.realname ?? "").Contains(keyword);
                    if (!match) continue;
                }

                if (!string.IsNullOrEmpty(userIdKw))
                {
                    string uid = record.userId ?? "";
                    bool userMatch = uid.Equals(userIdKw, System.StringComparison.OrdinalIgnoreCase) || uid.Contains(userIdKw);
                    if (!userMatch) continue;
                }

                if (!string.IsNullOrEmpty(selectedExperiment) &&
                    !string.Equals(record.experimentName ?? "", selectedExperiment, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (hasStart || hasEnd)
                {
                    if (!TryParseDateTimeFlexible(record.recordTime, out var rt))
                        continue;
                    if (hasStart && rt < startDt) continue;
                    if (hasEnd && rt > endDt) continue;
                }

                CreateRecordItem(record);
                count++;
            }

            if (recordCountText != null)
                recordCountText.text = $"共 {count} 条记录";
        }

        private void CreateRecordItem(ExperimentRecord record)
        {
            if (recordListContent == null) return;

            GameObject item;

            if (recordItemPrefab != null)
            {
                item = Instantiate(recordItemPrefab, recordListContent);
                var texts = item.GetComponentsInChildren<Text>();
                // 仅显示：recordId，真实姓名，实验名称，实验记录时间，分数
                if (texts.Length >= 1) texts[0].text = record.recordId;
                if (texts.Length >= 2) texts[1].text = record.realname ?? "";
                if (texts.Length >= 3) texts[2].text = record.experimentName ?? "";
                if (texts.Length >= 4) texts[3].text = record.recordTime ?? "";
                if (texts.Length >= 5) texts[4].text = $"{record.score:F1}";
                if (texts.Length >= 6) texts[5].text = "";
                var buttons = item.GetComponentsInChildren<Button>();
                string rid = record.recordId;
                foreach (var btn in buttons)
                    if (btn.name.ToLower().Contains("delete") || btn.name.Contains("删除"))
                        btn.onClick.AddListener(() => OnDeleteRecord(rid));
            }
            else
            {
                // 动态创建
                item = new GameObject($"RecordItem_{record.recordId}");
                item.transform.SetParent(recordListContent, false);

                var rect = item.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(0, 70);

                var layout = item.AddComponent<HorizontalLayoutGroup>();
                layout.spacing = 10;
                layout.padding = new RectOffset(10, 10, 5, 5);
                layout.childForceExpandWidth  = true;
                layout.childForceExpandHeight = true;

                var bg = item.AddComponent<Image>();
                bg.color = new Color(0.95f, 0.95f, 0.95f);

                string info = $"UID：{record.userId}\n" +
                              $"实验：{record.experimentName} | 分数：{record.score:F1} | 时间：{record.recordTime}";

                var textObj = new GameObject("InfoText");
                textObj.transform.SetParent(item.transform, false);
                var text = textObj.AddComponent<Text>();
                text.text      = info;
                text.font      = UIFont.Get();
                text.fontSize  = 13;
                text.color     = Color.black;
                text.alignment = TextAnchor.MiddleLeft;

                string rid = record.recordId;
                AddSmallButton(item.transform, "删除",
                    new Color(0.9f, 0.2f, 0.2f), () => OnDeleteRecord(rid));
            }
        }

        private void OnSearchRecord()
        {
            string kw = searchRecordInput != null ? searchRecordInput.text.Trim() : "";
            RefreshRecordList(kw);
        }

        private void OnResetRecordFilters()
        {
            if (searchRecordUserIdInput != null) searchRecordUserIdInput.text = "";
            if (searchRecordInput != null) searchRecordInput.text = "";
            if (recordStartInput != null) recordStartInput.text = "";
            if (recordEndInput != null) recordEndInput.text = "";
            if (experimentNameDropdown != null)
            {
                experimentNameDropdown.value = 0;
                experimentNameDropdown.RefreshShownValue();
            }
            RefreshRecordList("");
        }

        private void OnDeleteRecord(string recordId)
        {
            UIManager.Instance.ShowConfirm(
                "确认删除",
                "确定要删除该实验记录吗？",
                () =>
                {
#if UNITY_WEBGL && !UNITY_EDITOR
                    UIManager.Instance.ShowLoading("正在删除记录...");
                    StartCoroutine(DataManager.Instance.DeleteRecordAsync(recordId, (ok, err) =>
                    {
                        UIManager.Instance.HideLoading();
                        if (ok)
                        {
                            UIManager.Instance.ShowToast("实验记录已删除");
                            RefreshRecordList("");
                        }
                        else
                        {
                            UIManager.Instance.ShowMessage("删除失败", err);
                        }
                    }));
#else
                    bool ok = DataManager.Instance.DeleteRecord(recordId, out string err);
                    if (ok)
                    {
                        UIManager.Instance.ShowToast("实验记录已删除");
                        RefreshRecordList("");
                    }
                    else
                    {
                        UIManager.Instance.ShowMessage("删除失败", err);
                    }
#endif
                }
            );
        }

        private string GetSelectedExperimentName()
        {
            if (experimentNameDropdown == null || experimentNameDropdown.options == null || experimentNameDropdown.options.Count == 0)
                return "";
            int idx = experimentNameDropdown.value;
            if (idx <= 0) return "";
            if (idx >= experimentNameDropdown.options.Count) return "";
            return experimentNameDropdown.options[idx].text ?? "";
        }

        private void RefreshExperimentNameDropdownOptions(List<ExperimentRecord> records)
        {
            // 兼容旧实现：保留签名但改为统一入口
            RefreshExperimentNameDropdownOptions();
        }

        private void RefreshExperimentNameDropdownOptions()
        {
            if (experimentNameDropdown == null) return;

            // 实验名称：直接从 experiments 表读取
            var set = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            try
            {
                var experiments = DataManager.Instance.GetAllExperiments();
                if (experiments != null)
                {
                    for (int i = 0; i < experiments.Count; i++)
                    {
                        var e = experiments[i];
                        if (e == null) continue;
                        string n = (e.experimentName ?? "").Trim();
                        if (!string.IsNullOrEmpty(n)) set.Add(n);
                    }
                }
            }
            catch
            {
                // ignore
            }

            var names = set.ToList();
            names.Sort(System.StringComparer.OrdinalIgnoreCase);

            string current = GetSelectedExperimentName();

            experimentNameDropdown.ClearOptions();
            var options = new List<string>(names.Count + 1) { "全部" };
            options.AddRange(names);
            experimentNameDropdown.AddOptions(options);

            // 尽量保持选择不跳
            int newIndex = 0;
            if (!string.IsNullOrEmpty(current))
            {
                for (int i = 1; i < options.Count; i++)
                {
                    if (string.Equals(options[i], current, System.StringComparison.OrdinalIgnoreCase))
                    {
                        newIndex = i;
                        break;
                    }
                }
            }
            experimentNameDropdown.value = newIndex;
            experimentNameDropdown.RefreshShownValue();
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
