using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

namespace game_1
{
    public class Countdownhonggan : MonoBehaviour
    {
        [Header("UI References")]
        public GameObject countdownPanel;      // 倒计时面板
        public TMP_Text countdownText;         // 倒计时文本
        public GameObject operationPromptPanel; // 操作提示面板
        public TMP_Text promptText;            // 操作提示文本
        public Button fengjiPanel;             // 风机面板按钮
        public Button powerPanel;              // 电源面板按钮

        [Header("Timer Settings")]
        public float displayTotalTime = 3600f; // 显示的总时间（1小时=3600秒）
        public float actualTime = 20f;         // 实际时间（20秒）

        private float speedFactor;
        private float elapsedTime = 0f;
        private bool countdownStarted = false;
        private bool countdownTriggered = false; // 标记是否已触发倒计时
        private bool clickedFengji = false;
        private bool clickedPower = false;

        void Start()
        {
            // 计算加速因子
            speedFactor = displayTotalTime / actualTime;

            // 初始化UI状态
            countdownPanel.SetActive(false);
            operationPromptPanel.SetActive(false);

            // 设置提示文本
            promptText.text = "已烘干一小时\n请依次关闭风机开关、电源开关";

            // 绑定按钮事件
            fengjiPanel.onClick.AddListener(OnFengjiClicked);
            powerPanel.onClick.AddListener(OnPowerClicked);
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

        void OnFengjiClicked()
        {
            // 仅第一次点击风机面板时触发倒计时
            if (!countdownTriggered)
            {
                countdownTriggered = true;
                StartCountdown();
            }
            // 如果提示面板已激活，则记录点击顺序
            else if (operationPromptPanel.activeSelf && !clickedFengji)
            {
                clickedFengji = true;
            }
        }

        void OnPowerClicked()
        {
            // 必须在点击风机之后才能点击电源
            if (clickedFengji && !clickedPower && operationPromptPanel.activeSelf)
            {
                clickedPower = true;
                StartCoroutine(HidePromptAfterDelay(1f));
            }
        }

        void StartCountdown()
        {
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

        IEnumerator HidePromptAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            operationPromptPanel.SetActive(false);
            Debug.Log("所有操作已完成！");

            // 重置状态以便下次使用
            clickedFengji = false;
            clickedPower = false;
        }
    }}
