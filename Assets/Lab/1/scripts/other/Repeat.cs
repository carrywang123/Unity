using UnityEngine;
using UnityEngine.UI;

namespace game_1
{
    public class Repeat : MonoBehaviour
    {
        // 在Inspector中拖拽对应的UI元素到这些字段
        public GameObject repeatExperimentPanel;
        public GameObject pourWastePanel;
        public GameObject repeatCanvas;
        public Button confirmButton;

        // 记录状态
        private bool hasClickedPourWaste = false;
        private bool hasClickedRepeatExperiment = false;
        private Button repeatExperimentButton;

        void Start()
        {
            // 初始化UI状态
            repeatCanvas.SetActive(false);

            // 获取重复实验Panel的Button组件
            repeatExperimentButton = repeatExperimentPanel.GetComponent<Button>();
            if (repeatExperimentButton == null)
            {
                repeatExperimentButton = repeatExperimentPanel.AddComponent<Button>();
            }

            // 添加点击事件监听
            pourWastePanel.GetComponent<Button>().onClick.AddListener(OnPourWasteClicked);
            repeatExperimentButton.onClick.AddListener(OnRepeatExperimentClicked);
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }

        void OnPourWasteClicked()
        {
            // 记录已点击倒入废液桶
            hasClickedPourWaste = true;
        }

        void OnRepeatExperimentClicked()
        {
            // 如果已经点击过重复实验，直接返回
            if (hasClickedRepeatExperiment) return;

            // 如果之前点击过倒入废液桶，显示提示Canvas
            if (hasClickedPourWaste)
            {
                repeatCanvas.SetActive(true);
            }

            // 标记已点击重复实验
            hasClickedRepeatExperiment = true;

            // 禁用重复实验按钮
            repeatExperimentButton.interactable = false;
        }

        void OnConfirmClicked()
        {
            // 隐藏提示Canvas
            repeatCanvas.SetActive(false);
        }
    }}
