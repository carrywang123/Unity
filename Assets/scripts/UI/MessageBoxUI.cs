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

        [Header("=== 纯提示图标（右上角）===")]
        [Tooltip("右上角提示图标的背景 Image（同一个 Image，通过切换 Sprite 来显示成功/失败）")]
        public Image infoIconImage;

        [Tooltip("成功 Sprite（image_pass）")]
        public Sprite passSprite;

        [Tooltip("失败 Sprite（image_error）")]
        public Sprite errorSprite;

        [Tooltip("纯提示弹窗显示在右上角的边距（像素）")]
        public Vector2 infoTopRightMargin = new Vector2(24f, 24f);

        [Tooltip("纯提示弹窗是否显示在右上角（ShowInfo 会强制启用）")]
        public bool infoShowAtTopRight = true;

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

        // 记录原始布局（用于 ShowConfirm 恢复）
        private Vector2 _originAnchorMin;
        private Vector2 _originAnchorMax;
        private Vector2 _originPivot;
        private Vector2 _originAnchoredPos;

        // ─────────────────────────────────────────────────────
        #region Unity 生命周期
        // ─────────────────────────────────────────────────────

        private void Awake()
        {
            _canvasGroup   = GetComponent<CanvasGroup>();
            _rectTransform = GetComponent<RectTransform>();

            if (_rectTransform != null)
            {
                _originAnchorMin   = _rectTransform.anchorMin;
                _originAnchorMax   = _rectTransform.anchorMax;
                _originPivot       = _rectTransform.pivot;
                _originAnchoredPos = _rectTransform.anchoredPosition;
            }

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
            ApplyInfoStyle(title, content);
            Show();
        }

        /// <summary>显示纯提示弹窗（指定成功/失败图标）</summary>
        public void ShowInfo(string title, string content, bool isSuccess, Action onConfirm = null,
                             string confirmText = "确 定")
        {
            Setup(title, content, onConfirm, null, confirmText, "", false);
            ApplyInfoStyle(title, content, isSuccess);
            Show();
        }

        /// <summary>显示确认弹窗</summary>
        public void ShowConfirm(string title, string content,
                                Action onConfirm, Action onCancel = null,
                                string confirmText = "确 定",
                                string cancelText  = "取 消")
        {
            Setup(title, content, onConfirm, onCancel, confirmText, cancelText, true);
            ApplyConfirmStyle();
            Show();
        }

        /// <summary>隐藏弹窗</summary>
        public void Hide()
        {
            // 可能在已隐藏状态下再次被调用（例如重复点击/回调），此时无法启动协程
            if (!gameObject.activeInHierarchy)
            {
                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha = 0f;
                    _canvasGroup.interactable = false;
                    _canvasGroup.blocksRaycasts = false;
                }
                gameObject.SetActive(false);
                return;
            }

            StopAllCoroutines();
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

        private void ApplyInfoStyle(string title, string content)
        {
            // 简单判定：包含“成功/完成/通过/已”视为成功；包含“失败/错误/异常/不存在/禁止”视为失败
            string t = (title ?? "") + " " + (content ?? "");
            bool isFail =
                t.Contains("失败") || t.Contains("错误") || t.Contains("异常") ||
                t.Contains("不存在") || t.Contains("禁止") || t.Contains("无效");

            bool isSuccess =
                !isFail && (t.Contains("成功") || t.Contains("完成") || t.Contains("通过") || t.Contains("已"));

            // 未命中时默认用成功图标（更友好）；如需强制，请用 ShowInfo(..., bool isSuccess, ...)
            ApplyInfoStyle(title, content, isSuccess);
        }

        private void ApplyInfoStyle(string title, string content, bool isSuccess)
        {
            if (infoIconImage != null)
            {
                var sp = isSuccess ? passSprite : errorSprite;
                if (sp != null) infoIconImage.sprite = sp;
                infoIconImage.gameObject.SetActive(true);
            }

            if (infoShowAtTopRight)
                MoveToTopRight(infoTopRightMargin);
        }

        private void ApplyConfirmStyle()
        {
            // 确认弹窗：恢复原始布局，隐藏提示图标（如果存在）
            RestoreOriginLayout();
            if (infoIconImage != null) infoIconImage.gameObject.SetActive(false);
        }

        private void MoveToTopRight(Vector2 margin)
        {
            if (_rectTransform == null) return;
            // 右上角定位：锚点与 pivot 都设为右上
            _rectTransform.anchorMin = new Vector2(1f, 1f);
            _rectTransform.anchorMax = new Vector2(1f, 1f);
            _rectTransform.pivot     = new Vector2(1f, 1f);
            _rectTransform.anchoredPosition = new Vector2(-Mathf.Abs(margin.x), -Mathf.Abs(margin.y));
        }

        private void RestoreOriginLayout()
        {
            if (_rectTransform == null) return;
            _rectTransform.anchorMin        = _originAnchorMin;
            _rectTransform.anchorMax        = _originAnchorMax;
            _rectTransform.pivot            = _originPivot;
            _rectTransform.anchoredPosition = _originAnchoredPos;
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
