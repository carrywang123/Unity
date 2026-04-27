using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

namespace game_1
{
    public class Countdownjiaoban : MonoBehaviour
    {
        [Header("UI References")]
        public Button confirmButton;       // 新增的确认按钮
        public Button startButton;         // 开始按钮（jiaoban panel）
        public GameObject countdownPanel;  // 倒计时面板
        public TMP_Text countdownText;     // 倒计时文本
        public GameObject operationPromptPanel; // 操作提示面板
        public TMP_Text promptText;        // 操作提示文本
        public Button jiaobanButton;       // 搅拌器按钮
        public Button magneticButton;      // 磁力搅拌器按钮

        [Header("Timer Settings")]
        public float displayTotalTime = 300f; // 显示的总时间（5分钟=300秒）
        public float actualTime = 10f;      // 实际时间（10秒）

        private float speedFactor;
        private float elapsedTime = 0f;
        private bool countdownStarted = false;
        private bool countdownTriggered = false; // 标记是否已触发过倒计时
        private bool clickedJiaoban = false;
        private bool clickedMagnetic = false;

        void Start()
        {
            // 计算加速因子
            speedFactor = displayTotalTime / actualTime;

            // 初始化UI状态
            countdownPanel.SetActive(false);
            operationPromptPanel.SetActive(false);

            // 设置提示文本
            promptText.text = "浸出时间到\r\n请依次关闭恒速搅拌器\r\n磁力搅拌器的加热开关\r\n及电源开关";

            // 绑定按钮事件
            confirmButton.onClick.AddListener(OnConfirmClicked);
            startButton.onClick.AddListener(StartCountdown);
            jiaobanButton.onClick.AddListener(OnJiaobanClicked);
            magneticButton.onClick.AddListener(OnMagneticClicked);

            // 初始状态设置
            startButton.interactable = false; // 初始时startButton不可点击
        }

        void Update()
        {
            if (countdownStarted && countdownPanel.activeSelf)
            {
                elapsedTime += Time.deltaTime * speedFactor;

                if (elapsedTime >= displayTotalTime)
                {
                    elapsedTime = displayTotalTime;
                    OnCountdownFinished();
                    return;
                }

                UpdateCountdownDisplay();
            }
        }

        void OnConfirmClicked()
        {
            // 点击确认按钮后激活startButton
            startButton.interactable = true;
        }

        void StartCountdown()
        {
            // 确保只触发一次
            if (countdownTriggered) return;

            countdownTriggered = true;
            countdownPanel.SetActive(true);
            countdownStarted = true;

            // 重置计时
            elapsedTime = 0f;
            UpdateCountdownDisplay();
        }

        void UpdateCountdownDisplay()
        {
            float remainingTime = displayTotalTime - elapsedTime;
            int minutes = Mathf.FloorToInt(remainingTime / 60f);
            int seconds = Mathf.FloorToInt(remainingTime % 60f);
            countdownText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        }

        void OnCountdownFinished()
        {
            // 隐藏倒计时面板
            countdownPanel.SetActive(false);

            // 延迟1秒显示操作提示面板
            StartCoroutine(ShowPromptAfterDelay(1f));
        }

        IEnumerator ShowPromptAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            // 显示操作提示面板
            operationPromptPanel.SetActive(true);


        }

        void OnJiaobanClicked()
        {
            if (!clickedJiaoban)
            {
                clickedJiaoban = true;

            }
        }

        void OnMagneticClicked()
        {
            if (clickedJiaoban && !clickedMagnetic)
            {
                clickedMagnetic = true;


                // 所有操作完成，隐藏提示面板
                StartCoroutine(HidePromptAfterDelay(1f));
            }
        }

        IEnumerator HidePromptAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            operationPromptPanel.SetActive(false);
            Debug.Log("所有操作已完成！");
        }
    }}
