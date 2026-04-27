using UnityEngine;
using UnityEngine.EventSystems;

namespace game_1
{
    public class CanvasOnClick : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private GameObject targetCanvas; // 要显示/隐藏的目标Canvas

        private void Awake()
        {
            // 确保目标Canvas初始状态与当前设置一致
            if (targetCanvas != null)
            {
                targetCanvas.SetActive(false);
            }
        }

        // 当Panel被点击时调用
        public void OnPointerClick(PointerEventData eventData)
        {
            // 切换目标Canvas的激活状态
            if (targetCanvas != null)
            {
                targetCanvas.SetActive(!targetCanvas.activeSelf);
            }
        }
    }}
