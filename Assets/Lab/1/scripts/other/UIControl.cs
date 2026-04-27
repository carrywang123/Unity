using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

namespace game_1
{
    public class UIControl : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("References")]
        [SerializeField] private GameObject primaryCanvas;
        [SerializeField] private GameObject secondaryCanvas;
        [SerializeField] private float hoverDelay = 0.2f;
        [SerializeField] private float exitGracePeriod = 0.3f; // 离开后延迟隐藏

        private bool isPointerOverPrimary = false;
        private bool isPointerOverSecondary = false;
        private bool isSecondaryCanvasActive = false;

        private Coroutine showCoroutine;
        private Coroutine hideCoroutine;

        private void Start()
        {
            if (secondaryCanvas != null)
                secondaryCanvas.SetActive(false);
        }

        private void Update()
        {
            UpdatePointerOverSecondaryCanvas();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isPointerOverPrimary = true;

            // 如果准备隐藏，取消隐藏
            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
                hideCoroutine = null;
            }

            // 启动显示
            if (!isSecondaryCanvasActive && showCoroutine == null)
                showCoroutine = StartCoroutine(ShowAfterDelay());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerOverPrimary = false;

            // 如果显示还没完成，就取消
            if (showCoroutine != null)
            {
                StopCoroutine(showCoroutine);
                showCoroutine = null;
            }

            // 启动隐藏倒计时（留个宽容时间）
            if (hideCoroutine == null)
                hideCoroutine = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator ShowAfterDelay()
        {
            yield return new WaitForSeconds(hoverDelay);

            if (isPointerOverPrimary)
                ShowSecondaryCanvas();

            showCoroutine = null;
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(exitGracePeriod);

            if (!isPointerOverPrimary && !isPointerOverSecondary)
                HideSecondaryCanvas();

            hideCoroutine = null;
        }

        private void ShowSecondaryCanvas()
        {
            if (!isSecondaryCanvasActive && secondaryCanvas != null)
            {
                secondaryCanvas.SetActive(true);
                isSecondaryCanvasActive = true;
            }
        }

        private void HideSecondaryCanvas()
        {
            if (isSecondaryCanvasActive && secondaryCanvas != null)
            {
                secondaryCanvas.SetActive(false);
                isSecondaryCanvasActive = false;
            }
        }

        private void UpdatePointerOverSecondaryCanvas()
        {
            if (secondaryCanvas == null || !secondaryCanvas.activeInHierarchy)
            {
                isPointerOverSecondary = false;
                return;
            }

            PointerEventData eventData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            bool prev = isPointerOverSecondary;
            isPointerOverSecondary = false;

            foreach (var result in results)
            {
                if (result.gameObject.transform.IsChildOf(secondaryCanvas.transform))
                {
                    isPointerOverSecondary = true;
                    break;
                }
            }

            // 如果状态变化 && 当前不在一级Canvas上，重启退出倒计时
            if (!isPointerOverPrimary && prev != isPointerOverSecondary)
            {
                if (hideCoroutine != null)
                {
                    StopCoroutine(hideCoroutine);
                    hideCoroutine = null;
                }

                if (!isPointerOverSecondary)
                    hideCoroutine = StartCoroutine(HideAfterDelay());
            }
        }

        public void OnSecondaryPanelClicked()
        {
            if (primaryCanvas != null)
                primaryCanvas.SetActive(false);

            HideSecondaryCanvas();
        }
    }
}
