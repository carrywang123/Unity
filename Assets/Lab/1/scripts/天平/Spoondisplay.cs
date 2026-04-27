using UnityEngine;
using UnityEngine.UI;

namespace game_1
{
    public class Spoondisplay : MonoBehaviour
    {
        public InputField inputField1; // 第一个输入框 (可输入)
        public InputField inputField2; // 第二个输入框 (只读)
        public Button confirmButton;   // 确认按钮
        public Button exitButton;      // 新增的退出按钮

        void Start()
        {
            // 设置输入框2为只读
            inputField2.interactable = false;

            // 为输入框1添加输入验证
            inputField1.onValueChanged.AddListener(ValidateInput);

            // 为按钮添加点击事件监听
            confirmButton.onClick.AddListener(TransferNumber);

            // 为退出按钮添加点击事件监听
            exitButton.onClick.AddListener(ResetFields);
        }

        void ValidateInput(string input)
        {
            // 如果输入为空，不做处理
            if (string.IsNullOrEmpty(input))
                return;

            // 检查输入是否符合格式要求
            if (!IsValidNumber(input))
            {
                // 删除最后一个无效字符
                inputField1.text = input.Substring(0, input.Length - 1);
            }
        }

        bool IsValidNumber(string input)
        {
            // 允许为空
            if (string.IsNullOrEmpty(input))
                return true;

            // 检查是否包含多个小数点
            if (input.Split('.').Length > 2)
                return false;

            // 检查字符是否都是数字或小数点
            foreach (char c in input)
            {
                if (!char.IsDigit(c) && c != '.')
                    return false;
            }

            // 分割整数和小数部分
            string[] parts = input.Split('.');
            string integerPart = parts[0];
            string decimalPart = parts.Length > 1 ? parts[1] : "";

            // 检查整数部分位数 (最多3位)
            if (integerPart.Length > 3)
                return false;

            // 检查小数部分位数 (最多4位)
            if (decimalPart.Length > 4)
                return false;

            return true;
        }

        void TransferNumber()
        {
            string number = inputField1.text;

            // 如果输入为空，不做处理
            if (string.IsNullOrEmpty(number))
            {
                inputField2.text = "";
                return;
            }

            // 验证数字格式
            if (IsValidNumber(number))
            {
                // 格式化数字: 确保小数点后不超过4位
                if (number.Contains("."))
                {
                    string[] parts = number.Split('.');
                    string decimalPart = parts[1];
                    if (decimalPart.Length > 4)
                    {
                        decimalPart = decimalPart.Substring(0, 4);
                        number = parts[0] + "." + decimalPart;
                    }
                }

                inputField2.text = number;
            }
            else
            {
                inputField2.text = "无效数字格式";
            }
        }

        // 新增方法：重置输入框
        void ResetFields()
        {
            inputField1.text = "";
            inputField2.text = "";
        }
    }}
