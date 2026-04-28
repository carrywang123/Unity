// ============================================================
// 文件名：UIManager.cs
// 功  能：UI面板统一管理（面板切换、消息弹窗、加载动画）
// 作  者：化工虚拟仿真实验平台
// ============================================================

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ChemLab.UI;
using ChemLab.Utils;

namespace ChemLab.Managers
{
    public class UIManager : MonoBehaviour
    {
        // ── 单例 ──────────────────────────────────────────────
        public static UIManager Instance { get; private set; }

        // ── 面板引用（在Inspector中绑定） ─────────────────────
        [Header("=== 主要面板 ===")]
        [Tooltip("登录面板")]
        public GameObject loginPanel;

        [Tooltip("注册面板")]
        public GameObject registerPanel;

        [Tooltip("管理员面板")]
        public GameObject adminPanel;

        [Tooltip("普通用户面板")]
        public GameObject userPanel;

        // ── 弹窗组件 ──────────────────────────────────────────
        [Header("=== 消息弹窗 ===")]
        [Tooltip("消息弹窗根节点")]
        public GameObject messageBox;

        [Tooltip("弹窗标题文本")]
        public Text messageTitle;

        [Tooltip("弹窗内容文本")]
        public Text messageContent;

        [Tooltip("弹窗确认按钮")]
        public Button messageConfirmBtn;

        [Tooltip("弹窗取消按钮（可选）")]
        public Button messageCancelBtn;

        // ── 加载遮罩 ──────────────────────────────────────────
        [Header("=== 加载遮罩 ===")]
        [Tooltip("加载中遮罩")]
        public GameObject loadingMask;

        [Tooltip("加载提示文本")]
        public Text loadingText;

        // ── 顶部提示条 ────────────────────────────────────────
        [Header("=== 顶部提示条 ===")]
        [Tooltip("顶部Toast提示根节点")]
        public GameObject toastPanel;

        [Tooltip("Toast文本")]
        public Text toastText;

        // ── 背景 ──────────────────────────────────────────────
        [Header("=== 背景 ===")]
        [Tooltip("主背景图")]
        public Image backgroundImage;

        // ── 私有变量 ──────────────────────────────────────────
        private Action _onConfirmCallback;
        private Action _onCancelCallback;
        private Coroutine _toastCoroutine;

        [Header("=== 全局 AI 悬浮窗 ===")]
        public bool enableGlobalAiChat = true;
        [Tooltip("DeepSeek API Key（可在 UserPanelUI 登录后覆盖）")]
        public string aiChatApiKey = "";

        private Canvas _aiChatCanvas;
        private DeepSeekChatPanelUI _aiChat;
        private GameObject _aiChatRoot;

        // ─────────────────────────────────────────────────────
        #region Unity 生命周期
        // ─────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;

            if (enableGlobalAiChat)
                EnsureGlobalAiChat();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "Main") return;
            RefreshPanelReferencesFromMainScene();
            if (DataManager.Instance != null && DataManager.Instance.CurrentUser != null)
                NavigateByRole();
        }

        /// <summary>Main 场景卸载再加载后，序列化引用会失效；按场景重新绑定 UI。</summary>
        public void RefreshPanelReferencesFromMainScene()
        {
            const string sn = "Main";
            var login = FindUIInScene<UI.LoginUI>(sn);
            if (login != null) loginPanel = login.gameObject;
            var reg = FindUIInScene<UI.RegisterUI>(sn);
            if (reg != null) registerPanel = reg.gameObject;
            var adm = FindUIInScene<UI.AdminPanelUI>(sn);
            if (adm != null) adminPanel = adm.gameObject;
            var usr = FindUIInScene<UI.UserPanelUI>(sn);
            if (usr != null) userPanel = usr.gameObject;

            var msg = FindUIInScene<UI.MessageBoxUI>(sn);
            if (msg != null)
            {
                messageBox = msg.gameObject;
                messageTitle = msg.titleText;
                messageContent = msg.contentText;
                messageConfirmBtn = msg.confirmButton;
                messageCancelBtn = msg.cancelButton;
            }

            var load = FindUIInScene<UI.LoadingUI>(sn);
            if (load != null)
            {
                loadingMask = load.gameObject;
                loadingText = load.loadingText;
            }

            var toast = FindUIInScene<UI.ToastUI>(sn);
            if (toast != null)
            {
                toastPanel = toast.gameObject;
                toastText = toast.messageText;
            }

            if (messageConfirmBtn != null)
            {
                messageConfirmBtn.onClick.RemoveAllListeners();
                messageConfirmBtn.onClick.AddListener(OnMessageConfirm);
            }
            if (messageCancelBtn != null)
            {
                messageCancelBtn.onClick.RemoveAllListeners();
                messageCancelBtn.onClick.AddListener(OnMessageCancel);
            }

            InstallCursorHoverForAllButtons();
        }

        private static T FindUIInScene<T>(string sceneName) where T : MonoBehaviour
        {
            var arr = UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in arr)
            {
                if (c != null && c.gameObject.scene.name == sceneName)
                    return c;
            }
            return null;
        }

        private void Start()
        {
            // 初始状态：只显示登录面板
            ShowLoginPanel();

            // 绑定弹窗按钮事件
            if (messageConfirmBtn != null)
                messageConfirmBtn.onClick.AddListener(OnMessageConfirm);
            if (messageCancelBtn != null)
                messageCancelBtn.onClick.AddListener(OnMessageCancel);

            // 初始隐藏弹窗和加载遮罩
            if (messageBox   != null) messageBox.SetActive(false);
            if (loadingMask  != null) loadingMask.SetActive(false);
            if (toastPanel   != null) toastPanel.SetActive(false);

            // 全局：给所有 Button 自动注入 hover 光标切换
            UICursor.Preload();
            InstallCursorHoverForAllButtons();
        }

        #endregion

        public void SetAiChatApiKey(string key)
        {
            aiChatApiKey = (key ?? "").Trim();
            if (_aiChat != null) _aiChat.SetApiKey(aiChatApiKey);
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;

            var es = new GameObject("EventSystem_Runtime");
            DontDestroyOnLoad(es);
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        private void EnsureGlobalAiChat()
        {
            EnsureEventSystem();

            if (_aiChatRoot == null)
            {
                var prefab = Resources.Load<GameObject>("Prefabs/AiChatOverlay");
                if (prefab == null)
                {
                    Debug.LogWarning("[UIManager] 未找到预制体 Resources/Prefabs/AiChatOverlay.prefab，已跳过全局 AI 悬浮窗。");
                    return;
                }

                _aiChatRoot = Instantiate(prefab);
                _aiChatRoot.name = "AiChatOverlay";
                DontDestroyOnLoad(_aiChatRoot);
            }

            if (_aiChat == null)
                _aiChat = _aiChatRoot.GetComponentInChildren<DeepSeekChatPanelUI>(true);

            if (_aiChat != null)
                _aiChat.SetApiKey(aiChatApiKey);
        }

        private void InstallCursorHoverForAllButtons()
        {
            // 包含 inactive（面板切换时常常先 SetActive(false)）
            var buttons = Resources.FindObjectsOfTypeAll<Button>();
            for (int i = 0; i < buttons.Length; i++)
            {
                var btn = buttons[i];
                if (btn == null) continue;
                if (btn.gameObject == null) continue;
                if (!btn.gameObject.scene.IsValid() || !btn.gameObject.scene.isLoaded) continue;

                // 不给不可交互的按钮强行挂（仍可手动挂 UICursorHoverTarget 覆盖）
                if (!btn.IsInteractable()) continue;

                var hover = btn.GetComponent<UICursorHoverTarget>();
                if (hover == null)
                    hover = btn.gameObject.AddComponent<UICursorHoverTarget>();

                // 个别按钮：父物体的父物体名为 Row，则不希望 hover 上移（但仍希望手型光标）
                if (IsGrandParentNamedRow(btn.transform))
                {
                    hover.hoverMoveUp = 0f;
                }
            }
        }

        private static bool IsGrandParentNamedRow(Transform t)
        {
            if (t == null) return false;
            var parent = t.parent;
            if (parent == null) return false;
            var grandParent = parent.parent;
            if (grandParent == null) return false;
            return string.Equals(grandParent.name, "Row", StringComparison.Ordinal);
        }

        // ─────────────────────────────────────────────────────
        #region 面板切换
        // ─────────────────────────────────────────────────────

        /// <summary>隐藏所有主面板</summary>
        private void HideAllPanels()
        {
            if (loginPanel    != null) loginPanel.SetActive(false);
            if (registerPanel != null) registerPanel.SetActive(false);
            if (adminPanel    != null) adminPanel.SetActive(false);
            if (userPanel     != null) userPanel.SetActive(false);
        }

        /// <summary>显示登录面板</summary>
        public void ShowLoginPanel()
        {
            HideAllPanels();
            if (loginPanel != null)
            {
                loginPanel.SetActive(true);
                // 通知登录面板刷新
                loginPanel.GetComponent<UI.LoginUI>()?.OnPanelShow();
            }
        }

        /// <summary>显示注册面板</summary>
        public void ShowRegisterPanel()
        {
            HideAllPanels();
            if (registerPanel != null)
            {
                registerPanel.SetActive(true);
                registerPanel.GetComponent<UI.RegisterUI>()?.OnPanelShow();
            }
        }

        /// <summary>显示管理员面板</summary>
        public void ShowAdminPanel()
        {
            HideAllPanels();
            if (adminPanel != null)
            {
                adminPanel.SetActive(true);
                adminPanel.GetComponent<UI.AdminPanelUI>()?.OnPanelShow();
            }
        }

        /// <summary>显示普通用户面板</summary>
        public void ShowUserPanel()
        {
            HideAllPanels();
            if (userPanel != null)
            {
                userPanel.SetActive(true);
                userPanel.GetComponent<UI.UserPanelUI>()?.OnPanelShow();
            }
        }

        /// <summary>根据当前登录用户角色跳转到对应面板</summary>
        public void NavigateByRole()
        {
            var user = DataManager.Instance.CurrentUser;
            if (user == null)
            {
                ShowLoginPanel();
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            StartCoroutine(NavigateByRoleWebGL());
            return;
#endif
            if (user.role == Models.UserRole.Admin)
                ShowAdminPanel();
            else
                ShowUserPanel();
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        private IEnumerator NavigateByRoleWebGL()
        {
            ShowLoading("正在加载数据...");
            bool ok = false;
            string err = "";
            yield return DataManager.Instance.WarmupAfterLoginAsync((succ, msg) =>
            {
                ok = succ;
                err = msg;
            });
            HideLoading();

            if (!ok)
            {
                ShowMessage("加载失败", string.IsNullOrEmpty(err) ? "无法从服务器获取数据" : err, ShowLoginPanel);
                yield break;
            }

            var user = DataManager.Instance.CurrentUser;
            if (user != null && user.role == Models.UserRole.Admin)
                ShowAdminPanel();
            else
                ShowUserPanel();
        }
#endif

        #endregion

        // ─────────────────────────────────────────────────────
        #region 消息弹窗
        // ─────────────────────────────────────────────────────

        /// <summary>
        /// 显示信息弹窗（仅确认按钮）
        /// </summary>
        public void ShowMessage(string title, string content, Action onConfirm = null)
        {
            if (messageBox == null) return;

            ResetMessageBoxVisualState();

            if (messageTitle   != null) messageTitle.text   = title;
            if (messageContent != null) messageContent.text = content;

            _onConfirmCallback = onConfirm;
            _onCancelCallback  = null;

            if (messageCancelBtn != null) messageCancelBtn.gameObject.SetActive(false);
            if (messageConfirmBtn != null)
            {
                messageConfirmBtn.gameObject.SetActive(true);
                var btnText = messageConfirmBtn.GetComponentInChildren<Text>();
                if (btnText != null) btnText.text = "确 定";
            }

            messageBox.SetActive(true);
        }

        /// <summary>
        /// 显示确认弹窗（确认 + 取消按钮）
        /// </summary>
        public void ShowConfirm(string title, string content,
                                Action onConfirm, Action onCancel = null)
        {
            if (messageBox == null) return;

            ResetMessageBoxVisualState();

            if (messageTitle   != null) messageTitle.text   = title;
            if (messageContent != null) messageContent.text = content;

            _onConfirmCallback = onConfirm;
            _onCancelCallback  = onCancel;

            if (messageCancelBtn  != null) messageCancelBtn.gameObject.SetActive(true);
            if (messageConfirmBtn != null) messageConfirmBtn.gameObject.SetActive(true);

            messageBox.SetActive(true);
        }

        private void ResetMessageBoxVisualState()
        {
            // 如果弹窗物体上挂了 CanvasGroup/动画脚本，可能在上次 FadeOut 后 alpha=0、不可交互；
            // 这里强制复位，确保再次 Show 时可见且可点击。
            var cg = messageBox.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }
        }

        private void OnMessageConfirm()
        {
            if (messageBox != null) messageBox.SetActive(false);
            _onConfirmCallback?.Invoke();
            _onConfirmCallback = null;
        }

        private void OnMessageCancel()
        {
            if (messageBox != null) messageBox.SetActive(false);
            _onCancelCallback?.Invoke();
            _onCancelCallback = null;
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region 加载遮罩
        // ─────────────────────────────────────────────────────

        public void ShowLoading(string text = "加载中...")
        {
            if (loadingMask == null) return;
            if (loadingText != null) loadingText.text = text;
            loadingMask.SetActive(true);
        }

        public void HideLoading()
        {
            if (loadingMask != null) loadingMask.SetActive(false);
        }

        #endregion

        // ─────────────────────────────────────────────────────
        #region Toast 提示
        // ─────────────────────────────────────────────────────

        /// <summary>
        /// 显示顶部Toast提示（自动消失）
        /// </summary>
        public void ShowToast(string message, float duration = 2.5f)
        {
            if (toastPanel == null) return;

            if (_toastCoroutine != null)
                StopCoroutine(_toastCoroutine);

            _toastCoroutine = StartCoroutine(ToastCoroutine(message, duration));
        }

        private IEnumerator ToastCoroutine(string message, float duration)
        {
            if (toastText != null)
            {
                // 修复：某些字体/材质引用丢失或被错误替换时，会导致屏幕显示“乱码/错位”；
                // 这里每次展示前强制回到项目统一字体，并清空自定义材质以使用字体默认材质。
                toastText.font = ChemLab.Utils.UIFont.Get();
                toastText.material = null;
                toastText.text = message;
            }
            if (toastPanel != null) toastPanel.SetActive(true);

            yield return new WaitForSeconds(duration);

            if (toastPanel != null) toastPanel.SetActive(false);
        }

        #endregion
    }
}
