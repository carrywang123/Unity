// ============================================================
// 文件名：AdminPanelUI.cs
// 功  能：管理员面板
//         - 用户管理（查看/添加/删除/禁用/重置密码）
//         - 实验记录管理（查看全部/删除）
//         - 数据统计概览
// 作  者：化工虚拟仿真实验平台
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ChemLab.Managers;
using ChemLab.Models;

namespace ChemLab.UI
{
    public class AdminPanelUI : MonoBehaviour
    {
        // ── 顶部信息 ──────────────────────────────────────────
        [Header("=== 顶部信息 ===")]
        public Text welcomeText;
        public Text loginTimeText;
        public Button logoutBtn;

        // ── Tab 切换 ──────────────────────────────────────────
        [Header("=== Tab 按钮 ===")]
        public Button tabOverviewBtn;
        public Button tabUserBtn;
        public Button tabRecordBtn;

        // ── 各 Tab 面板 ───────────────────────────────────────
        [Header("=== Tab 面板 ===")]
        public GameObject overviewPanel;
        public GameObject userManagePanel;
        public GameObject recordManagePanel;

        // ── 概览面板 ──────────────────────────────────────────
        [Header("=== 概览数据 ===")]
        public Text totalUserText;
        public Text activeUserText;
        public Text totalRecordText;
        public Text completedRecordText;
        public Text avgScoreText;

        // ── 用户管理面板 ──────────────────────────────────────
        [Header("=== 用户管理 ===")]
        public Transform userListContent;       // ScrollView Content
        public GameObject userItemPrefab;       // 用户列表Item预制体
        public InputField searchUserInput;
        public Button searchUserBtn;
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
        public Text       resetTargetUserText;
        public InputField newPasswordInput;
        public Button     confirmResetBtn;
        public Button     cancelResetBtn;
        public Text       resetErrorText;

        // ── 实验记录面板 ──────────────────────────────────────
        [Header("=== 实验记录管理 ===")]
        public Transform recordListContent;
        public GameObject recordItemPrefab;
        public InputField searchRecordInput;
        public Button     searchRecordBtn;
        public Text       recordCountText;

        // ── 私有变量 ──────────────────────────────────────────
        private List<UserModel>       _allUsers   = new List<UserModel>();
        private List<ExperimentRecord> _allRecords = new List<ExperimentRecord>();
        private string _pendingResetUserId = "";

        // Tab颜色
        private static readonly Color TAB_ACTIVE   = new Color(0.2f, 0.5f, 0.9f);
        private static readonly Color TAB_INACTIVE  = new Color(0.4f, 0.4f, 0.4f);

        // ─────────────────────────────────────────────────────
        #region Unity 生命周期
        // ─────────────────────────────────────────────────────

        private void Awake()
        {
            // 顶部按钮
            if (logoutBtn != null) logoutBtn.onClick.AddListener(OnLogout);

            // Tab按钮
            if (tabOverviewBtn != null) tabOverviewBtn.onClick.AddListener(() => SwitchTab(0));
            if (tabUserBtn     != null) tabUserBtn.onClick.AddListener(()     => SwitchTab(1));
            if (tabRecordBtn   != null) tabRecordBtn.onClick.AddListener(()   => SwitchTab(2));

            // 用户管理
            if (searchUserBtn != null) searchUserBtn.onClick.AddListener(OnSearchUser);
            if (addUserBtn    != null) addUserBtn.onClick.AddListener(OnShowAddUserPanel);

            // 添加用户子面板
            if (confirmAddUserBtn != null) confirmAddUserBtn.onClick.AddListener(OnConfirmAddUser);
            if (cancelAddUserBtn  != null) cancelAddUserBtn.onClick.AddListener(OnCancelAddUser);

            // 重置密码子面板
            if (confirmResetBtn != null) confirmResetBtn.onClick.AddListener(OnConfirmReset);
            if (cancelResetBtn  != null) cancelResetBtn.onClick.AddListener(OnCancelReset);

            // 实验记录
            if (searchRecordBtn != null) searchRecordBtn.onClick.AddListener(OnSearchRecord);

            // 初始隐藏子面板
            if (addUserSubPanel       != null) addUserSubPanel.SetActive(false);
            if (resetPasswordSubPanel != null) resetPasswordSubPanel.SetActive(false);
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

            // 默认显示概览Tab
            SwitchTab(0);
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region Tab 切换
        // ─────────────────────────────────────────────────────

        private void SwitchTab(int index)
        {
            if (overviewPanel     != null) overviewPanel.SetActive(index == 0);
            if (userManagePanel   != null) userManagePanel.SetActive(index == 1);
            if (recordManagePanel != null) recordManagePanel.SetActive(index == 2);

            SetTabColor(tabOverviewBtn, index == 0);
            SetTabColor(tabUserBtn,     index == 1);
            SetTabColor(tabRecordBtn,   index == 2);

            switch (index)
            {
                case 0: RefreshOverview();     break;
                case 1: RefreshUserList("");   break;
                case 2: RefreshRecordList(""); break;
            }
        }

        private void SetTabColor(Button btn, bool active)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null) img.color = active ? TAB_ACTIVE : TAB_INACTIVE;
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region 概览
        // ─────────────────────────────────────────────────────

        private void RefreshOverview()
        {
            _allUsers   = DataManager.Instance.GetAllUsers();
            _allRecords = DataManager.Instance.GetAllRecords();

            int activeCount    = _allUsers.FindAll(u => u.isActive).Count;
            int completedCount = _allRecords.FindAll(r => r.isCompleted).Count;

            float avgScore = 0f;
            if (completedCount > 0)
            {
                float total = 0f;
                _allRecords.ForEach(r => { if (r.isCompleted) total += r.score; });
                avgScore = total / completedCount;
            }

            if (totalUserText      != null) totalUserText.text      = $"总用户数\n{_allUsers.Count}";
            if (activeUserText     != null) activeUserText.text     = $"活跃用户\n{activeCount}";
            if (totalRecordText    != null) totalRecordText.text    = $"实验总数\n{_allRecords.Count}";
            if (completedRecordText!= null) completedRecordText.text= $"已完成\n{completedCount}";
            if (avgScoreText       != null) avgScoreText.text       = $"平均分\n{avgScore:F1}";
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region 用户管理
        // ─────────────────────────────────────────────────────

        private void RefreshUserList(string keyword)
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
                // 关键词过滤
                if (!string.IsNullOrEmpty(keyword))
                {
                    bool match = user.username.Contains(keyword) ||
                                 user.realName.Contains(keyword) ||
                                 user.email.Contains(keyword);
                    if (!match) continue;
                }

                CreateUserItem(user);
            }
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
                texts[0].text = user.username;
                texts[1].text = user.realName;
                texts[2].text = user.role == UserRole.Admin ? "管理员" : "普通用户";
                texts[3].text = user.isActive ? "正常" : "已禁用";
                texts[4].text = user.createTime;
            }

            // 绑定按钮
            var buttons = item.GetComponentsInChildren<Button>();
            string uid = user.userId;

            foreach (var btn in buttons)
            {
                string btnName = btn.name.ToLower();
                if (btnName.Contains("delete") || btnName.Contains("删除"))
                    btn.onClick.AddListener(() => OnDeleteUser(uid));
                else if (btnName.Contains("toggle") || btnName.Contains("禁用") || btnName.Contains("启用"))
                    btn.onClick.AddListener(() => OnToggleUser(uid));
                else if (btnName.Contains("reset") || btnName.Contains("重置"))
                    btn.onClick.AddListener(() => OnShowResetPassword(uid, user.username));
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
            string roleStr   = user.role == UserRole.Admin ? "[管理员]" : "[用户]";
            string statusStr = user.isActive ? "正常" : "禁用";
            string info = $"{roleStr} {user.username} | {user.realName} | {statusStr} | 注册:{user.createTime}";

            var textObj = new GameObject("InfoText");
            textObj.transform.SetParent(item.transform, false);
            var text = textObj.AddComponent<Text>();
            text.text      = info;
            text.fontSize  = 14;
            text.color     = user.isActive ? Color.black : Color.gray;
            text.alignment = TextAnchor.MiddleLeft;

            // 操作按钮区
            string uid      = user.userId;
            string uname    = user.username;
            bool   isAdmin  = user.role == UserRole.Admin;

            if (!isAdmin)
            {
                AddSmallButton(item.transform, user.isActive ? "禁用" : "启用",
                    new Color(0.9f, 0.6f, 0.1f), () => OnToggleUser(uid));

                AddSmallButton(item.transform, "重置密码",
                    new Color(0.2f, 0.6f, 0.9f), () => OnShowResetPassword(uid, uname));

                AddSmallButton(item.transform, "删除",
                    new Color(0.9f, 0.2f, 0.2f), () => OnDeleteUser(uid));
            }
            else
            {
                var adminTag = new GameObject("AdminTag");
                adminTag.transform.SetParent(item.transform, false);
                var tagText = adminTag.AddComponent<Text>();
                tagText.text      = "（内置管理员）";
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
            string kw = searchUserInput != null ? searchUserInput.text.Trim() : "";
            RefreshUserList(kw);
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
                    bool ok = DataManager.Instance.DeleteUser(userId, out string err);
                    if (ok)
                    {
                        UIManager.Instance.ShowToast($"用户 [{user.username}] 已删除");
                        RefreshUserList("");
                    }
                    else
                    {
                        UIManager.Instance.ShowMessage("删除失败", err);
                    }
                }
            );
        }

        private void OnToggleUser(string userId)
        {
            bool ok = DataManager.Instance.ToggleUserActive(userId, out string err);
            if (ok)
            {
                var user = DataManager.Instance.FindUserById(userId);
                string status = user != null && user.isActive ? "已启用" : "已禁用";
                UIManager.Instance.ShowToast($"账号状态已更新：{status}");
                RefreshUserList("");
            }
            else
            {
                UIManager.Instance.ShowMessage("操作失败", err);
            }
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

            bool ok = DataManager.Instance.AdminAddUser(
                username, password, realName, email, role, out string err);

            if (!ok)
            {
                if (addUserErrorText != null) addUserErrorText.text = err;
                return;
            }

            if (addUserSubPanel != null) addUserSubPanel.SetActive(false);
            UIManager.Instance.ShowToast($"用户 [{username}] 添加成功！");
            RefreshUserList("");
        }

        private void OnCancelAddUser()
        {
            if (addUserSubPanel != null) addUserSubPanel.SetActive(false);
        }

        // ── 重置密码 ──────────────────────────────────────────

        private void OnShowResetPassword(string userId, string username)
        {
            _pendingResetUserId = userId;
            if (resetTargetUserText != null) resetTargetUserText.text = $"重置用户：{username}";
            if (newPasswordInput    != null) newPasswordInput.text    = "";
            if (resetErrorText      != null) resetErrorText.text      = "";
            if (resetPasswordSubPanel != null) resetPasswordSubPanel.SetActive(true);
        }

        private void OnConfirmReset()
        {
            string newPwd = newPasswordInput != null ? newPasswordInput.text : "";
            bool ok = DataManager.Instance.ResetPassword(_pendingResetUserId, newPwd, out string err);

            if (!ok)
            {
                if (resetErrorText != null) resetErrorText.text = err;
                return;
            }

            if (resetPasswordSubPanel != null) resetPasswordSubPanel.SetActive(false);
            UIManager.Instance.ShowToast("密码重置成功！");
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

            if (recordListContent != null)
            {
                foreach (Transform child in recordListContent)
                    Destroy(child.gameObject);
            }

            int count = 0;
            foreach (var record in _allRecords)
            {
                if (!string.IsNullOrEmpty(keyword))
                {
                    bool match = record.username.Contains(keyword) ||
                                 record.experimentName.Contains(keyword) ||
                                 record.experimentType.Contains(keyword);
                    if (!match) continue;
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
                if (texts.Length >= 6)
                {
                    texts[0].text = record.username;
                    texts[1].text = record.experimentName;
                    texts[2].text = record.experimentType;
                    texts[3].text = record.isCompleted ? $"{record.score:F1}分" : "未完成";
                    texts[4].text = record.startTime;
                    texts[5].text = record.result;
                }
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
                bg.color = record.isCompleted
                    ? new Color(0.93f, 0.97f, 0.93f)
                    : new Color(0.97f, 0.93f, 0.93f);

                string statusStr = record.isCompleted ? $"✓ {record.score:F1}分" : "进行中";
                string info = $"[{record.username}] {record.experimentName}（{record.experimentType}）\n" +
                              $"状态：{statusStr} | 开始：{record.startTime} | {record.result}";

                var textObj = new GameObject("InfoText");
                textObj.transform.SetParent(item.transform, false);
                var text = textObj.AddComponent<Text>();
                text.text      = info;
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

        private void OnDeleteRecord(string recordId)
        {
            UIManager.Instance.ShowConfirm(
                "确认删除",
                "确定要删除该实验记录吗？",
                () =>
                {
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
                }
            );
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
                        DataManager.Instance.Logout();
                        UIManager.Instance.ShowLoginPanel();
                        UIManager.Instance.ShowToast(ok ? "已安全退出登录" : ("退出失败：" + err));
                    }));
                }
            );
        }

        #endregion
    }
}
