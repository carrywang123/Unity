using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace game_1
{
    public class ChangeTextColor : MonoBehaviour
    {
        public TMP_Text textComponent;             // 所有步骤的显示文本
        public TMP_Text currentstep;               // 当前步骤文本
        public GameObject scrollView;              // 滚动容器（带 ScrollRect 的对象）
        public RectTransform content;              // Scroll View 里的 Content（在 Inspector 里手动拖）
        public Button startButton;                 // 开始按钮
        public Button skipStepButton;              // 跳过当前步骤按钮
        public GameObject completionPanel;         // 实验完成提示面板
        public TMP_Text panelText;                 // 面板中的文字

        public Button weighingButton;              // 跳过称量阶段
        public Button roastingButton;              // 跳过焙烧阶段
        public Button leachingButton;              // 跳过浸出阶段
        public Button filtrationButton;            // 跳过过滤阶段
        public Button dryingButton;                // 跳过干燥阶段

        public ExperimentStep[] experimentSteps;   // 所有实验步骤

        private AudioSource audioSource;
        private ScrollRect scrollRect;
        private string originalText;
        private string colorTagStart = "<color=red>";
        private string colorTagEnd = "</color>";
        private int currentStepIndex = 0;
        private bool isExperimentStarted = false;

        void Start()
        {
            audioSource = GetComponent<AudioSource>();

            // 安全获取 ScrollRect
            if (scrollView != null)
            {
                scrollRect = scrollView.GetComponent<ScrollRect>();
                if (scrollRect == null)
                {
                    scrollRect = scrollView.GetComponentInParent<ScrollRect>();
                }
            }

            if (scrollRect == null)
            {
                Debug.LogError("未找到 ScrollRect，请把“Scroll View”字段拖到包含 ScrollRect 组件的对象（不要拖 Viewport）");
            }

            InitializeSteps();
            originalText = textComponent.text;
            currentStepIndex = 0;

            if (completionPanel != null)
                completionPanel.SetActive(false);

            startButton.onClick.AddListener(StartExperiment);
            skipStepButton.onClick.AddListener(SkipCurrentStep);

            // 跳过阶段按钮绑定
            weighingButton.onClick.AddListener(() => AutoCompleteSteps(0, 8, 9));
            roastingButton.onClick.AddListener(() => AutoCompleteSteps(9, 29, 30));
            leachingButton.onClick.AddListener(() => AutoCompleteSteps(30, 38, 39));
            filtrationButton.onClick.AddListener(() => AutoCompleteSteps(39, 43, 44));
            dryingButton.onClick.AddListener(() => AutoCompleteSteps(44, 48, 49));
        }

        private void InitializeSteps()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < experimentSteps.Length; i++)
            {
                sb.Append($" {i + 1}.{experimentSteps[i].instruction}\n");

                for (int j = 0; j < experimentSteps[i].requiredButtons.Length; j++)
                {
                    int stepIndex = i;
                    int buttonIndex = j;
                    experimentSteps[i].requiredButtons[j].onClick.AddListener(() =>
                        OnStepButtonClicked(stepIndex, buttonIndex));
                }
            }

            textComponent.text = sb.ToString();
            originalText = textComponent.text;
            textComponent.richText = true;
        }

        private void StartExperiment()
        {
            isExperimentStarted = true;
            startButton.gameObject.SetActive(false);
            StartCurrentStep();
        }

        private void OnStepButtonClicked(int stepIndex, int buttonIndex)
        {
            if (!isExperimentStarted || stepIndex != currentStepIndex) return;

            var currentStep = experimentSteps[stepIndex];
            if (buttonIndex != currentStep.currentButtonIndex) return;

            currentStep.currentButtonIndex++;

            if (currentStep.currentButtonIndex >= currentStep.requiredButtons.Length)
            {
                ProceedToNextStep();
            }
            else
            {
                currentStep.requiredButtons[currentStep.currentButtonIndex].interactable = true;
            }
        }

        private void StartCurrentStep()
        {
            if (currentStepIndex >= experimentSteps.Length) return;

            var step = experimentSteps[currentStepIndex];
            currentstep.text = $" {currentStepIndex + 1}.{step.instruction}";
            TextColorChange(step.instruction, currentStepIndex);
            StartCoroutine(PlayAudio(step.audioClip));

            if (step.requiredButtons.Length > 0)
            {
                step.requiredButtons[0].interactable = true;
            }

            // 为了兼容 TMP 的生成时机，延迟一帧再滚动
            StartCoroutine(ScrollNextFrame(step.instruction, currentStepIndex));
        }

        private IEnumerator ScrollNextFrame(string highlightedText, int stepIndex)
        {
            yield return null; // 等一帧
            ScrollToHighlightedText(highlightedText, stepIndex);
        }

        private IEnumerator PlayAudio(AudioClip clip)
        {
            if (clip != null)
            {
                audioSource.clip = clip;
                audioSource.Play();
                yield return new WaitForSeconds(clip.length);
            }
            else
            {
                yield return null;
            }
        }

        private void ProceedToNextStep()
        {
            currentStepIndex++;
            if (currentStepIndex >= experimentSteps.Length)
            {
                Debug.Log("实验完成!");
                StartCoroutine(ShowPanelAfterAudio(0f)); // 播放完最后一个后显示Panel
                return;
            }
            StartCurrentStep();
        }

        private void SkipCurrentStep()
        {
            if (!isExperimentStarted || currentStepIndex >= experimentSteps.Length) return;

            // 将当前步骤标记为已完成（否则按钮点击事件会无效）
            experimentSteps[currentStepIndex].currentButtonIndex = experimentSteps[currentStepIndex].requiredButtons.Length;

            // 让当前步骤按钮保持可交互（避免直接禁用，可能有动画或提示）
            foreach (var btn in experimentSteps[currentStepIndex].requiredButtons)
            {
                btn.interactable = true;
            }

            currentStepIndex++;

            if (currentStepIndex >= experimentSteps.Length)
            {
                Debug.Log("实验完成!");
                StartCoroutine(ShowPanelAfterAudio(0f));
                return;
            }

            StartCurrentStep();
        }

        private void AutoCompleteSteps(int startIndex, int endIndex, int nextStepIndex)
        {
            if (!isExperimentStarted) return;

            // 不禁用按钮，仅更新 currentButtonIndex 为最大值，使步骤视为已完成
            for (int i = startIndex; i <= endIndex && i < experimentSteps.Length; i++)
            {
                experimentSteps[i].currentButtonIndex = experimentSteps[i].requiredButtons.Length;

                // 确保按钮仍可交互
                foreach (var btn in experimentSteps[i].requiredButtons)
                {
                    btn.interactable = true;
                }
            }

            currentStepIndex = nextStepIndex;

            if (currentStepIndex >= experimentSteps.Length)
            {
                Debug.Log("实验完成！");
                StartCoroutine(ShowPanelAfterAudio(0f));
                return;
            }

            StartCurrentStep();
        }

        private void TextColorChange(string highlightedText, int stepIndex)
        {
            string normalizedText = $" {stepIndex + 1}.{highlightedText}";
            textComponent.text = originalText.Replace(normalizedText,
                colorTagStart + normalizedText + colorTagEnd);
        }

        /// <summary>
        /// 使用“当前步骤 / 总步骤数”估算滚动位置，不再依赖 textInfo/characterInfo
        /// </summary>
        private void ScrollToHighlightedText(string highlightedText, int stepIndex)
        {
            // 确保 ScrollRect
            if (scrollRect == null)
            {
                scrollRect = scrollView?.GetComponent<ScrollRect>()
                            ?? scrollView?.GetComponentInParent<ScrollRect>()
                            ?? GetComponentInParent<ScrollRect>();
                if (scrollRect == null)
                {
                    Debug.LogError("ScrollRect 仍然为 null，请检查 Scroll View 字段绑定");
                    return;
                }
            }

            if (content == null)
            {
                Debug.LogError("content 未赋值，请在 Inspector 里把 Scroll View 的 Content 拖到 content 字段上");
                return;
            }

            if (experimentSteps == null || experimentSteps.Length == 0)
            {
                Debug.LogWarning("experimentSteps 为空，无法计算滚动位置");
                return;
            }

            int totalSteps = experimentSteps.Length;
            stepIndex = Mathf.Clamp(stepIndex, 0, totalSteps - 1);

            // t: 0 表示第 0 步，1 表示最后一步
            float t = (totalSteps <= 1) ? 0f : (float)stepIndex / (totalSteps - 1);

            // ScrollRect.verticalNormalizedPosition: 1 顶部，0 底部
            scrollRect.verticalNormalizedPosition = 1f - t;
        }

        private IEnumerator ShowPanelAfterAudio(float delay)
        {
            yield return new WaitForSeconds(delay);
            completionPanel.SetActive(true);
            yield return new WaitForSeconds(3f);
            completionPanel.SetActive(false);
        }

        public void HideCompletionPanel()
        {
            if (completionPanel != null)
                completionPanel.SetActive(false);
        }
    }
}
