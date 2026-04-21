// ============================================================
// 文件名：DeepSeekChatPanelUI.cs
// 功  能：DeepSeek API 化学实验助手对话（主界面首页可动态生成）
// ============================================================

using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ChemLab.Utils;

namespace ChemLab.UI
{
    /// <summary>
    /// 挂在预制体 TitleBar 上：
    /// - launcher 按钮打开/关闭窗口
    /// - close 按钮关闭窗口
    /// - TitleBar 可拖拽移动窗口
    /// - 发送按钮请求 DeepSeek
    /// </summary>
    public class DeepSeekChatPanelUI : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        [Header("UI 组件")]
        public InputField inputField;
        public Text outputText;
        public Button sendButton;

        [Header("悬浮窗组件")]
        public Button launcherButton;
        public Button closeButton;
        public RectTransform windowRect;
        public Canvas rootCanvas;

        public ContentSizeFitter contentSizeFitter;
        [Tooltip("需要强制重建布局的根节点（一般填 ContentSizeFitter 所在物体，或其父节点）")]
        public RectTransform layoutRoot;

        [Header("DeepSeek 设置")]
        [TextArea(2, 4)]
        [Tooltip("留空则不在运行时请求网络，仅在 Inspector 中填写")]
        public string apiKey = "";

        public string model = "deepseek-chat";

        [TextArea(1, 3)]
        public string systemPrompt = "你是一个化学实验室助手，请用简体中文回答。";

        private const string ApiUrl = "https://api.deepseek.com/v1/chat/completions";
        private bool _requesting;
        private bool _dragging;
        private Vector2 _dragStartPointer;
        private Vector2 _dragStartAnchoredPos;

        /// <summary>强制刷新 ContentSizeFitter / LayoutGroup（可供外部调用）。</summary>
        public void RefreshLayoutNow()
        {
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        private void Awake()
        {
            if (sendButton != null)
                sendButton.onClick.AddListener(OnSendClicked);

            if (launcherButton != null)
                launcherButton.onClick.AddListener(ToggleWindow);

            if (closeButton != null)
                closeButton.onClick.AddListener(() => SetWindowVisible(false));
        }

        private void Start()
        {
            if (sendButton != null)
                sendButton.onClick.RemoveListener(OnSendClicked);
            if (sendButton != null)
                sendButton.onClick.AddListener(OnSendClicked);

            // 默认隐藏窗口（让 launcher 来打开）
            if (windowRect != null)
                windowRect.gameObject.SetActive(false);
        }

        private void OnSendClicked()
        {
            if (_requesting) return;
            if (inputField == null || outputText == null) return;

            if (string.IsNullOrEmpty(apiKey))
            {
                outputText.text = "请先在 DeepSeekChatPanelUI 组件中填写 API Key。";
                return;
            }

            string userInput = inputField.text.Trim();
            if (string.IsNullOrEmpty(userInput)) return;

            sendButton.interactable = false;
            _requesting = true;
            StartCoroutine(SendToDeepSeek(userInput));
            inputField.text = "";
        }

        private IEnumerator SendToDeepSeek(string prompt)
        {
            outputText.text = "AI 正在思考中，请稍候...";
            RefreshLayoutNow();

            string jsonBody = BuildChatRequestJson(model, systemPrompt, prompt);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

            using (var request = new UnityWebRequest(ApiUrl, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", "Bearer " + apiKey);

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                    outputText.text = "请求失败：" + request.error + "\n" + request.downloadHandler.text;
                else
                    outputText.text = ExtractAnswerFromJson(request.downloadHandler.text);
                // 等一帧让 UI 完成一次布局/渲染流程后再刷新
                yield return null;
                RefreshLayoutNow();
            }

            _requesting = false;
            if (sendButton != null) sendButton.interactable = true;
        }

        private static string BuildChatRequestJson(string modelName, string systemContent, string userContent)
        {
            return "{\"model\":\"" + EscapeJson(modelName) + "\",\"messages\":[" +
                   "{\"role\":\"system\",\"content\":\"" + EscapeJson(systemContent) + "\"}," +
                   "{\"role\":\"user\",\"content\":\"" + EscapeJson(userContent) + "\"}]}";
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "");
        }

        private static string ExtractAnswerFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return "AI 回复为空。";
            int contentIndex = json.IndexOf("\"content\":\"");
            if (contentIndex == -1) return "AI 回复解析失败。";

            contentIndex += 11;
            int endIndex = json.IndexOf("\"", contentIndex);
            if (endIndex == -1) return "AI 回复解析失败。";

            string rawContent = json.Substring(contentIndex, endIndex - contentIndex);
            return rawContent.Replace("\\n", "\n").Replace("\\\"", "\"");
        }

        /// <summary>清空输入与输出（可绑定到按钮）。</summary>
        public void ClearConversation()
        {
            if (inputField != null) inputField.text = "";
            if (outputText != null) outputText.text = "";
        }

        public void SetApiKey(string key)
        {
            apiKey = (key ?? "").Trim();
        }

        private void ToggleWindow()
        {
            if (windowRect == null) return;
            SetWindowVisible(!windowRect.gameObject.activeSelf);
        }

        private void SetWindowVisible(bool visible)
        {
            if (windowRect == null) return;
            windowRect.gameObject.SetActive(visible);
            if (visible) windowRect.transform.SetAsLastSibling();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (windowRect == null) return;
            if (rootCanvas == null) rootCanvas = GetComponentInParent<Canvas>(true);
            if (rootCanvas == null) return;

            _dragging = RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)rootCanvas.transform, eventData.position, eventData.pressEventCamera, out _dragStartPointer);
            _dragStartAnchoredPos = windowRect.anchoredPosition;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging) return;
            if (windowRect == null) return;
            if (rootCanvas == null) rootCanvas = GetComponentInParent<Canvas>(true);
            if (rootCanvas == null) return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    (RectTransform)rootCanvas.transform, eventData.position, eventData.pressEventCamera, out var p))
                return;

            Vector2 delta = p - _dragStartPointer;
            windowRect.anchoredPosition = _dragStartAnchoredPos + delta;
        }
    }
}
