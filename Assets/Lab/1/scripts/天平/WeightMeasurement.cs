using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

namespace game_1
{
    public class WeightMeasurement : MonoBehaviour
    {
        // 新增：主Panel引用（替代原来的CanvasGroup）
        public GameObject mainPanel; // 这是你要隐藏/显示的主Panel

        // UI Elements
        public GameObject spoon1;
        public GameObject spoon2;
        public GameObject spoon3;
        public InputField weightInputField1;
        public InputField weightInputField2;
        public TextMeshPro balanceDisplay;
        public TextMeshProUGUI[] dataTableDisplays1; // KMnO4数据表
        public TextMeshProUGUI[] dataTableDisplays2; // MnO2数据表
        public TextMeshProUGUI[] dataTableDisplays3; // 计算结果表
        public TextMeshProUGUI[] dataTableDisplays4; // 焙烧产物数据表
        public Button confirmButton;
        public Button calculateButton;
        public TextMeshProUGUI warningText;
        public GameObject warningPanel;
        public GameObject KMnO4Panel;
        public GameObject MnO2Panel;
        public GameObject RoastedPanel;
        public GameObject cyclePanel;
        public GameObject dataPanel;
        public GameObject dataPanel1; // KMnO4和MnO2数据面板
        public GameObject dataPanel2; // 焙烧产物数据面板
        public Button lookBtn;
        public GameObject repeatPanel;

        private enum MeasurementMode { KMnO4, MnO2, Roasted }
        private MeasurementMode currentMode = MeasurementMode.KMnO4;
        private bool isWaitingForRepeatPanel = false;
        private int dataEntriesCount = 0;

        private void Start()
        {
            // 验证UI元素
            if (dataTableDisplays1.Length != 6 || dataTableDisplays2.Length != 6 ||
                dataTableDisplays3.Length != 6 || dataTableDisplays4.Length != 6)
            {
                Debug.LogError("数据表格需要正好6个TMP字段!");
            }

            // 按钮监听
            confirmButton.onClick.AddListener(OnConfirmButtonClick);
            lookBtn.onClick.AddListener(OnLookButtonClick);
            calculateButton.onClick.AddListener(OnCalculateButtonClick);

            // 新增：为三个面板添加点击事件
            AddPanelClickListeners();


            // 初始UI状态
            warningPanel.SetActive(false);
            cyclePanel.SetActive(false);
            dataPanel.SetActive(false);
            calculateButton.gameObject.SetActive(false);
            UpdatePanelVisibility();
        }
        // 新增：为三个面板添加点击事件监听
        private void AddPanelClickListeners()
        {
            // 为KMnO4Panel添加点击事件
            if (KMnO4Panel != null)
            {
                Button kmno4Button = KMnO4Panel.GetComponent<Button>();
                if (kmno4Button == null)
                {
                    kmno4Button = KMnO4Panel.AddComponent<Button>();
                    kmno4Button.transition = Selectable.Transition.None;
                }
                kmno4Button.onClick.AddListener(() => ShowMainPanel());
            }

            // 为MnO2Panel添加点击事件
            if (MnO2Panel != null)
            {
                Button mno2Button = MnO2Panel.GetComponent<Button>();
                if (mno2Button == null)
                {
                    mno2Button = MnO2Panel.AddComponent<Button>();
                    mno2Button.transition = Selectable.Transition.None;
                }
                mno2Button.onClick.AddListener(() => ShowMainPanel());
            }

            // 为RoastedPanel添加点击事件
            if (RoastedPanel != null)
            {
                Button roastedButton = RoastedPanel.GetComponent<Button>();
                if (roastedButton == null)
                {
                    roastedButton = RoastedPanel.AddComponent<Button>();
                    roastedButton.transition = Selectable.Transition.None;
                }
                roastedButton.onClick.AddListener(() => ShowMainPanel());
            }
        }
        private void Update()
        {
            repeatPanel.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (isWaitingForRepeatPanel)
                {
                    StartCoroutine(ShowCyclePanel());
                    isWaitingForRepeatPanel = false;
                }
            });
        }
        // 新增：隐藏Panel的方法
        private void HideMainPanel()
        {
            if (mainPanel != null)
            {
                mainPanel.SetActive(false);
            }
        }

        // 新增：显示Panel的方法
        public void ShowMainPanel()
        {
            if (mainPanel != null)
            {
                mainPanel.SetActive(true);
            }
        }

        // 更新面板可见性
        private void UpdatePanelVisibility()
        {
            dataPanel1.SetActive(currentMode != MeasurementMode.Roasted);
            dataPanel2.SetActive(currentMode == MeasurementMode.Roasted);
        }

        private void OnConfirmButtonClick()
        {
            // 启动延迟隐藏Panel的协程
            StartCoroutine(DelayedHidePanel());
        }

        // 新增：延迟隐藏Panel的协程
        private IEnumerator DelayedHidePanel()
        {
            string input = weightInputField1.text;

            if (!float.TryParse(input, out float weight))
            {
                ShowWarning("请输入有效数字!");
                yield break; // 直接退出协程，不执行后面的代码
            }

            if (!IsFourDecimalPlaces(input))
            {
                ShowWarning("天平最多显示4位小数\n请重新输入！");
                yield break;
            }

            switch (currentMode)
            {
                case MeasurementMode.KMnO4:
                    if (weight < 5.0f || weight > 5.1f)
                    {
                        ShowWarning("高硫锰矿称重范围应为5.0000~5.1000g\n请重新输入!");
                        yield break;
                    }
                    spoon1.GetComponent<SpoonController>().dong();
                    yield return new WaitForSeconds(1f);
                    balanceDisplay.text = weight.ToString("F4");
                    dataTableDisplays1[0].text = weight.ToString("F4");
                    break;

                case MeasurementMode.MnO2:
                    if (weight < 3.0f || weight > 3.1f)
                    {
                        ShowWarning("氧化锰矿称重范围应为3.0000~3.1000g\n请重新输入!");
                        yield break;
                    }
                    spoon2.GetComponent<SpoonController>().dong();
                    yield return new WaitForSeconds(1f);
                    balanceDisplay.text = weight.ToString("F4");
                    dataTableDisplays2[0].text = "0.0000";
                    dataTableDisplays2[1].text = weight.ToString("F4");
                    break;

                case MeasurementMode.Roasted:
                    if (weight < 3.0f || weight > 3.1f)
                    {
                        ShowWarning("坩埚焙烧物称重范围应为3.0000~3.1000g\n请重新输入!");
                        yield break;
                    }
                    spoon3.GetComponent<SpoonController>().dong();
                    yield return new WaitForSeconds(1f);
                    balanceDisplay.text = weight.ToString("F4");
                    dataTableDisplays4[0].text = weight.ToString("F4");
                    break;
            }

            // 延迟2秒
            yield return new WaitForSeconds(2f);

            // 隐藏Panel
            HideMainPanel();
            isWaitingForRepeatPanel = true;
        }

        private bool IsFourDecimalPlaces(string input)
        {
            if (!input.Contains(".")) return false;
            string[] parts = input.Split('.');
            return parts.Length == 2 && parts[1].Length == 4;
        }

        private void ShowWarning(string message)
        {
            warningText.text = message;
            warningPanel.SetActive(true);
        }

        public void OnWarningOK()
        {
            warningPanel.SetActive(false);
        }

        private IEnumerator ShowCyclePanel()
        {
            yield return new WaitForSeconds(2f);
            cyclePanel.SetActive(true);

            switch (currentMode)
            {
                case MeasurementMode.KMnO4:
                    cyclePanel.GetComponentInChildren<TextMeshProUGUI>().text =
                        "上述步骤2~3重复5次\n得到5次测量高硫锰矿的结果";
                    break;
                case MeasurementMode.MnO2:
                    cyclePanel.GetComponentInChildren<TextMeshProUGUI>().text =
                        "上述步骤6~7重复4次\n得到4次测量氧化锰矿的结果";
                    break;
                case MeasurementMode.Roasted:
                    cyclePanel.GetComponentInChildren<TextMeshProUGUI>().text =
                        "上述步骤27~28重复4次\n得到4次测量坩埚焙烧物的结果";
                    break;
            }
        }

        private void OnLookButtonClick()
        {
            cyclePanel.SetActive(false);
            dataPanel.SetActive(true);
            UpdatePanelVisibility();

            switch (currentMode)
            {
                case MeasurementMode.KMnO4:
                    for (int i = 1; i < 6; i++)
                    {
                        dataTableDisplays1[i].text = (5.0f + Random.Range(0f, 0.1f)).ToString("F4");
                    }
                    dataEntriesCount++;
                    break;
                case MeasurementMode.MnO2:
                    for (int i = 2; i < 6; i++)
                    {
                        dataTableDisplays2[i].text = (3.0f + Random.Range(0f, 0.1f)).ToString("F4");
                    }
                    dataEntriesCount++;
                    break;
                case MeasurementMode.Roasted:
                    for (int i = 1; i < 6; i++)
                    {
                        dataTableDisplays4[i].text = (3.0f + Random.Range(0f, 0.1f)).ToString("F4");
                    }
                    dataEntriesCount++;
                    break;
            }

            CheckAllDataFilled();
        }

        private void CheckAllDataFilled()
        {
            bool kmno4Filled = !string.IsNullOrEmpty(dataTableDisplays1[5].text);
            bool mno2Filled = !string.IsNullOrEmpty(dataTableDisplays2[5].text);


            if (kmno4Filled && mno2Filled)
            {
                calculateButton.gameObject.SetActive(true);
            }
        }

        private void OnCalculateButtonClick()
        {
            // 只计算KMnO4和MnO2的数据，不包括Roasted数据
            for (int i = 0; i < 6; i++)
            {
                float sum = float.Parse(dataTableDisplays1[i].text) +
                           float.Parse(dataTableDisplays2[i].text);
                dataTableDisplays3[i].text = sum.ToString("F4");
            }
            calculateButton.gameObject.SetActive(false);
        }

        public void SetKMnO4Mode()
        {
            currentMode = MeasurementMode.KMnO4;
            weightInputField1.text = "";
            weightInputField2.text = "";
            UpdatePanelVisibility();
        }

        public void SetMnO2Mode()
        {
            currentMode = MeasurementMode.MnO2;
            weightInputField1.text = "";
            weightInputField2.text = "";
            UpdatePanelVisibility();
        }

        public void SetRoastedMode()
        {
            currentMode = MeasurementMode.Roasted;
            weightInputField1.text = "";
            weightInputField2.text = "";
            UpdatePanelVisibility();
        }
    }}
