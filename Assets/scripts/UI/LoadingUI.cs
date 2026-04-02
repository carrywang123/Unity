// ============================================================
// 文件名：LoadingUI.cs
// 功  能：加载遮罩组件（旋转动画 + 提示文本）
// 作  者：化工虚拟仿真实验平台
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ChemLab.UI
{
    public class LoadingUI : MonoBehaviour
    {
        [Header("=== 组件引用 ===")]
        [Tooltip("旋转的加载图标")]
        public RectTransform spinnerIcon;

        [Tooltip("加载提示文本")]
        public Text loadingText;

        [Tooltip("进度条（可选）")]
        public Slider progressBar;

        [Header("=== 旋转设置 ===")]
        [Tooltip("旋转速度（度/秒）")]
        public float rotateSpeed = 180f;

        [Header("=== 点点动画 ===")]
        [Tooltip("是否启用省略号动画")]
        public bool enableDotAnimation = true;

        // ── 私有变量 ──────────────────────────────────────────
        private bool      _isSpinning = false;
        private Coroutine _dotCoroutine;
        private string    _baseText = "加载中";

        // ─────────────────────────────────────────────────────
        #region Unity 生命周期
        // ─────────────────────────────────────────────────────

        private void OnEnable()
        {
            _isSpinning = true;
            if (enableDotAnimation && loadingText != null)
            {
                _baseText    = loadingText.text;
                _dotCoroutine = StartCoroutine(DotAnimation());
            }
        }

        private void OnDisable()
        {
            _isSpinning = false;
            if (_dotCoroutine != null)
            {
                StopCoroutine(_dotCoroutine);
                _dotCoroutine = null;
            }
        }

        private void Update()
        {
            if (_isSpinning && spinnerIcon != null)
            {
                spinnerIcon.Rotate(0f, 0f, -rotateSpeed * Time.deltaTime);
            }
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region 公开接口
        // ─────────────────────────────────────────────────────

        public void SetText(string text)
        {
            _baseText = text;
            if (loadingText != null) loadingText.text = text;
        }

        public void SetProgress(float value)
        {
            if (progressBar != null)
            {
                progressBar.gameObject.SetActive(true);
                progressBar.value = Mathf.Clamp01(value);
            }
        }

        public void HideProgress()
        {
            if (progressBar != null)
                progressBar.gameObject.SetActive(false);
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region 省略号动画
        // ─────────────────────────────────────────────────────

        private IEnumerator DotAnimation()
        {
            string[] dots = { "", ".", "..", "..." };
            int index = 0;

            while (true)
            {
                if (loadingText != null)
                    loadingText.text = _baseText + dots[index % dots.Length];
                index++;
                yield return new WaitForSeconds(0.4f);
            }
        }

        #endregion
    }
}
