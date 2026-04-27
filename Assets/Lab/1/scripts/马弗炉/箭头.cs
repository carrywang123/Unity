using UnityEngine;
using UnityEngine.UI;

namespace game_1
{
    public class PanelSwitcher : MonoBehaviour
    {
        // 引用三个 Panel
        public GameObject[] panels;

        // 引用左右按钮
        public Button leftButton;
        public Button rightButton;

        private int currentPanelIndex = 0; // 当前显示的 Panel 索引

        void Start()
        {
            // 初始化 Panel 状态
            UpdatePanels();

            // 绑定按钮点击事件
            leftButton.onClick.AddListener(ShowPreviousPanel);
            rightButton.onClick.AddListener(ShowNextPanel);
        }

        // 显示上一个 Panel
        void ShowPreviousPanel()
        {
            if (currentPanelIndex > 0)
            {
                currentPanelIndex--;
                UpdatePanels();
            }
        }

        // 显示下一个 Panel
        void ShowNextPanel()
        {
            if (currentPanelIndex < panels.Length - 1)
            {
                currentPanelIndex++;
                UpdatePanels();
            }
        }

        // 更新 Panel 显示状态
        void UpdatePanels()
        {
            for (int i = 0; i < panels.Length; i++)
            {
                panels[i].SetActive(i == currentPanelIndex);
            }

            // 更新按钮状态
            leftButton.interactable = (currentPanelIndex > 0);
            rightButton.interactable = (currentPanelIndex < panels.Length - 1);
        }
    }}
