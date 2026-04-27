using UnityEngine;
using TMPro;
using System.Collections;

namespace game_1
{
    public class TemperatureController : MonoBehaviour
    {
        [Header("UI Components")]
        public TextMeshProUGUI[] numberDisplays; // 三个数字显示框（个位、十位、百位）
        public TextMeshProUGUI resultDisplay;    // 结果显示框
        public GameObject powerButton;           // 电源按钮对象
        public GameObject setButton;             // 设置按钮对象
        public GameObject leftButton;            // 左切换按钮
        public GameObject upButton;              // 上增加按钮
        public GameObject downButton;            // 下减少按钮

        [Header("Settings")]
        private int currentIndex = 0;            // 当前选中的数字框索引（0=个位，1=十位，2=百位）
        private int[] numberValues = { 0, 2, 0 };// 存储三个数字值（默认十位为2）
        private Coroutine blinkCoroutine;        // 闪烁效果协程
        private Coroutine heatingCoroutine;      // 加热动画协程
        private bool isSettingMode = false;      // 是否处于设置模式
        private bool isPowerOn = false;          // 电源状态

        // 电源按钮点击事件
        public void OnPowerButtonClick()
        {
            isPowerOn = !isPowerOn; // 切换电源状态


            if (isPowerOn)
            {
                // 开机操作
                ShowAllDisplays();
                resultDisplay.text = "20"; // 默认显示20度
                isSettingMode = false;    // 初始状态为非设置模式
            }
            else
            {
                // 关机操作
                TurnOffAllDisplays();
                isSettingMode = false;     // 关机时退出设置模式
            }
        }

        // 设置按钮点击事件
        public void OnSetButtonClick()
        {
            if (!isPowerOn) return;

            if (isSettingMode)
            {
                // 结束设置模式，开始加热
                isSettingMode = false;
                ShowAllDisplays();
                StartHeating();
            }
            else
            {
                // 进入设置模式
                isSettingMode = true;
                currentIndex = 0; // 默认从个位开始设置
                if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
                blinkCoroutine = StartCoroutine(BlinkEffect(currentIndex));
            }
        }

        // 显示所有数字框（非设置模式）
        private void ShowAllDisplays()
        {
            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
            }

            for (int i = 0; i < numberDisplays.Length; i++)
            {
                numberDisplays[i].text = numberValues[i].ToString();
            }
        }

        // 关闭所有显示
        private void TurnOffAllDisplays()
        {
            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
            }

            foreach (var display in numberDisplays)
            {
                display.text = "";
            }

            resultDisplay.text = "";
        }

        // 左按钮点击事件（切换选中的数字位）
        // 左按钮点击事件（切换选中的数字位）
        public void OnLeftButtonClick()
        {
            if (!isPowerOn || !isSettingMode) return;

            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
            }

            // 先恢复所有数字的显示（确保非选中数字不闪烁）
            for (int i = 0; i < numberDisplays.Length; i++)
            {
                numberDisplays[i].text = numberValues[i].ToString();
            }

            // 切换到下一个数字位
            currentIndex = (currentIndex + 1) % numberDisplays.Length;

            // 开始新选中数字的闪烁效果
            blinkCoroutine = StartCoroutine(BlinkEffect(currentIndex));
        }

        // 闪烁效果（当前选中的数字位）
        private IEnumerator BlinkEffect(int index)
        {
            while (isPowerOn && isSettingMode)
            {
                // 只闪烁当前选中的数字位
                numberDisplays[index].text = "";
                yield return new WaitForSeconds(0.5f);

                numberDisplays[index].text = numberValues[index].ToString();
                yield return new WaitForSeconds(0.5f);

                // 确保其他数字位保持显示
                for (int i = 0; i < numberDisplays.Length; i++)
                {
                    if (i != index)
                    {
                        numberDisplays[i].text = numberValues[i].ToString();
                    }
                }
            }
        }
        // 上按钮点击事件（增加当前数字位的值）
        public void OnUpButtonClick()
        {
            if (!isPowerOn || !isSettingMode) return;

            numberValues[currentIndex] = (numberValues[currentIndex] + 1) % 10;
            UpdateDisplay(currentIndex);
        }

        // 下按钮点击事件（减少当前数字位的值）
        public void OnDownButtonClick()
        {
            if (!isPowerOn || !isSettingMode) return;

            numberValues[currentIndex] = (numberValues[currentIndex] - 1 + 10) % 10;
            UpdateDisplay(currentIndex);
        }

        // 开始加热
        private void StartHeating()
        {
            int targetTemp = numberValues[2] * 100 + numberValues[1] * 10 + numberValues[0];
            int startTemp = 20; // 从20度开始加热

            if (heatingCoroutine != null)
            {
                StopCoroutine(heatingCoroutine);
            }

            heatingCoroutine = StartCoroutine(HeatingAnimation(startTemp, targetTemp, 2f));
        }

        // 更新显示
        private void UpdateDisplay(int index)
        {
            numberDisplays[index].text = numberValues[index].ToString();
        }

        // 闪烁效果（当前选中的数字位）


        // 加热动画
        private IEnumerator HeatingAnimation(int startTemp, int targetTemp, float duration)
        {
            float elapsedTime = 0f;
            int currentTemp = startTemp;
            resultDisplay.text = currentTemp.ToString();

            while (elapsedTime < duration)
            {
                elapsedTime += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsedTime / duration);
                currentTemp = Mathf.RoundToInt(Mathf.Lerp(startTemp, targetTemp, progress));
                resultDisplay.text = currentTemp.ToString();
                yield return null;
            }

            resultDisplay.text = targetTemp.ToString();
        }

        // 初始化
        private void Start()
        {
            // 初始状态为关机
            isPowerOn = false;
            TurnOffAllDisplays();
        }
    }}
