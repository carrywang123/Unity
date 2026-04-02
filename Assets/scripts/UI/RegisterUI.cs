// ============================================================
// 文件名：RegisterUI.cs
// 功  能：注册界面逻辑
//         - 用户名、密码、确认密码、真实姓名、邮箱
//         - 实时表单验证
//         - 注册成功后自动跳回登录页
// 作  者：化工虚拟仿真实验平台
// ============================================================

using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using ChemLab.Managers;

namespace ChemLab.UI
{
    public class RegisterUI : MonoBehaviour
    {
        // ── Inspector 绑定 ────────────────────────────────────
        [Header("=== 输入框 ===")]
        public InputField usernameInput;
        public InputField passwordInput;
        public InputField confirmPasswordInput;
        public InputField realNameInput;
        public InputField emailInput;

        [Header("=== 验证提示文本（每个输入框下方）===")]
        public Text usernameHint;
        public Text passwordHint;
        public Text confirmPasswordHint;
        public Text realNameHint;
        public Text emailHint;

        [Header("=== 按钮 ===")]
        public Button registerBtn;
        public Button backToLoginBtn;
        public Button togglePasswordBtn;
        public Button toggleConfirmPasswordBtn;

        [Header("=== 其他文本 ===")]
        public Text titleText;
        public Text globalErrorText;

        [Header("=== 密码强度 ===")]
        public Slider passwordStrengthSlider;
        public Text   passwordStrengthText;

        // ── 私有变量 ──────────────────────────────────────────
        private bool _isPasswordVisible        = false;
        private bool _isConfirmPasswordVisible = false;

        // 颜色常量
        private static readonly Color COLOR_OK    = new Color(0.1f, 0.7f, 0.3f);
        private static readonly Color COLOR_ERROR = new Color(0.9f, 0.2f, 0.2f);
        private static readonly Color COLOR_WARN  = new Color(0.9f, 0.6f, 0.1f);
        private static readonly Color COLOR_GRAY  = new Color(0.6f, 0.6f, 0.6f);

        // ─────────────────────────────────────────────────────
        #region Unity 生命周期
        // ─────────────────────────────────────────────────────

        private void Awake()
        {
            // 绑定按钮
            if (registerBtn              != null) registerBtn.onClick.AddListener(OnRegisterClick);
            if (backToLoginBtn           != null) backToLoginBtn.onClick.AddListener(OnBackToLogin);
            if (togglePasswordBtn        != null) togglePasswordBtn.onClick.AddListener(OnTogglePassword);
            if (toggleConfirmPasswordBtn != null) toggleConfirmPasswordBtn.onClick.AddListener(OnToggleConfirmPassword);

            // 密码框默认隐藏
            SetPasswordContentType(passwordInput,        false);
            SetPasswordContentType(confirmPasswordInput, false);

            // 绑定实时验证
            if (usernameInput        != null) usernameInput.onValueChanged.AddListener(_ => ValidateUsername());
            if (passwordInput        != null) passwordInput.onValueChanged.AddListener(_ => { ValidatePassword(); ValidateConfirmPassword(); });
            if (confirmPasswordInput != null) confirmPasswordInput.onValueChanged.AddListener(_ => ValidateConfirmPassword());
            if (realNameInput        != null) realNameInput.onValueChanged.AddListener(_ => ValidateRealName());
            if (emailInput           != null) emailInput.onValueChanged.AddListener(_ => ValidateEmail());
        }

        private void Start()
        {
            if (titleText != null) titleText.text = "新用户注册";
            ClearAllHints();
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region 面板显示回调
        // ─────────────────────────────────────────────────────

        public void OnPanelShow()
        {
            // 清空所有输入框
            if (usernameInput        != null) usernameInput.text        = "";
            if (passwordInput        != null) passwordInput.text        = "";
            if (confirmPasswordInput != null) confirmPasswordInput.text = "";
            if (realNameInput        != null) realNameInput.text        = "";
            if (emailInput           != null) emailInput.text           = "";

            ClearAllHints();

            // 重置密码强度
            if (passwordStrengthSlider != null) passwordStrengthSlider.value = 0;
            if (passwordStrengthText   != null) passwordStrengthText.text    = "";
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region 按钮事件
        // ─────────────────────────────────────────────────────

        private void OnRegisterClick()
        {
            // 全量验证
            bool ok = true;
            ok &= ValidateUsername();
            ok &= ValidatePassword();
            ok &= ValidateConfirmPassword();
            ok &= ValidateRealName();
            ok &= ValidateEmail();

            if (!ok)
            {
                ShowGlobalError("请检查并修正表单中的错误！");
                return;
            }

            string username = usernameInput.text.Trim();
            string password = passwordInput.text;
            string realName = realNameInput != null ? realNameInput.text.Trim() : "";
            string email    = emailInput    != null ? emailInput.text.Trim()    : "";

            StartCoroutine(DataManager.Instance.RegisterUserAsync(username, password, realName, email, (success, errorMsg) =>
            {
                if (!success)
                {
                    ShowGlobalError(errorMsg);
                    return;
                }

                ClearGlobalError();
                UIManager.Instance.ShowMessage(
                    "注册成功",
                    $"账号 [{username}] 注册成功！\n请使用新账号登录。",
                    () => UIManager.Instance.ShowLoginPanel()
                );
            }));
        }

        private void OnBackToLogin()
        {
            UIManager.Instance.ShowLoginPanel();
        }

        private void OnTogglePassword()
        {
            _isPasswordVisible = !_isPasswordVisible;
            SetPasswordContentType(passwordInput, _isPasswordVisible);
        }

        private void OnToggleConfirmPassword()
        {
            _isConfirmPasswordVisible = !_isConfirmPasswordVisible;
            SetPasswordContentType(confirmPasswordInput, _isConfirmPasswordVisible);
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region 表单验证
        // ─────────────────────────────────────────────────────

        private bool ValidateUsername()
        {
            if (usernameInput == null) return true;
            string val = usernameInput.text.Trim();

            if (string.IsNullOrEmpty(val))
            { SetHint(usernameHint, "用户名不能为空", COLOR_ERROR); return false; }

            if (val.Length < 3)
            { SetHint(usernameHint, "用户名至少3个字符", COLOR_ERROR); return false; }

            if (val.Length > 20)
            { SetHint(usernameHint, "用户名不能超过20个字符", COLOR_ERROR); return false; }

            if (!Regex.IsMatch(val, @"^[a-zA-Z0-9_\u4e00-\u9fa5]+$"))
            { SetHint(usernameHint, "用户名只能包含字母、数字、下划线或中文", COLOR_ERROR); return false; }

            SetHint(usernameHint, "✓ 用户名可用", COLOR_OK);
            return true;
        }

        private bool ValidatePassword()
        {
            if (passwordInput == null) return true;
            string val = passwordInput.text;

            if (string.IsNullOrEmpty(val))
            { SetHint(passwordHint, "密码不能为空", COLOR_ERROR); UpdateStrength(0); return false; }

            if (val.Length < 6)
            { SetHint(passwordHint, "密码至少6位", COLOR_ERROR); UpdateStrength(1); return false; }

            int strength = CalcPasswordStrength(val);
            UpdateStrength(strength);

            if (strength == 1)
                SetHint(passwordHint, "密码强度：弱（建议包含字母+数字）", COLOR_WARN);
            else if (strength == 2)
                SetHint(passwordHint, "密码强度：中", COLOR_WARN);
            else
                SetHint(passwordHint, "✓ 密码强度：强", COLOR_OK);

            return true;
        }

        private bool ValidateConfirmPassword()
        {
            if (confirmPasswordInput == null || passwordInput == null) return true;
            string pwd     = passwordInput.text;
            string confirm = confirmPasswordInput.text;

            if (string.IsNullOrEmpty(confirm))
            { SetHint(confirmPasswordHint, "请再次输入密码", COLOR_ERROR); return false; }

            if (pwd != confirm)
            { SetHint(confirmPasswordHint, "两次密码输入不一致", COLOR_ERROR); return false; }

            SetHint(confirmPasswordHint, "✓ 密码一致", COLOR_OK);
            return true;
        }

        private bool ValidateRealName()
        {
            if (realNameInput == null) return true;
            string val = realNameInput.text.Trim();

            if (string.IsNullOrEmpty(val))
            { SetHint(realNameHint, "真实姓名不能为空", COLOR_ERROR); return false; }

            if (val.Length > 20)
            { SetHint(realNameHint, "姓名不能超过20个字符", COLOR_ERROR); return false; }

            SetHint(realNameHint, "✓", COLOR_OK);
            return true;
        }

        private bool ValidateEmail()
        {
            if (emailInput == null) return true;
            string val = emailInput.text.Trim();

            // 邮箱可选
            if (string.IsNullOrEmpty(val))
            { SetHint(emailHint, "邮箱为选填项", COLOR_GRAY); return true; }

            if (!Regex.IsMatch(val, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            { SetHint(emailHint, "邮箱格式不正确", COLOR_ERROR); return false; }

            SetHint(emailHint, "✓ 邮箱格式正确", COLOR_OK);
            return true;
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region 辅助方法
        // ─────────────────────────────────────────────────────

        private void SetHint(Text hint, string msg, Color color)
        {
            if (hint == null) return;
            hint.text  = msg;
            hint.color = color;
            hint.gameObject.SetActive(true);
        }

        private void ClearAllHints()
        {
            ClearHint(usernameHint);
            ClearHint(passwordHint);
            ClearHint(confirmPasswordHint);
            ClearHint(realNameHint);
            ClearHint(emailHint);
            ClearGlobalError();
        }

        private void ClearHint(Text hint)
        {
            if (hint == null) return;
            hint.text = "";
            hint.gameObject.SetActive(false);
        }

        private void ShowGlobalError(string msg)
        {
            if (globalErrorText == null) return;
            globalErrorText.text  = msg;
            globalErrorText.color = COLOR_ERROR;
            globalErrorText.gameObject.SetActive(true);
        }

        private void ClearGlobalError()
        {
            if (globalErrorText == null) return;
            globalErrorText.text = "";
            globalErrorText.gameObject.SetActive(false);
        }

        private void SetPasswordContentType(InputField input, bool visible)
        {
            if (input == null) return;
            input.contentType = visible
                ? InputField.ContentType.Standard
                : InputField.ContentType.Password;
            input.ForceLabelUpdate();
        }

        /// <summary>计算密码强度 1=弱 2=中 3=强</summary>
        private int CalcPasswordStrength(string pwd)
        {
            int score = 0;
            if (pwd.Length >= 8)  score++;
            if (Regex.IsMatch(pwd, @"[A-Z]")) score++;
            if (Regex.IsMatch(pwd, @"[a-z]")) score++;
            if (Regex.IsMatch(pwd, @"[0-9]")) score++;
            if (Regex.IsMatch(pwd, @"[^a-zA-Z0-9]")) score++;

            if (score <= 2) return 1;
            if (score <= 3) return 2;
            return 3;
        }

        private void UpdateStrength(int level)
        {
            if (passwordStrengthSlider != null)
                passwordStrengthSlider.value = level / 3f;

            if (passwordStrengthText != null)
            {
                switch (level)
                {
                    case 0: passwordStrengthText.text = ""; break;
                    case 1: passwordStrengthText.text = "弱"; passwordStrengthText.color = COLOR_ERROR; break;
                    case 2: passwordStrengthText.text = "中"; passwordStrengthText.color = COLOR_WARN;  break;
                    case 3: passwordStrengthText.text = "强"; passwordStrengthText.color = COLOR_OK;    break;
                }
            }
        }

        #endregion
    }
}
