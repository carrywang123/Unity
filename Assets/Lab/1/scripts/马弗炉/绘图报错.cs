using UnityEngine;
using UnityEngine.UI;

namespace game_1
{
    public class 绘图报错 : MonoBehaviour
    {
        [System.Serializable]
        public class InputFieldRange
        {
            public InputField inputField; // 输入框
            public float minValue; // 最小值
            public float maxValue; // 最大值
        }

        public InputFieldRange[] inputFields; // 四个输入框及其范围
        public Button confirmButton; // 确认按钮
        public GameObject errorPanel; // 报错提示的Panel
        public Text errorText; // 报错提示的文字

        void Start()
        {

            // 监听确认按钮点击事件
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);
            // 隐藏报错Panel
            errorPanel.SetActive(false);
        }

        private void OnConfirmButtonClicked()
        {
            if (inputFields == null || inputFields.Length == 0) return;

            bool isValid = true;
            string errorMessage = "";

            // 遍历所有输入框
            for (int i = 0; i < inputFields.Length; i++)
            {
                if (inputFields[i].inputField == null)
                {
                    Debug.LogError($"输入框 {i} 未分配！");
                    continue;
                }

                string input = inputFields[i].inputField.text;

                if (string.IsNullOrEmpty(input))
                {
                    isValid = false;
                    errorMessage = "输入框 " + (i + 1) + " 不能为空！";
                    ClearInvalidInput(i);
                    break;
                }

                if (float.TryParse(input, out float numericValue))
                {
                    if (numericValue < inputFields[i].minValue || numericValue > inputFields[i].maxValue)
                    {
                        isValid = false;
                        errorMessage = "输入框 " + (i + 1) + " 填写超出范围！请填写 " + inputFields[i].minValue + " 到 " + inputFields[i].maxValue + " 之间的数字。";
                        ClearInvalidInput(i);
                        break;
                    }
                }
                else
                {
                    isValid = false;
                    errorMessage = "输入框 " + (i + 1) + " 输入无效！请输入数字。";
                    ClearInvalidInput(i);
                    break;
                }
            }

            if (isValid)
            {
                errorPanel.SetActive(false);
                Debug.Log("所有输入有效！");
            }
            else
            {
                ShowError(errorMessage);
            }
        }

        private void ClearInvalidInput(int index)
        {
            if (inputFields == null || index < 0 || index >= inputFields.Length) return;
            if (inputFields[index].inputField != null)
            {
                inputFields[index].inputField.text = "";
            }
        }

        private void ShowError(string message)
        {
            if (errorPanel != null) errorPanel.SetActive(true);
            if (errorText != null) errorText.text = message;
        }
    }}
