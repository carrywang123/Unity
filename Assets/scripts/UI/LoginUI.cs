// ============================================================
// 文件名：LoginUI.cs
// 功  能：登录界面逻辑
//         - 支持管理员登录（账号222 密码222）
//         - 支持普通用户登录
//         - 登录成功后根据角色跳转对应面板
// 作  者：化工虚拟仿真实验平台
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using ChemLab.Managers;

namespace ChemLab.UI
{
    public class LoginUI : MonoBehaviour
    {
        // ── Inspector 绑定 ────────────────────────────────────
        [Header("=== 输入框 ===")]
        [Tooltip("用户名输入框")]
        public InputField usernameInput;

        [Tooltip("密码输入框")]
        public InputField passwordInput;

        [Header("=== 按钮 ===")]
        [Tooltip("登录按钮")]
        public Button loginBtn;

        [Tooltip("跳转注册按钮")]
        public Button goRegisterBtn;

        [Tooltip("显示/隐藏密码切换按钮")]
        public Button togglePasswordBtn;

        [Header("=== 文本 ===")]
        [Tooltip("错误提示文本")]
        public Text errorText;

        [Tooltip("欢迎标题文本")]
        public Text titleText;

        [Tooltip("版本号文本")]
        public Text versionText;

        [Tooltip("密码显示切换图标文本（可用文字代替图标）")]
        public Text togglePasswordIcon;

        [Header("=== 记住密码 ===")]
        [Tooltip("记住密码Toggle")]
        public Toggle rememberMeToggle;

        // ── 私有变量 ──────────────────────────────────────────
        private bool _isPasswordVisible = false;
        private const string PREF_REMEMBER   = "RememberMe";
        private const string PREF_USERNAME   = "SavedUsername";

        // ─────────────────────────────────────────────────────
        #region Unity 生命周期
        // ─────────────────────────────────────────────────────

        private void Awake()
        {
            // 绑定按钮事件
            if (loginBtn          != null) loginBtn.onClick.AddListener(OnLoginClick);
            if (goRegisterBtn     != null) goRegisterBtn.onClick.AddListener(OnGoRegisterClick);
            if (togglePasswordBtn != null) togglePasswordBtn.onClick.AddListener(OnTogglePassword);

            // 密码框默认隐藏
            if (passwordInput != null)
                passwordInput.contentType = InputField.ContentType.Password;
        }

        private void Start()
        {
            // 设置标题和版本
            if (titleText   != null) titleText.text   = "化工虚拟仿真实验平台";
            if (versionText != null) versionText.text = "Version 1.0.0";

            // 清空错误提示
            ClearError();

            // 读取记住密码
            LoadRememberedUser();
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region 面板显示回调（由UIManager调用）
        // ─────────────────────────────────────────────────────

        public void OnPanelShow()
        {
            ClearError();
            // 不清空用户名（保留记住密码的值）
            if (passwordInput != null) passwordInput.text = "";
            _isPasswordVisible = false;
            if (passwordInput != null)
                passwordInput.contentType = InputField.ContentType.Password;
            UpdateToggleIcon();
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region 按钮事件
        // ─────────────────────────────────────────────────────

        /// <summary>点击登录</summary>
        private void OnLoginClick()
        {
            ClearError();

            string username = usernameInput  != null ? usernameInput.text.Trim()  : "";
            string password = passwordInput  != null ? passwordInput.text          : "";

            // 远端数据库模式：异步登录（由后端连 MySQL）
            bool isAdmin = username == "admin"; // 简单推断：后端用 admin 账号作为管理员
            StartCoroutine(DataManager.Instance.LoginAsync(username, password, isAdmin, (ok, errorMsg) =>
            {
                if (!ok)
                {
                    ShowError(errorMsg);
                    ShakeInputField(passwordInput);
                    return;
                }

                // 处理记住密码
                if (rememberMeToggle != null && rememberMeToggle.isOn)
                {
                    PlayerPrefs.SetInt(PREF_REMEMBER, 1);
                    PlayerPrefs.SetString(PREF_USERNAME, username);
                }
                else
                {
                    PlayerPrefs.SetInt(PREF_REMEMBER, 0);
                    PlayerPrefs.DeleteKey(PREF_USERNAME);
                }
                PlayerPrefs.Save();

                // 登录成功提示
                var user = DataManager.Instance.CurrentUser;
                string roleStr = user.role == Models.UserRole.Admin ? "管理员" : "普通用户";
                UIManager.Instance.ShowToast($"欢迎回来，{user.realName}！（{roleStr}）");

                UIManager.Instance.NavigateByRole();
            }));
        }

        /// <summary>跳转注册页</summary>
        private void OnGoRegisterClick()
        {
            UIManager.Instance.ShowRegisterPanel();
        }

        /// <summary>切换密码显示/隐藏</summary>
        private void OnTogglePassword()
        {
            _isPasswordVisible = !_isPasswordVisible;

            if (passwordInput != null)
            {
                passwordInput.contentType = _isPasswordVisible
                    ? InputField.ContentType.Standard
                    : InputField.ContentType.Password;
                // 强制刷新显示
                passwordInput.ForceLabelUpdate();
            }

            UpdateToggleIcon();
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region 辅助方法
        // ─────────────────────────────────────────────────────

        private void ShowError(string msg)
        {
            if (errorText != null)
            {
                errorText.text  = msg;
                errorText.color = new Color(0.9f, 0.2f, 0.2f);
                errorText.gameObject.SetActive(true);
            }
        }

        private void ClearError()
        {
            if (errorText != null)
            {
                errorText.text = "";
                errorText.gameObject.SetActive(false);
            }
        }

        private void UpdateToggleIcon()
        {
            if (togglePasswordIcon != null)
                togglePasswordIcon.text = _isPasswordVisible ? "隐藏" : "显示";
        }

        private void LoadRememberedUser()
        {
            bool remember = PlayerPrefs.GetInt(PREF_REMEMBER, 0) == 1;
            if (rememberMeToggle != null) rememberMeToggle.isOn = remember;

            if (remember)
            {
                string savedName = PlayerPrefs.GetString(PREF_USERNAME, "");
                if (usernameInput != null) usernameInput.text = savedName;
            }
        }

        /// <summary>简单的输入框抖动效果</summary>
        private void ShakeInputField(InputField input)
        {
            if (input == null) return;
            StartCoroutine(ShakeCoroutine(input.transform));
        }

        private System.Collections.IEnumerator ShakeCoroutine(Transform t)
        {
            Vector3 origin = t.localPosition;
            float   elapsed = 0f;
            float   duration = 0.4f;
            float   magnitude = 8f;

            while (elapsed < duration)
            {
                float x = origin.x + Random.Range(-1f, 1f) * magnitude;
                t.localPosition = new Vector3(x, origin.y, origin.z);
                elapsed += Time.deltaTime;
                yield return null;
            }
            t.localPosition = origin;
        }

        #endregion
    }
}
