using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChemLab.Utils
{
    /// <summary>
    /// 挂在 UI 可点击目标上：
    /// - hover 时切换为手型光标
    /// - hover 时按钮轻微上移，exit/disable 后回归原位
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UICursorHoverTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Hover 动效")]
        [Tooltip("hover 时向上移动的像素（anchoredPosition.y 增量）")]
        public float hoverMoveUp = 6f;

        [Tooltip("移动动画时长（秒）")]
        public float moveDuration = 0.08f;

        private Selectable _selectable;
        private RectTransform _rectTransform;
        private Vector2 _baseAnchoredPos;
        private Coroutine _moveCoroutine;

        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
            _rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            CaptureBasePosRealtime();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if(ShouldDisableMoveUpByHierarchy()) return;
            if (_selectable != null && !_selectable.IsInteractable()) return;
            UICursor.SetHand();
            CaptureBasePosRealtime();
            MoveTo(_baseAnchoredPos + new Vector2(0f, hoverMoveUp));
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if(ShouldDisableMoveUpByHierarchy()) return;
            UICursor.SetDefault();
            CaptureBasePosRealtimeFromHovered(hoverMoveUp);
            MoveBackToBase();
        }

        private void OnDisable()
        {
            // 避免按钮被隐藏/销毁时光标卡在手型
            UICursor.SetDefault();

            // 避免面板切换时位置卡住
            MoveBackToBase(immediate: true);
        }

        private void CaptureBasePosIfNeeded()
        {
            // 兼容旧调用点：改为实时捕获
            CaptureBasePosRealtime();
        }

        private void CaptureBasePosRealtime()
        {
            if (_rectTransform == null) return;
            _baseAnchoredPos = _rectTransform.anchoredPosition;
        }

        private void CaptureBasePosRealtimeFromHovered(float appliedMoveUp)
        {
            if (_rectTransform == null) return;
            // 退出时当前通常处于“上移后”的位置，回退基准应当是实时位置减去上移量
            _baseAnchoredPos = _rectTransform.anchoredPosition - new Vector2(0f, appliedMoveUp);
        }

        private bool ShouldDisableMoveUpByHierarchy()
        {
            // 规则：父物体名字符合 yyyy-MM-dd（或 yyyyy-MM-dd）则不上移
            var p = transform.parent;
            if (p == null) return false;

            var name = p.name;
            if (string.IsNullOrEmpty(name)) return false;

            // 兼容：你提到的 yyyyy（5位年）以及常见的 yyyy（4位年）
            return DateTime.TryParseExact(name, "yyyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
                   || DateTime.TryParseExact(name, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
        }

        private void MoveBackToBase(bool immediate = false)
        {
            MoveTo(_baseAnchoredPos, immediate);
        }

        private void MoveTo(Vector2 target, bool immediate = false)
        {
            if (_rectTransform == null) return;

            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }

            if (immediate || moveDuration <= 0f)
            {
                _rectTransform.anchoredPosition = target;
                return;
            }

            _moveCoroutine = StartCoroutine(MoveCoroutine(target, moveDuration));
        }

        private System.Collections.IEnumerator MoveCoroutine(Vector2 target, float duration)
        {
            Vector2 start = _rectTransform.anchoredPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
                _rectTransform.anchoredPosition = Vector2.LerpUnclamped(start, target, t);
                yield return null;
            }

            _rectTransform.anchoredPosition = target;
            _moveCoroutine = null;
        }
    }
}

