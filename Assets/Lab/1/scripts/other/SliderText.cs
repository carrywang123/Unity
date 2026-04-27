using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace game_1
{
    public class SliderText : MonoBehaviour
    {
        [SerializeField] private Slider slider;
        [SerializeField] private TMP_Text textToMove;
        [SerializeField] private float textMoveRange = 100f; // 文本移动的范围
        [SerializeField] private RectTransform textTransform; // 文本的RectTransform

        private Vector2 textStartPosition;

        private void Start()
        {
            // 如果没有手动指定，尝试自动获取组件
            if (slider == null) slider = GetComponent<Slider>();
            if (textToMove == null) textToMove = GetComponentInChildren<TMP_Text>();
            if (textTransform == null && textToMove != null)
                textTransform = textToMove.GetComponent<RectTransform>();

            // 记录文本初始位置
            if (textTransform != null)
                textStartPosition = textTransform.anchoredPosition;

            // 添加滑动事件监听
            slider.onValueChanged.AddListener(OnSliderValueChanged);
        }

        private void OnSliderValueChanged(float value)
        {
            if (textTransform == null) return;

            // 计算文本的新位置（与Slider值相反）
            float normalizedValue = value / (slider.maxValue - slider.minValue);
            float newY = textStartPosition.y + (normalizedValue * +textMoveRange);

            // 更新文本位置
            textTransform.anchoredPosition = new Vector2(
                textStartPosition.x,
                newY
            );
        }

        private void OnDestroy()
        {
            // 移除事件监听
            slider.onValueChanged.RemoveListener(OnSliderValueChanged);
        }
    }}
