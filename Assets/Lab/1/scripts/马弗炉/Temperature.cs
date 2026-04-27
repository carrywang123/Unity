using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

namespace game_1
{
    public class Temperature : MonoBehaviour
    {
        // [Header部分保持原有声明不变]
        [Header("New UI Elements")]
        public Button heatButton;
        public GameObject runButtonPanel;
        public Button confirmButton;
        public GameObject monitorLight;

        [Header("Input Fields")]
        public InputField[] cInputFields; // C01-C06
        public InputField[] tInputFields; // T01-T06

        [Header("UI References")]
        public GameObject temperaturePanel;
        public TextMeshProUGUI realTimeTempText;
        public TextMeshProUGUI targetTempText;
        public TextMeshProUGUI[] digits;
        public Button airSwitchBtn;
        public Button mainPowerBtn;

        public GameObject furnacePromptPanel;
        public TextMeshProUGUI operationPromptText;

        [Header("Furnace Panels")]
        public GameObject furnaceOpenClosePanel;
        public GameObject furnaceLoadPanel;

        [Header("Settings")]
        public float tempIncreaseSpeed = 100f;
        public float digitBlinkInterval = 0.5f;
        public float operationTimeout = 30f;
        public float holdingTime = 6f;

        private enum SystemState { Off, Setting, Ready, Running, Paused }
        private enum TempType { C, T }
        private enum OperationPhase { PutIn, TakeOut }

        private SystemState currentState = SystemState.Off;
        private TempType currentTempType = TempType.C;
        private OperationPhase currentPhase;
        private int currentSegment = 1;
        private int selectedDigitIndex = 0;
        private bool isTemperaturePanelClicked = false;

        private bool[] powerStates = new bool[2];
        private Dictionary<string, int> tempSettings = new Dictionary<string, int>();

        private Coroutine blinkCoroutine;
        private Coroutine tempRoutine;
        private Coroutine operationRoutine;
        private Coroutine displayRoutine;

        private int furnaceOperationStep = 0;
        private bool furnaceOperationCompleted;
        private bool inputsValid;
        private bool isFirstSegment = true;
        [Header("Exit Button")]
        public Button exitButton;


        void Start()
        {
            InitializeSystem();
            SetupButtonListeners();
            SetupFurnacePanels();
            UpdateUIState();
            InitializeInputFields();
            AddPanelClickHandler(runButtonPanel, OnRunButtonClicked);
            AddPanelClickHandler(temperaturePanel, OnTemperaturePanelClicked);
            // Add this with your other button listeners
            exitButton.onClick.AddListener(OnExitButtonClicked);
        }
        private void OnTemperaturePanelClicked()
        {
            if (powerStates[0] && powerStates[1] && !isTemperaturePanelClicked)
            {
                isTemperaturePanelClicked = true;
                SetInputFieldsInteractable(true);
            }
        }

        private void ResetToC01()
        {
            if (currentState != SystemState.Setting) return;

            currentSegment = 1;
            currentTempType = TempType.C;
            selectedDigitIndex = 0;
            UpdateDisplay();
            StopBlinking();
            StartBlinking();
        }
        private void InitializeInputFields()
        {
            foreach (var field in cInputFields) field.interactable = false;
            foreach (var field in tInputFields) field.interactable = false;

        }

        void InitializeSystem()
        {
            tempSettings.Clear();
            for (int i = 1; i <= 6; i++)
            {
                tempSettings[$"C{i:D2}"] = 0;
                tempSettings[$"T{i:D2}"] = 0;
            }
        }

        void SetupButtonListeners()
        {
            airSwitchBtn.onClick.AddListener(() => UpdatePowerState(0));
            mainPowerBtn.onClick.AddListener(() => UpdatePowerState(1));

            confirmButton.onClick.AddListener(ConfirmSettings);
            heatButton.onClick.AddListener(OnHeatButtonClicked);

            foreach (var inputField in cInputFields)
                inputField.onEndEdit.AddListener(OnInputFieldEdited);
            foreach (var inputField in tInputFields)
                inputField.onEndEdit.AddListener(OnInputFieldEdited);
        }
        void OnHeatButtonClicked()
        {
            if (currentState == SystemState.Running)
            {
                currentState = SystemState.Paused;
                monitorLight.SetActive(false);
                if (operationRoutine != null) StopCoroutine(operationRoutine);
            }
            else if (currentState == SystemState.Paused)
            {
                currentState = SystemState.Running;
                monitorLight.SetActive(true);
                operationRoutine = StartCoroutine(TemperatureControlRoutine());
            }
        }
        private void OnInputFieldEdited(string arg0)
        {
            inputsValid = ValidateAllInputs();
            confirmButton.interactable = inputsValid;
        }

        bool ValidateAllInputs()
        {
            foreach (var inputField in cInputFields)
            {
                if (!int.TryParse(inputField.text, out int val) || val < 0 || val > 999)
                    return false;
            }

            foreach (var inputField in tInputFields)
            {
                if (!int.TryParse(inputField.text, out int val) || val < 0 || val > 999)
                    return false;
            }
            return true;
        }
        void ConfirmSettings()
        {
            if (!inputsValid) return;

            for (int i = 0; i < cInputFields.Length; i++)
                tempSettings[$"C{(i + 1):D2}"] = int.Parse(cInputFields[i].text);

            for (int i = 0; i < tInputFields.Length; i++)
                tempSettings[$"T{(i + 1):D2}"] = int.Parse(tInputFields[i].text);

            SetInputFieldsInteractable(false);
            confirmButton.interactable = false;
            currentState = SystemState.Ready;
            realTimeTempText.text = "C01";
            StartCoroutine(BlinkTargetTemp());
        }
        private bool AreAllInputsSetAndValid()
        {
            // Check if all C fields have valid values
            foreach (var field in cInputFields)
            {
                if (string.IsNullOrEmpty(field.text)) return false;
                if (!int.TryParse(field.text, out int val)) return false;
                if (val < 0 || val > 999) return false;
            }

            // Check if all T fields have valid values
            foreach (var field in tInputFields)
            {
                if (string.IsNullOrEmpty(field.text)) return false;
                if (!int.TryParse(field.text, out int val)) return false;
                if (val < 0 || val > 999) return false;
            }

            return true;
        }
        public void OnExitButtonClicked()
        {
            if (AreAllInputsSetAndValid() && currentState == SystemState.Ready)
            {
                operationPromptText.text = "请打开加热开关，再运行控温程序";
                furnacePromptPanel.SetActive(true);

            }
        }

        private IEnumerator HidePromptAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            furnacePromptPanel.SetActive(false);
        }
        IEnumerator BlinkTargetTemp()
        {
            int targetValue = tempSettings["C01"];
            while (currentState == SystemState.Ready)
            {
                targetTempText.text = "RUN";
                yield return new WaitForSeconds(0.8f);
                targetTempText.text = targetValue.ToString();
                yield return new WaitForSeconds(0.8f);
            }
        }
        void SetInputFieldsInteractable(bool state)
        {
            foreach (var inputField in cInputFields)
            {
                inputField.interactable = state;
            }
            foreach (var inputField in tInputFields)
            {
                inputField.interactable = state;
            }
        }

        void SetupFurnacePanels()
        {
            AddPanelClickHandler(furnaceOpenClosePanel, () => HandleFurnaceOperation(0));
            AddPanelClickHandler(furnaceLoadPanel, () => HandleFurnaceOperation(1));
        }

        void AddPanelClickHandler(GameObject panel, UnityEngine.Events.UnityAction callback)
        {
            EventTrigger trigger = panel.GetComponent<EventTrigger>() ?? panel.AddComponent<EventTrigger>();
            EventTrigger.Entry entry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerClick
            };
            entry.callback.AddListener((data) => callback());
            trigger.triggers.Add(entry);
        }

        void UpdatePowerState(int index)
        {
            powerStates[index] = !powerStates[index];

            if (index == 1 && powerStates[1] && !powerStates[0])
            {
                powerStates[1] = false;
                Debug.LogError("请先打开空气开关！");
                return;
            }

            UpdateUIState();

            if (powerStates[0] && powerStates[1])
            {
                currentState = SystemState.Setting;
                SetInputFieldsInteractable(true);
                confirmButton.interactable = false;
            }
            else
            {
                SetInputFieldsInteractable(false);
                confirmButton.interactable = false;
            }
        }

        void UpdateUIState()
        {
            temperaturePanel.SetActive(powerStates[0] && powerStates[1]);
            mainPowerBtn.interactable = powerStates[0];

        }


        void UpdateDisplay()
        {
            string currentKey = $"{currentTempType}{currentSegment:D2}";
            targetTempText.text = tempSettings[currentKey].ToString("D3");
            realTimeTempText.text = $"{currentTempType}{currentSegment:D2}";

            int value = tempSettings[currentKey];
            digits[0].text = (value / 100).ToString();
            digits[1].text = ((value % 100) / 10).ToString();
            digits[2].text = (value % 10).ToString();
        }

        void AdjustDigit(int direction)
        {
            if (currentState != SystemState.Setting) return;

            string key = $"{currentTempType}{currentSegment:D2}";
            int value = tempSettings[key];

            int[] parts = { value / 100, (value % 100) / 10, value % 10 };
            parts[selectedDigitIndex] = Mathf.Clamp(parts[selectedDigitIndex] + direction, 0, 9);

            tempSettings[key] = parts[0] * 100 + parts[1] * 10 + parts[2];
            UpdateDisplay();
        }


        void StartBlinking()
        {
            if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
            blinkCoroutine = StartCoroutine(BlinkDigit());
        }

        void StopBlinking()
        {
            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
                foreach (var digit in digits)
                {
                    digit.color = Color.white;
                }
            }
        }

        IEnumerator BlinkDigit()
        {
            while (true)
            {
                digits[selectedDigitIndex].color = new Color(1, 1, 1, 0.5f);
                yield return new WaitForSeconds(digitBlinkInterval);
                digits[selectedDigitIndex].color = Color.white;
                yield return new WaitForSeconds(digitBlinkInterval);
            }
        }

        void SwitchMode()
        {
            if (currentState != SystemState.Setting) return;

            if (currentTempType == TempType.C)
            {
                currentTempType = TempType.T;
                realTimeTempText.text = $"T{currentSegment:D2}";
            }
            else
            {
                currentTempType = TempType.C;
                currentSegment++;

                if (currentSegment > 6)
                {
                    currentState = SystemState.Ready;
                    targetTempText.text = "STOP";
                    realTimeTempText.text = "READY";
                    StopBlinking();
                    return;
                }

                realTimeTempText.text = $"C{currentSegment:D2}";
            }
            UpdateDisplay();
        }

        void OnRunButtonClicked()
        {

            if (currentState == SystemState.Ready)
            {
                currentState = SystemState.Running;
                operationPromptText.text = "控温系统运行中";
                currentSegment = 1; // 重置为起始段

                if (tempRoutine != null) StopCoroutine(tempRoutine);
                tempRoutine = StartCoroutine(TemperatureControlRoutine());

                if (displayRoutine != null) StopCoroutine(displayRoutine);
                displayRoutine = StartCoroutine(UpdateTargetTempDisplay());
            }

        }

        IEnumerator UpdateTargetTempDisplay()
        {
            while (currentState == SystemState.Running)
            {
                // 双重校验段号有效性
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

                targetTempText.text = "RUN";
                yield return new WaitForSeconds(1f);

                targetTempText.text = targetTemp.ToString("D3");
                yield return new WaitForSeconds(1f);
            }
        }

        IEnumerator TemperatureControlRoutine()
        {
            monitorLight.SetActive(true);
            while (currentSegment <= 6 && currentState == SystemState.Running)
            {
                string currentKey = $"C{currentSegment:D2}";
                int targetTemp = tempSettings[currentKey];
                float currentTemp = currentSegment == 1 ? 0 : tempSettings[$"C{currentSegment - 1:D2}"];

                // 显示升温阶段提示
                operationPromptText.text = "升温阶段进行中";
                furnacePromptPanel.SetActive(true);

                // 升温阶段
                while (currentTemp < targetTemp)
                {
                    currentTemp += Time.deltaTime * tempIncreaseSpeed;
                    realTimeTempText.text = Mathf.FloorToInt(currentTemp).ToString();
                    yield return null;
                }

                // 关闭升温提示面板
                furnacePromptPanel.SetActive(false);

                // 前5段执行正常操作流程
                if (currentSegment < 6)
                {
                    yield return HandleSegmentOperation();
                }
                // 第6段执行降温流程
                else
                {
                    yield return HandleFinalSegment();
                }

                currentSegment++;
            }

            // 最终状态处理
            monitorLight.SetActive(false);
            currentState = SystemState.Off;
        }

        IEnumerator HandleFinalSegment()
        {
            // 从第5段温度降到第6段温度
            float currentTemp = tempSettings["C05"];
            int targetTemp = tempSettings["C06"];

            // 显示降温提示
            operationPromptText.text = "降温阶段进行中";
            furnacePromptPanel.SetActive(true);

            // 降温阶段
            while (currentTemp > targetTemp)
            {
                currentTemp -= Time.deltaTime * tempIncreaseSpeed;
                realTimeTempText.text = Mathf.FloorToInt(currentTemp).ToString();
                yield return null;
            }

            // 温度到达后显示完成提示
            operationPromptText.text = "控温程序完成，请依次关闭加热开关、总电源开关和空气开关";

            // 等待用户按顺序关闭开关
            yield return WaitForShutdownSequence();

            furnacePromptPanel.SetActive(false);
        }

        IEnumerator WaitForShutdownSequence()
        {
            bool[] stepsCompleted = new bool[3]; // 0:加热开关, 1:总电源, 2:空气开关

            while (!stepsCompleted[0] || !stepsCompleted[1] || !stepsCompleted[2])
            {
                // 检查按钮状态变化
                if (!stepsCompleted[0] && !heatButton.interactable) // 假设关闭后按钮不可交互
                {
                    stepsCompleted[0] = true;
                    operationPromptText.text = "请关闭总电源开关和空气开关";
                }
                else if (stepsCompleted[0] && !stepsCompleted[1] && !mainPowerBtn.interactable)
                {
                    stepsCompleted[1] = true;
                    operationPromptText.text = "请关闭空气开关";
                }
                else if (stepsCompleted[0] && stepsCompleted[1] && !stepsCompleted[2] && !airSwitchBtn.interactable)
                {
                    stepsCompleted[2] = true;
                }

                yield return null;
            }
        }
        IEnumerator HandleSegmentOperation()
        {
            // 放入操作
            currentPhase = OperationPhase.PutIn;
            yield return ExecuteFurnaceOperation();

            // 保温等待
            operationPromptText.text = "保温阶段反应进行中";
            furnacePromptPanel.SetActive(true);
            yield return new WaitForSeconds(holdingTime);
            furnacePromptPanel.SetActive(false);

            // 拿出操作
            currentPhase = OperationPhase.TakeOut;
            yield return ExecuteFurnaceOperation();

            // 等待继续运行
            operationPromptText.text = "请点击继续运行控温程序";
            furnacePromptPanel.SetActive(true);
            yield return WaitForRunButtonClick();
            furnacePromptPanel.SetActive(false);
        }
        IEnumerator ExecuteFurnaceOperation()
        {
            furnaceOperationCompleted = false;
            furnaceOperationStep = 0;
            furnacePromptPanel.SetActive(true);

            float timeoutTimer = 0f;
            while (!furnaceOperationCompleted && timeoutTimer < operationTimeout)
            {
                UpdateOperationPrompt();
                timeoutTimer += Time.deltaTime;
                yield return null;
            }

            furnacePromptPanel.SetActive(false);
        }

        IEnumerator WaitForRunButtonClick()
        {
            bool clicked = false;
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerClick;
            entry.callback.AddListener((data) => { clicked = true; });

            EventTrigger trigger = runButtonPanel.GetComponent<EventTrigger>();
            trigger.triggers.Add(entry);

            while (!clicked) yield return null;

            trigger.triggers.Remove(entry);
        }

   

        IEnumerator FlashPanel(GameObject panel, Color color)
        {
            Image img = panel.GetComponent<Image>();
            Color original = img.color;
            img.color = color;
            yield return new WaitForSeconds(0.5f);
            img.color = original;
        }



        void UpdateOperationPrompt()
        {
            string[] prompts;
            if (currentSegment == 1)
            {
                // 第一段特殊处理：两个坩埚
                if (currentPhase == OperationPhase.PutIn)
                {
                    prompts = new string[] { "打开箱门", "放入1号坩埚", "放入2号坩埚", "关闭箱门" };
                }
                else
                {
                    prompts = new string[] { "打开箱门", "拿出2号坩埚", "拿出1号坩埚", "关闭箱门" };
                }
            }
            else
            {
                // 其他段：单个坩埚（3-6号）
                int crucibleNumber = currentSegment + 1; // 3-6号坩埚
                if (currentPhase == OperationPhase.PutIn)
                {
                    prompts = new string[] { "打开箱门", $"放入{crucibleNumber}号坩埚", "关闭箱门" };
                }
                else
                {
                    prompts = new string[] { "打开箱门", $"拿出{crucibleNumber}号坩埚", "关闭箱门" };
                }
            }

            // 显示当前步骤提示
            if (furnaceOperationStep < prompts.Length)
            {
                operationPromptText.text = prompts[furnaceOperationStep];
            }
            else
            {
                operationPromptText.text = "操作完成";
            }
        }

        void HandleFurnaceOperation(int operationType)
        {
            if (!furnacePromptPanel.activeSelf) return;

            // 0 = 箱门操作, 1 = 坩埚操作
            if (operationType == 0)
            {
                // 箱门操作：第一步或最后一步
                if (furnaceOperationStep == 0 || furnaceOperationStep == GetMaxSteps() - 1)
                {
                    StartCoroutine(FlashPanel(furnaceOpenClosePanel, Color.green));
                    furnaceOperationStep++;

                    // 检查是否完成所有步骤
                    if (furnaceOperationStep >= GetMaxSteps())
                    {
                        furnaceOperationCompleted = true;
                    }
                }
            }
            else if (operationType == 1)
            {
                // 坩埚操作：中间步骤
                if (furnaceOperationStep > 0 && furnaceOperationStep < GetMaxSteps() - 1)
                {
                    StartCoroutine(FlashPanel(furnaceLoadPanel, Color.blue));
                    furnaceOperationStep++;
                }
            }
        }

        // 获取当前阶段最大步骤数
        private int GetMaxSteps()
        {
            return currentSegment == 1 ? 4 : 3; // 第一段4步，其他段3步
        }
    }
}
