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

        [Header("=== 进度条平滑设置 ===")]
        [Tooltip("进度条快速冲到 90% 的速度（每秒变化量）")]
        public float fastSpeedTo90 = 6f;

        [Tooltip("最后 10%（0.9 -> 1.0）慢速收尾的时长（秒）")]
        public float slowTailSeconds = 0.8f;

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

        private Coroutine _progressCoroutine;
        private float _targetProgress = 0f;
        private float _displayProgress = 0f;
        private bool _tailMode = false;
        private float _tailStartTime = -1f;
        private float _tailStartValue = 0f;

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

            // 进度条协程（如果有进度条组件）
            if (progressBar != null && _progressCoroutine == null)
                _progressCoroutine = StartCoroutine(ProgressAnimation());
        }

        private void OnDisable()
        {
            _isSpinning = false;
            if (_dotCoroutine != null)
            {
                StopCoroutine(_dotCoroutine);
                _dotCoroutine = null;
            }

            if (_progressCoroutine != null)
            {
                StopCoroutine(_progressCoroutine);
                _progressCoroutine = null;
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
                float v = Mathf.Clamp01(value);
                _targetProgress = v;

                // 如果目标要到 1，进入“最后 10% 慢速收尾”模式
                if (Mathf.Approximately(v, 1f))
                {
                    _tailMode = true;
                    _tailStartTime = -1f; // 延迟到真正进入 0.9->1.0 时再记录
                }
            }
        }

        public void HideProgress()
        {
            if (progressBar != null)
                progressBar.gameObject.SetActive(false);
        }

        #endregion

        private IEnumerator ProgressAnimation()
        {
            // 使用 unscaledTime，避免 Time.timeScale=0 时卡住 UI 动画
            while (true)
            {
                if (progressBar == null || !progressBar.gameObject.activeInHierarchy)
                {
                    yield return null;
                    continue;
                }

                float dt = Time.unscaledDeltaTime;

                float fastSpd = Mathf.Max(0.01f, fastSpeedTo90);
                float tailDur = Mathf.Max(0.05f, slowTailSeconds);

                // 第一段：快速冲到 min(target, 0.9)
                float stage1Target = Mathf.Min(_targetProgress, 0.9f);
                _displayProgress = Mathf.MoveTowards(_displayProgress, stage1Target, fastSpd * dt);

                // 第二段：只有 target=1 时，最后 10% 慢速到顶
                if (_tailMode && Mathf.Approximately(_targetProgress, 1f))
                {
                    // 等第一段到 0.9 后才开始慢速收尾
                    if (_displayProgress >= 0.9f - 0.0001f)
                    {
                        if (_tailStartTime < 0f)
                        {
                            _tailStartTime = Time.unscaledTime;
                            _tailStartValue = _displayProgress;
                        }

                        float t = (Time.unscaledTime - _tailStartTime) / tailDur;
                        _displayProgress = Mathf.Lerp(_tailStartValue, 1f, Mathf.Clamp01(t));

                        if (_displayProgress >= 0.9999f)
                        {
                            _displayProgress = 1f;
                            _tailMode = false;
                            _tailStartTime = -1f;
                        }
                    }
                }

                progressBar.value = Mathf.Clamp01(_displayProgress);
                yield return null;
            }
        }

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
