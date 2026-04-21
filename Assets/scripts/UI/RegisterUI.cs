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
using System.Collections;

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

        [Header("=== 其他文本 ===")]
        public Text titleText;
        public Text globalErrorText;

        // ── 私有变量 ──────────────────────────────────────────
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
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region 按钮事件
        // ─────────────────────────────────────────────────────

        private void OnRegisterClick()
        {
            // 必填控件未绑定时直接拦截，避免空引用
            if (usernameInput == null || passwordInput == null || confirmPasswordInput == null)
            {
                ShowGlobalError("注册界面输入框未正确绑定（username/password/confirmPassword）。请在 Inspector 里拖拽绑定后再试。");
                return;
            }

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
            Debug.Log($"RegisterUI: OnRegisterClick: username={username}, password={password}, realName={realName}, email={email}");
            StartCoroutine(DataManager.Instance.RegisterUserAsync(username, password, realName, email, (success, errorMsg) =>
            {
                // 请求返回时本组件可能已被切换/销毁，避免空引用
                if (this == null || !isActiveAndEnabled) return;

                if (!success)
                {
                    Debug.Log("1");
                    try
                    {
                        ShowGlobalError(errorMsg);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError("[RegisterUI] ShowGlobalError 发生异常：" + e);
                    }
                    return;
                }
                Debug.Log("2");
                try
                {
                    ClearGlobalError();
                }
                catch (System.Exception e)
                {
                    Debug.LogError("[RegisterUI] ClearGlobalError 发生异常：" + e);
                }

                try
                {
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.ShowMessage(
                            "注册成功",
                            $"账号 [{username}] 注册成功！\n2 秒后自动跳转到登录页。"
                        );
                    }
                    else
                    {
                        Debug.LogWarning("[RegisterUI] UIManager.Instance 为空，无法显示注册成功弹窗。");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError("[RegisterUI] ShowMessage 发生异常：" + e);
                }
                StartCoroutine(AutoBackToLogin(2f));
            }));
        }

        private void OnBackToLogin()
        {
            UIManager.Instance.ShowLoginPanel();
        }

        #endregion

        private IEnumerator AutoBackToLogin(float seconds)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, seconds));
            UIManager.Instance.ShowLoginPanel();
        }

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
            { SetHint(passwordHint, "密码不能为空", COLOR_ERROR); return false; }

            if (val.Length < 6)
            { SetHint(passwordHint, "密码至少6位", COLOR_ERROR); return false; }

            SetHint(passwordHint, "✓", COLOR_OK);

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
            // 允许未绑定 globalErrorText 时仍能提示错误（避免空引用导致二次崩溃）
            if (globalErrorText != null)
            {
                globalErrorText.text  = msg;
                globalErrorText.color = COLOR_ERROR;
                if (globalErrorText.gameObject != null)
                    globalErrorText.gameObject.SetActive(true);
                return;
            }

            Debug.LogWarning("[RegisterUI] globalErrorText 未绑定，改用弹窗提示：" + msg);
            if (UIManager.Instance != null)
                UIManager.Instance.ShowMessage("注册失败", string.IsNullOrEmpty(msg) ? "注册失败" : msg);
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

        #endregion
    }
}
