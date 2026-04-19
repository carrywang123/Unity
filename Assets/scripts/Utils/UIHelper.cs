// ============================================================
// 文件名：UIHelper.cs
// 功  能：UI工具类（动态创建UI元素的辅助方法）
// 作  者：化工虚拟仿真实验平台
// ============================================================

using UnityEngine;
using UnityEngine.UI;

namespace ChemLab.Utils
{
    public static class UIHelper
    {
        // ─────────────────────────────────────────────────────
        #region 创建基础UI元素
        // ─────────────────────────────────────────────────────

        /// <summary>创建一个带背景色的Panel</summary>
        public static GameObject CreatePanel(Transform parent, string name,
                                              Color bgColor, Vector2 size)
        {
            var obj  = new GameObject(name);
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = size;

            var img  = obj.AddComponent<Image>();
            img.color = bgColor;

            return obj;
        }

        /// <summary>创建文本</summary>
        public static Text CreateText(Transform parent, string name, string content,
                                       int fontSize = 14, Color? color = null,
                                       TextAnchor anchor = TextAnchor.MiddleLeft,
                                       FontStyle style = FontStyle.Normal)
        {
            var obj  = new GameObject(name);
            obj.transform.SetParent(parent, false);

            var text = obj.AddComponent<Text>();
            text.text      = content;
            text.fontSize  = fontSize;
            text.color     = color ?? Color.black;
            text.alignment = anchor;
            text.fontStyle = style;
            text.font      = UIFont.Get();

            return text;
        }

        /// <summary>创建按钮</summary>
        public static Button CreateButton(Transform parent, string name, string label,
                                           Color bgColor, Color textColor,
                                           int fontSize = 14,
                                           Vector2? size = null)
        {
            var obj  = new GameObject(name);
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = size ?? new Vector2(120, 40);

            var img  = obj.AddComponent<Image>();
            img.color = bgColor;

            var btn  = obj.AddComponent<Button>();
            btn.targetGraphic = img;

            // 按钮颜色过渡
            var colors = btn.colors;
            colors.highlightedColor = new Color(
                bgColor.r * 1.1f, bgColor.g * 1.1f, bgColor.b * 1.1f);
            colors.pressedColor = new Color(
                bgColor.r * 0.85f, bgColor.g * 0.85f, bgColor.b * 0.85f);
            btn.colors = colors;

            // 文本
            var textObj  = new GameObject("Text");
            textObj.transform.SetParent(obj.transform, false);
            var text = textObj.AddComponent<Text>();
            text.text      = label;
            text.fontSize  = fontSize;
            text.color     = textColor;
            text.alignment = TextAnchor.MiddleCenter;
            text.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return btn;
        }

        /// <summary>创建输入框</summary>
        public static InputField CreateInputField(Transform parent, string name,
                                                   string placeholder = "请输入...",
                                                   bool isPassword = false,
                                                   Vector2? size = null)
        {
            var obj  = new GameObject(name);
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = size ?? new Vector2(300, 40);

            var img  = obj.AddComponent<Image>();
            img.color = Color.white;

            var input = obj.AddComponent<InputField>();

            // 文本区域
            var textArea = new GameObject("Text");
            textArea.transform.SetParent(obj.transform, false);
            var textRect = textArea.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 5);
            textRect.offsetMax = new Vector2(-10, -5);

            var text = textArea.AddComponent<Text>();
            text.fontSize  = 14;
            text.color     = Color.black;
            text.alignment = TextAnchor.MiddleLeft;
            text.font      = UIFont.Get();
            input.textComponent = text;

            // 占位符
            var phObj  = new GameObject("Placeholder");
            phObj.transform.SetParent(obj.transform, false);
            var phRect = phObj.AddComponent<RectTransform>();
            phRect.anchorMin = Vector2.zero;
            phRect.anchorMax = Vector2.one;
            phRect.offsetMin = new Vector2(10, 5);
            phRect.offsetMax = new Vector2(-10, -5);

            var phText = phObj.AddComponent<Text>();
            phText.text      = placeholder;
            phText.fontSize  = 14;
            phText.color     = new Color(0.6f, 0.6f, 0.6f);
            phText.alignment = TextAnchor.MiddleLeft;
            phText.fontStyle = FontStyle.Italic;
            phText.font      = UIFont.Get();
            input.placeholder = phText;

            if (isPassword)
                input.contentType = InputField.ContentType.Password;

            return input;
        }

        /// <summary>创建分割线</summary>
        public static GameObject CreateDivider(Transform parent, Color? color = null,
                                                float height = 1f)
        {
            var obj  = new GameObject("Divider");
            obj.transform.SetParent(parent, false);

            var rect = obj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, height);

            var img  = obj.AddComponent<Image>();
            img.color = color ?? new Color(0.8f, 0.8f, 0.8f);

            var layout = obj.AddComponent<LayoutElement>();
            layout.preferredHeight = height;
            layout.flexibleWidth   = 1f;

            return obj;
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region RectTransform 工具
        // ─────────────────────────────────────────────────────

        /// <summary>设置RectTransform全屏拉伸</summary>
        public static void SetFullStretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>设置RectTransform居中</summary>
        public static void SetCenter(RectTransform rect, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot     = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region 颜色工具
        // ─────────────────────────────────────────────────────

        /// <summary>从十六进制字符串解析颜色（如 "#2196F3"）</summary>
        public static Color HexToColor(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out Color color))
                return color;
            return Color.white;
        }

        /// <summary>调整颜色亮度</summary>
        public static Color AdjustBrightness(Color color, float factor)
        {
            return new Color(
                Mathf.Clamp01(color.r * factor),
                Mathf.Clamp01(color.g * factor),
                Mathf.Clamp01(color.b * factor),
                color.a
            );
        }

        #endregion
    }
}
