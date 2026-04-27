using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace game_1
{
    public class TemButton : MonoBehaviour
    {
        [Header("UI组件引用")]
        public GameObject temperaturePanel;       // 温度控制面板
        public TextMeshProUGUI realTimeTempText;  // 实时温度显示文本
        public TextMeshProUGUI targetTempText;    // 目标温度显示文本
        public TextMeshProUGUI[] digits;          // 数字输入位数组（百位、十位、个位）
        public Button airSwitchBtn;               // 空气开关按钮
        public Button mainPowerBtn;               // 主电源按钮
        public Button upBtn;                      // 上调按钮
        public Button downBtn;                    // 下调按钮
        public Button leftBtn;                    // 左移按钮（切换数字位）
        public Button switchBtn;                  // 切换模式按钮

        [Header("系统设置")]
        public float tempIncreaseSpeed = 1f;      // 温度上升速度
        public float digitBlinkInterval = 0.5f;   // 数字闪烁间隔
        public float operationTimeout = 30f;      // 操作超时时间

        // 系统状态枚举
        private enum SystemState { Off, Setting, Ready, Running }
        // 温度类型枚举（C=当前温度，T=目标温度）
        private enum TempType { C, T }

        private SystemState currentState = SystemState.Off;  // 当前系统状态
        private TempType currentTempType = TempType.C;       // 当前温度类型
        private int currentSegment = 1;                      // 当前段号（1-6）
        private int selectedDigitIndex = 0;                  // 当前选中的数字位索引

        private bool[] powerStates = new bool[2];            // 电源状态数组（0=空气开关，1=主电源）
        private Dictionary<string, int> tempSettings = new Dictionary<string, int>(); // 温度设置字典

        private Coroutine blinkCoroutine;                    // 数字闪烁协程
        private Coroutine tempRoutine;                       // 温度控制协程
        private Coroutine furnaceOperationRoutine;          // 熔炉操作协程
        private Coroutine displayRoutine;                   // 显示更新协程

        private int furnaceOperationStep = 0;               // 熔炉操作步骤
        private bool furnaceOperationCompleted;             // 熔炉操作完成标志

        void Start()
        {
            InitializeSystem();     // 初始化系统
            SetupButtonListeners(); // 设置按钮监听

            // 初始隐藏温度显示
            realTimeTempText.gameObject.SetActive(false);
            targetTempText.gameObject.SetActive(false);
            UpdateUIState();        // 更新UI状态
        }

        // 初始化系统设置
        void InitializeSystem()
        {
            tempSettings.Clear();
            // 初始化6个段的C和T温度设置
            for (int i = 1; i <= 6; i++)
            {
                tempSettings[$"C{i:D2}"] = 0;  // 当前温度
                tempSettings[$"T{i:D2}"] = 0;  // 目标温度
            }
        }

        // 设置按钮点击事件监听
        void SetupButtonListeners()
        {
            airSwitchBtn.onClick.AddListener(() => UpdatePowerState(0));  // 空气开关
            mainPowerBtn.onClick.AddListener(() => UpdatePowerState(1));  // 主电源
            upBtn.onClick.AddListener(() => AdjustDigit(1));              // 数字+
            downBtn.onClick.AddListener(() => AdjustDigit(-1));           // 数字-
            leftBtn.onClick.AddListener(SwitchDigit);                     // 切换数字位
            switchBtn.onClick.AddListener(SwitchMode);                    // 切换模式
        }

        // 更新电源状态
        void UpdatePowerState(int index)
        {
            powerStates[index] = !powerStates[index];  // 切换电源状态

            UpdateUIState();  // 更新UI显示

            // 如果两个开关都打开
            if (powerStates[0] && powerStates[1])
            {
                // 显示温度文本
                realTimeTempText.gameObject.SetActive(true);
                targetTempText.gameObject.SetActive(true);

                currentState = SystemState.Setting;  // 进入设置状态
                InitializeSettingsDisplay();         // 初始化设置显示
            }
            else
            {
                // 隐藏温度文本
                realTimeTempText.gameObject.SetActive(false);
                targetTempText.gameObject.SetActive(false);
            }
        }

        // 更新UI状态
        void UpdateUIState()
        {
            temperaturePanel.SetActive(powerStates[0] && powerStates[1]);  // 只有两个开关都打开才显示面板

        }

        // 初始化设置显示
        void InitializeSettingsDisplay()
        {
            currentSegment = 1;            // 从第一段开始
            currentTempType = TempType.C;  // 默认显示当前温度
            UpdateDisplay();              // 更新显示
            StartBlinking();              // 开始数字闪烁
        }

        // 更新显示内容
        void UpdateDisplay()
        {
            string currentKey = $"{currentTempType}{currentSegment:D2}";  // 生成当前键（如C01）
            int value = tempSettings[currentKey];  // 获取当前值

            // 更新目标温度显示（3位数）
            targetTempText.text = value.ToString("D3");

            // 更新实时温度显示（显示当前段号）
            realTimeTempText.text = $"{currentTempType}{currentSegment:D2}";

            // 更新各个数字位显示
            digits[0].text = (value / 100).ToString();        // 百位
            digits[1].text = ((value % 100) / 10).ToString();  // 十位
            digits[2].text = (value % 10).ToString();         // 个位
        }

        // 调整数字值
        void AdjustDigit(int direction)
        {
            if (currentState != SystemState.Setting) return;  // 非设置状态不响应

            string key = $"{currentTempType}{currentSegment:D2}";
            int value = tempSettings[key];

            // 分解数字位
            int[] parts = { value / 100, (value % 100) / 10, value % 10 };
            // 调整选中位的值（0-9范围内）
            parts[selectedDigitIndex] = Mathf.Clamp(parts[selectedDigitIndex] + direction, 0, 9);

            // 重新组合数字并保存
            tempSettings[key] = parts[0] * 100 + parts[1] * 10 + parts[2];
            UpdateDisplay();  // 更新显示
        }

        // 切换选中的数字位
        void SwitchDigit()
        {
            if (currentState != SystemState.Setting) return;  // 非设置状态不响应

            // 循环切换数字位（百位←十位←个位）
            selectedDigitIndex = (selectedDigitIndex - 1 + 3) % 3;
            StopBlinking();   // 停止当前闪烁
            StartBlinking();  // 开始新选中位的闪烁
        }

        // 开始数字闪烁效果
        void StartBlinking()
        {
            if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
            blinkCoroutine = StartCoroutine(BlinkDigit());
        }

        // 停止数字闪烁效果
        void StopBlinking()
        {
            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
                // 恢复所有数字位为正常显示
                foreach (var digit in digits)
                {
                    digit.color = Color.white;
                }
            }
        }

        // 数字闪烁协程
        IEnumerator BlinkDigit()
        {
            while (true)
            {
                // 半透明状态
                digits[selectedDigitIndex].color = new Color(1, 1, 1, 0.5f);
                yield return new WaitForSeconds(digitBlinkInterval);

                // 恢复正常状态
                digits[selectedDigitIndex].color = Color.white;
                yield return new WaitForSeconds(digitBlinkInterval);
            }
        }

        // 切换模式（C/T）或段号
        void SwitchMode()
        {
            if (currentState == SystemState.Setting)
            {
                if (currentTempType == TempType.C)
                {
                    // 切换到目标温度设置
                    currentTempType = TempType.T;
                    realTimeTempText.text = $"T{currentSegment:D2}";
                }
                else
                {
                    // 切换回当前温度设置，并递增段号
                    currentTempType = TempType.C;
                    currentSegment++;

                    if (currentSegment <= 6)
                    {
                        realTimeTempText.text = $"C{currentSegment:D2}";
                    }

                    // 如果所有段都设置完成
                    if (currentSegment > 6)
                    {
                        currentState = SystemState.Ready;  // 进入准备状态
                        targetTempText.text = "STOP";     // 显示停止状态
                        realTimeTempText.text = "READY";   // 显示准备就绪
                        StopBlinking();                   // 停止数字闪烁
                        return;
                    }
                }
                UpdateDisplay();  // 更新显示
            }
        }

        // 运行按钮点击事件（需在Unity编辑器中关联此方法）
        public void OnRunButtonClicked()
        {
            if (currentState == SystemState.Ready)
            {
                currentState = SystemState.Running;  // 进入运行状态
                currentSegment = 1;                 // 从第一段开始

                // 启动温度控制协程
                if (tempRoutine != null) StopCoroutine(tempRoutine);
                tempRoutine = StartCoroutine(TemperatureControlRoutine());

                // 启动目标温度显示更新协程
                if (displayRoutine != null) StopCoroutine(displayRoutine);
                displayRoutine = StartCoroutine(UpdateTargetTempDisplay());
            }
        }

        // 更新目标温度显示协程
        IEnumerator UpdateTargetTempDisplay()
        {
            while (currentState == SystemState.Running)
            {
                // 校验段号有效性
                if (currentSegment < 1 || currentSegment > 6)
                {
                    Debug.LogWarning($"异常段号重置: {currentSegment} -> 1");
                    currentSegment = 1;
                }

                string currentKey = $"C{currentSegment:D2}";
                if (!tempSettings.ContainsKey(currentKey))
                {
                    Debug.LogError($"无效的温度键: {currentKey}");
                    yield break;
                }

                int targetTemp = tempSettings[currentKey];

                // 闪烁显示"RUN"
                targetTempText.text = "RUN";
                yield return new WaitForSeconds(1f);

                // 显示目标温度
                targetTempText.text = targetTemp.ToString("D3");
                yield return new WaitForSeconds(1f);
            }
        }

        // 温度控制协程
        IEnumerator TemperatureControlRoutine()
        {
            while (currentState == SystemState.Running)
            {
                currentSegment = Mathf.Clamp(currentSegment, 1, 6);  // 确保段号有效

                string currentKey = $"C{currentSegment:D2}";
                if (!tempSettings.ContainsKey(currentKey))
                {
                    Debug.LogError($"无效的温度键: {currentKey}");
                    yield break;
                }

                int targetTemp = tempSettings[currentKey];
                // 初始温度：第一段从0开始，其他段从前一段的结束温度开始
                float initialTemp = currentSegment == 1 ? 0 : tempSettings[$"C{currentSegment - 1:D2}"];
                float currentTemp = initialTemp;

                realTimeTempText.text = currentTemp.ToString("F0");  // 显示初始温度

                // 温度上升过程
                while (currentTemp < targetTemp)
                {
                    currentTemp += Time.deltaTime * tempIncreaseSpeed;
                    currentTemp = Mathf.Min(currentTemp, targetTemp);  // 不超过目标温度
                    realTimeTempText.text = currentTemp.ToString("F0");
                    yield return null;
                }

                // 达到目标温度
                realTimeTempText.text = targetTemp.ToString();
                yield return new WaitForSeconds(10f);  // 保持10秒

                // 启动熔炉操作流程
                furnaceOperationRoutine = StartCoroutine(WaitForFurnaceOperation());
                yield return furnaceOperationRoutine;  // 等待操作完成

                // 进入下一段
                currentSegment++;
                if (currentSegment > 6)
                {
                    // 所有段完成，回到准备状态
                    currentSegment = 1;
                    realTimeTempText.text = "000";
                    currentState = SystemState.Ready;
                    targetTempText.text = "STOP";
                    if (displayRoutine != null) StopCoroutine(displayRoutine);
                    yield break;
                }
            }
        }

        // 等待熔炉操作协程
        IEnumerator WaitForFurnaceOperation()
        {
            furnaceOperationCompleted = false;
            furnaceOperationStep = 0;

            SystemState originalState = currentState;
            currentState = SystemState.Off;  // 临时关闭系统状态

            float timeoutTimer = 0f;

            // 等待操作完成或超时
            while (!furnaceOperationCompleted && timeoutTimer < operationTimeout)
            {
                timeoutTimer += Time.deltaTime;
                yield return null;
            }

            if (timeoutTimer >= operationTimeout)
            {
                Debug.LogError("操作超时！");
                yield break;
            }

            currentState = originalState;  // 恢复系统状态
            furnaceOperationStep = 0;
            furnaceOperationCompleted = false;
        }

        // 面板闪烁效果（可用于提示）
        IEnumerator FlashPanel(GameObject panel, Color color)
        {
            Image img = panel.GetComponent<Image>();
            Color original = img.color;
            img.color = color;
            yield return new WaitForSeconds(0.5f);
            img.color = original;
        }
    }}
