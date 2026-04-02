// ============================================================
// 文件名：ToastUI.cs
// 功  能：顶部/底部Toast提示条组件
//         - 自动淡入淡出
//         - 支持成功/警告/错误三种样式
// 作  者：化工虚拟仿真实验平台
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ChemLab.UI
{
    public enum ToastType
    {
        Info,
        Success,
        Warning,
        Error
    }

    [RequireComponent(typeof(CanvasGroup))]
    public class ToastUI : MonoBehaviour
    {
        [Header("=== 组件引用 ===")]
        public Text    messageText;
        public Image   backgroundImage;
        public Image   iconImage;

        [Header("=== 样式颜色 ===")]
        public Color infoColor    = new Color(0.2f, 0.5f, 0.9f, 0.95f);
        public Color successColor = new Color(0.1f, 0.7f, 0.3f, 0.95f);
        public Color warningColor = new Color(0.9f, 0.6f, 0.1f, 0.95f);
        public Color errorColor   = new Color(0.9f, 0.2f, 0.2f, 0.95f);

        [Header("=== 动画设置 ===")]
        public float fadeDuration  = 0.3f;
        public float displayTime   = 2.5f;

        // ── 私有变量 ──────────────────────────────────────────
        private CanvasGroup _canvasGroup;
        private Coroutine   _showCoroutine;

        // ─────────────────────────────────────────────────────
        #region Unity 生命周期
        // ─────────────────────────────────────────────────────

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha          = 0f;
                _canvasGroup.interactable   = false;
                _canvasGroup.blocksRaycasts = false;
            }
            gameObject.SetActive(false);
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region 公开接口
        // ─────────────────────────────────────────────────────

        public void Show(string message, ToastType type = ToastType.Info,
                         float duration = -1f)
        {
            if (_showCoroutine != null)
                StopCoroutine(_showCoroutine);

            float showDuration = duration > 0 ? duration : displayTime;
            _showCoroutine = StartCoroutine(ShowCoroutine(message, type, showDuration));
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region 私有方法
        // ─────────────────────────────────────────────────────

        private IEnumerator ShowCoroutine(string message, ToastType type, float duration)
        {
            // 设置内容
            if (messageText != null) messageText.text = message;

            // 设置颜色
            if (backgroundImage != null)
            {
                switch (type)
                {
                    case ToastType.Success: backgroundImage.color = successColor; break;
                    case ToastType.Warning: backgroundImage.color = warningColor; break;
                    case ToastType.Error:   backgroundImage.color = errorColor;   break;
                    default:                backgroundImage.color = infoColor;    break;
                }
            }

            gameObject.SetActive(true);

            // 淡入
            yield return StartCoroutine(Fade(0f, 1f, fadeDuration));

            // 显示
            yield return new WaitForSeconds(duration);

            // 淡出
            yield return StartCoroutine(Fade(1f, 0f, fadeDuration));

            gameObject.SetActive(false);
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            if (_canvasGroup == null) yield break;

            _canvasGroup.interactable   = false;
            _canvasGroup.blocksRaycasts = false;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            _canvasGroup.alpha = to;
        }

        #endregion
    }
}
