using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

namespace game_1
{
    public class UIControl1 : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("References")]
        [SerializeField] private GameObject primaryCanvas;
        [SerializeField] private GameObject secondaryCanvas;
        [SerializeField] private float hoverDelay = 0.2f;
        [SerializeField] private float exitGracePeriod = 0.3f; // 离开后延迟隐藏

        private bool isPointerOverPrimary = false;
        private bool isPointerOverSecondary = false;
        private bool isSecondaryCanvasActive = false;

        private bool isLocked = false;  // 点击后锁定，防止自动隐藏

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

            if (hideCoroutine != null)
            {
                StopCoroutine(hideCoroutine);
                hideCoroutine = null;
            }

            if (!isSecondaryCanvasActive && showCoroutine == null)
                showCoroutine = StartCoroutine(ShowAfterDelay());
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerOverPrimary = false;

            if (showCoroutine != null)
            {
                StopCoroutine(showCoroutine);
                showCoroutine = null;
            }

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

            // 只有没锁定且鼠标不在一级和二级界面时才隐藏
            if (!isPointerOverPrimary && !isPointerOverSecondary && !isLocked)
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

        // 点击二级界面时调用，锁定界面不自动隐藏
        public void OnSecondaryPanelClicked()
        {
            isLocked = true;
        }

        // 手动关闭二级界面，解除锁定
        public void CloseSecondaryCanvas()
        {
            isLocked = false;
            HideSecondaryCanvas();
        }
    }
}
