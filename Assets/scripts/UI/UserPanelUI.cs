// ============================================================
// 文件名：UserPanelUI.cs
// 功  能：普通用户面板
//         - 个人信息展示
//         - 我的实验记录（查看/开始新实验）
//         - 修改密码
// 作  者：化工虚拟仿真实验平台
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ChemLab.Managers;
using ChemLab.Models;

namespace ChemLab.UI
{
    public class UserPanelUI : MonoBehaviour
    {
        // ── 顶部信息 ──────────────────────────────────────────
        [Header("=== 顶部信息 ===")]
        public Text welcomeText;
        public Text userInfoText;
        public Button logoutBtn;

        // ── Tab 切换 ──────────────────────────────────────────
        [Header("=== Tab 按钮 ===")]
        public Button tabHomeBtn;
        public Button tabRecordBtn;
        public Button tabProfileBtn;

        // ── 各 Tab 面板 ───────────────────────────────────────
        [Header("=== Tab 面板 ===")]
        public GameObject homePanel;
        public GameObject recordPanel;
        public GameObject profilePanel;

        // ── 首页面板 ──────────────────────────────────────────
        [Header("=== 首页 ===")]
        public Text homeWelcomeText;
        public Text myRecordCountText;
        public Text myCompletedCountText;
        public Text myAvgScoreText;
        public Text lastLoginText;

        // 实验入口按钮（示例）
        [Header("=== 实验入口 ===")]
        public Button startExperiment1Btn;  // 蒸馏实验
        public Button startExperiment2Btn;  // 萃取实验
        public Button startExperiment3Btn;  // 滴定实验
        public Button startExperiment4Btn;  // 结晶实验

        // ── 实验记录面板 ──────────────────────────────────────
        [Header("=== 我的实验记录 ===")]
        public Transform recordListContent;
        public GameObject recordItemPrefab;
        public Text recordSummaryText;

        // ── 个人信息面板 ──────────────────────────────────────
        [Header("=== 个人信息 ===")]
        public Text profileUsernameText;
        public Text profileRealNameText;
        public Text profileEmailText;
        public Text profileRoleText;
        public Text profileCreateTimeText;
        public Text profileLastLoginText;

        // 修改密码
        [Header("=== 修改密码 ===")]
        public InputField oldPasswordInput;
        public InputField newPasswordInput;
        public InputField confirmNewPasswordInput;
        public Button     changePasswordBtn;
        public Text       changePasswordMsg;

        // ── 私有变量 ──────────────────────────────────────────
        private List<ExperimentRecord> _myRecords = new List<ExperimentRecord>();

        private static readonly Color TAB_ACTIVE  = new Color(0.2f, 0.6f, 0.4f);
        private static readonly Color TAB_INACTIVE = new Color(0.4f, 0.4f, 0.4f);
        private static readonly Color COLOR_OK    = new Color(0.1f, 0.7f, 0.3f);
        private static readonly Color COLOR_ERROR = new Color(0.9f, 0.2f, 0.2f);

        // ─────────────────────────────────────────────────────
        #region Unity 生命周期
        // ─────────────────────────────────────────────────────

        private void Awake()
        {
            if (logoutBtn != null) logoutBtn.onClick.AddListener(OnLogout);

            if (tabHomeBtn    != null) tabHomeBtn.onClick.AddListener(()    => SwitchTab(0));
            if (tabRecordBtn  != null) tabRecordBtn.onClick.AddListener(()  => SwitchTab(1));
            if (tabProfileBtn != null) tabProfileBtn.onClick.AddListener(() => SwitchTab(2));

            // 实验入口
            if (startExperiment1Btn != null)
                startExperiment1Btn.onClick.AddListener(() => OnStartExperiment("乙醇蒸馏实验", "蒸馏"));
            if (startExperiment2Btn != null)
                startExperiment2Btn.onClick.AddListener(() => OnStartExperiment("苯甲酸萃取实验", "萃取"));
            if (startExperiment3Btn != null)
                startExperiment3Btn.onClick.AddListener(() => OnStartExperiment("酸碱中和滴定", "滴定"));
            if (startExperiment4Btn != null)
                startExperiment4Btn.onClick.AddListener(() => OnStartExperiment("硫酸铜结晶实验", "结晶"));

            // 修改密码
            if (changePasswordBtn != null)
                changePasswordBtn.onClick.AddListener(OnChangePassword);
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

            SwitchTab(0);
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region Tab 切换
        // ─────────────────────────────────────────────────────

        private void SwitchTab(int index)
        {
            if (homePanel    != null) homePanel.SetActive(index == 0);
            if (recordPanel  != null) recordPanel.SetActive(index == 1);
            if (profilePanel != null) profilePanel.SetActive(index == 2);

            SetTabColor(tabHomeBtn,    index == 0);
            SetTabColor(tabRecordBtn,  index == 1);
            SetTabColor(tabProfileBtn, index == 2);

            switch (index)
            {
                case 0: RefreshHome();    break;
                case 1: RefreshRecords(); break;
                case 2: RefreshProfile(); break;
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
        #region 首页
        // ─────────────────────────────────────────────────────

        private void RefreshHome()
        {
            var user = DataManager.Instance.CurrentUser;
            if (user == null) return;

            _myRecords = DataManager.Instance.GetRecordsByUser(user.userId);

            int completedCount = _myRecords.FindAll(r => r.isCompleted).Count;
            float avgScore = 0f;
            if (completedCount > 0)
            {
                float total = 0f;
                _myRecords.ForEach(r => { if (r.isCompleted) total += r.score; });
                avgScore = total / completedCount;
            }

            if (homeWelcomeText     != null) homeWelcomeText.text     = $"你好，{user.realName}！欢迎使用化工虚拟仿真实验平台";
            if (myRecordCountText   != null) myRecordCountText.text   = $"实验次数\n{_myRecords.Count}";
            if (myCompletedCountText!= null) myCompletedCountText.text= $"已完成\n{completedCount}";
            if (myAvgScoreText      != null) myAvgScoreText.text      = $"平均分\n{avgScore:F1}";
            if (lastLoginText       != null) lastLoginText.text       = $"上次登录：{user.lastLoginTime}";
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

            if (recordListContent != null)
            {
                foreach (Transform child in recordListContent)
                    Destroy(child.gameObject);
            }

            if (recordSummaryText != null)
                recordSummaryText.text = $"共 {_myRecords.Count} 条实验记录";

            if (_myRecords.Count == 0)
            {
                CreateEmptyHint(recordListContent, "暂无实验记录，快去开始第一个实验吧！");
                return;
            }

            foreach (var record in _myRecords)
                CreateRecordItem(record);
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
                    texts[0].text = record.experimentName;
                    texts[1].text = record.experimentType;
                    texts[2].text = record.isCompleted ? $"{record.score:F1}分" : "进行中";
                    texts[3].text = record.startTime;
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
                bg.color = record.isCompleted
                    ? new Color(0.93f, 0.97f, 0.93f, 1f)
                    : new Color(0.97f, 0.97f, 0.93f, 1f);

                // 第一行：实验名称 + 得分
                string scoreStr = record.isCompleted ? $"得分：{record.score:F1}" : "【进行中】";
                AddTextToItem(item.transform, $"📋 {record.experimentName}（{record.experimentType}）  {scoreStr}",
                    15, Color.black, FontStyle.Bold);

                // 第二行：时间
                string timeStr = record.isCompleted
                    ? $"开始：{record.startTime}  结束：{record.endTime}"
                    : $"开始：{record.startTime}";
                AddTextToItem(item.transform, timeStr, 12, Color.gray, FontStyle.Normal);

                // 第三行：结果
                if (!string.IsNullOrEmpty(record.result))
                    AddTextToItem(item.transform, $"结果：{record.result}", 12,
                        new Color(0.2f, 0.5f, 0.2f), FontStyle.Normal);
            }
        }

        private void AddTextToItem(Transform parent, string content, int fontSize,
                                    Color color, FontStyle style)
        {
            var obj = new GameObject("Text");
            obj.transform.SetParent(parent, false);
            var text = obj.AddComponent<Text>();
            text.text      = content;
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

            if (profileUsernameText  != null) profileUsernameText.text  = $"用户名：{user.username}";
            if (profileRealNameText  != null) profileRealNameText.text  = $"真实姓名：{user.realName}";
            if (profileEmailText     != null) profileEmailText.text     = $"邮箱：{(string.IsNullOrEmpty(user.email) ? "未填写" : user.email)}";
            if (profileRoleText      != null) profileRoleText.text      = $"角色：{(user.role == UserRole.Admin ? "管理员" : "普通用户")}";
            if (profileCreateTimeText!= null) profileCreateTimeText.text= $"注册时间：{user.createTime}";
            if (profileLastLoginText != null) profileLastLoginText.text = $"上次登录：{user.lastLoginTime}";

            // 清空密码修改框
            if (oldPasswordInput        != null) oldPasswordInput.text        = "";
            if (newPasswordInput        != null) newPasswordInput.text        = "";
            if (confirmNewPasswordInput != null) confirmNewPasswordInput.text = "";
            if (changePasswordMsg       != null) changePasswordMsg.text       = "";
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
        }

        private void SetChangeMsg(string msg, Color color)
        {
            if (changePasswordMsg == null) return;
            changePasswordMsg.text  = msg;
            changePasswordMsg.color = color;
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
                    var record = new ExperimentRecord(
                        user.userId, user.username,
                        experimentName, experimentType
                    );
                    DataManager.Instance.AddRecord(record);

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

            float score  = Random.Range(60f, 100f);
            string result = score >= 90 ? "实验操作规范，结果优秀！"
                          : score >= 75 ? "实验基本完成，有少量误差。"
                          : "实验完成，建议复习相关知识点。";

            DataManager.Instance.CompleteRecord(recordId, score, result);
            UIManager.Instance.ShowMessage(
                "实验完成",
                $"【{experimentName}】实验完成！\n得分：{score:F1} 分\n{result}",
                () => RefreshRecords()
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
