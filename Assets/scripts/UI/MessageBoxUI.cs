// ============================================================
// 文件名：MessageBoxUI.cs
// 功  能：通用消息弹窗组件（挂载在MessageBox预制体上）
//         支持：纯提示 / 确认取消 两种模式
//         支持：淡入淡出动画
// 作  者：化工虚拟仿真实验平台
// ============================================================

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ChemLab.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class MessageBoxUI : MonoBehaviour
    {
        [Header("=== 弹窗组件 ===")]
        public Text    titleText;
        public Text    contentText;
        public Button  confirmButton;
        public Button  cancelButton;
        public Text    confirmBtnText;
        public Text    cancelBtnText;

        [Header("=== 动画设置 ===")]
        [Tooltip("淡入淡出时长（秒）")]
        public float fadeDuration = 0.2f;

        [Tooltip("弹窗缩放动画")]
        public bool useScaleAnimation = true;

        // ── 私有变量 ──────────────────────────────────────────
        private CanvasGroup _canvasGroup;
        private RectTransform _rectTransform;
        private Action _onConfirm;
        private Action _onCancel;

        // ─────────────────────────────────────────────────────
        #region Unity 生命周期
        // ─────────────────────────────────────────────────────

        private void Awake()
        {
            _canvasGroup   = GetComponent<CanvasGroup>();
            _rectTransform = GetComponent<RectTransform>();

            if (confirmButton != null)
                confirmButton.onClick.AddListener(OnConfirmClick);
            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancelClick);
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region 公开接口
        // ─────────────────────────────────────────────────────

        /// <summary>显示纯提示弹窗</summary>
        public void ShowInfo(string title, string content, Action onConfirm = null,
                             string confirmText = "确 定")
        {
            Setup(title, content, onConfirm, null, confirmText, "", false);
            Show();
        }

        /// <summary>显示确认弹窗</summary>
        public void ShowConfirm(string title, string content,
                                Action onConfirm, Action onCancel = null,
                                string confirmText = "确 定",
                                string cancelText  = "取 消")
        {
            Setup(title, content, onConfirm, onCancel, confirmText, cancelText, true);
            Show();
        }

        /// <summary>隐藏弹窗</summary>
        public void Hide()
        {
            StartCoroutine(FadeOut());
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region 私有方法
        // ─────────────────────────────────────────────────────

        private void Setup(string title, string content,
                           Action onConfirm, Action onCancel,
                           string confirmText, string cancelText,
                           bool showCancel)
        {
            if (titleText   != null) titleText.text   = title;
            if (contentText != null) contentText.text = content;

            _onConfirm = onConfirm;
            _onCancel  = onCancel;

            if (confirmBtnText != null) confirmBtnText.text = confirmText;
            if (cancelBtnText  != null) cancelBtnText.text  = cancelText;

            if (cancelButton != null)
                cancelButton.gameObject.SetActive(showCancel);
        }

        private void Show()
        {
            gameObject.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(FadeIn());
        }

        private IEnumerator FadeIn()
        {
            _canvasGroup.alpha          = 0f;
            _canvasGroup.interactable   = false;
            _canvasGroup.blocksRaycasts = true;

            if (useScaleAnimation && _rectTransform != null)
                _rectTransform.localScale = Vector3.one * 0.8f;

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeDuration;
                _canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);

                if (useScaleAnimation && _rectTransform != null)
                    _rectTransform.localScale = Vector3.Lerp(
                        Vector3.one * 0.8f, Vector3.one, t);

                yield return null;
            }

            _canvasGroup.alpha          = 1f;
            _canvasGroup.interactable   = true;
            _canvasGroup.blocksRaycasts = true;

            if (useScaleAnimation && _rectTransform != null)
                _rectTransform.localScale = Vector3.one;
        }

        private IEnumerator FadeOut()
        {
            _canvasGroup.interactable   = false;
            _canvasGroup.blocksRaycasts = false;

            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
                yield return null;
            }

            _canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        private void OnConfirmClick()
        {
            Hide();
            _onConfirm?.Invoke();
            _onConfirm = null;
        }

        private void OnCancelClick()
        {
            Hide();
            _onCancel?.Invoke();
            _onCancel = null;
        }

        #endregion
    }
}
