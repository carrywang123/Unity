using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace game_1
{
    public class ChangeTextColor1 : MonoBehaviour
    {
        public TMP_Text textComponent;
        public TMP_Text currentstep;
        public GameObject scrollView;
        public RectTransform content;          // Scroll View 里的 Content
        public Button startButton;
        public GameObject completionPanel;
        public TMP_Text panelText;

        public ExperimentStep[] experimentSteps;

        private AudioSource audioSource;
        private ScrollRect scrollRect;
        private string originalText;
        private string colorTagStart = "<color=red>";
        private string colorTagEnd = "</color>";
        private int currentStepIndex = 0;
        private bool isExperimentStarted = false;

        public Button skipStepButton;      // 跳过当前步骤按钮
        public Button hintButton;          // 提示按钮
        public int hintPenalty = 1;        // 每次提示扣除的分数

        // 评分相关
        private int totalScore = 0;
        public TMP_Text scoreText;         // 当前得分UI
        public TMP_Text finalScoreText;    // 实验结束后显示得分
        public int stepScore = 10;         // 每完成一步加几分

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
            hintButton.onClick.AddListener(ShowHint);

            UpdateScoreUI();
        }

        private void ShowHint()
        {
            if (!isExperimentStarted || currentStepIndex >= experimentSteps.Length)
                return;

            Debug.Log("使用提示，扣分");

            // 扣分逻辑
            SubtractScore(hintPenalty);

            // 显示提示内容（需要在 ExperimentStep 中有 hintText 字段）
            string hint = experimentSteps[currentStepIndex].hintText;
            currentstep.text = $"提示：{hint}";
        }

        private void SubtractScore(int points)
        {
            totalScore -= points;
            if (totalScore < 0) totalScore = 0;
            UpdateScoreUI();
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

        private void SkipCurrentStep()
        {
            if (!isExperimentStarted || currentStepIndex >= experimentSteps.Length)
                return;

            Debug.Log($"跳过第 {currentStepIndex + 1} 步");

            // 跳过不加分
            ProceedToNextStep();
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
                // 步骤完成加分
                AddScore(stepScore);
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

            // 延迟一帧再滚动，避免 TMP 尚未更新完
            StartCoroutine(ScrollNextFrame(step.instruction, currentStepIndex));
        }

        private IEnumerator ScrollNextFrame(string highlightedText, int stepIndex)
        {
            yield return null;
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
                StartCoroutine(ShowPanelAfterAudio(0f));
                return;
            }
            StartCurrentStep();
        }

        // 加分
        private void AddScore(int points)
        {
            totalScore += points;
            UpdateScoreUI();
        }

        // 更新分数UI
        private void UpdateScoreUI()
        {
            if (scoreText != null)
                scoreText.text = $"当前得分：{totalScore}";
        }

        // 显示总分
        private IEnumerator ShowPanelAfterAudio(float delay)
        {
            yield return new WaitForSeconds(delay);
            completionPanel.SetActive(true);
            if (finalScoreText != null)
                finalScoreText.text = $"{totalScore}";

            yield return new WaitForSeconds(3f);
            completionPanel.SetActive(false);
        }

        private void TextColorChange(string highlightedText, int stepIndex)
        {
            string normalizedText = $" {stepIndex + 1}.{highlightedText}";
            textComponent.text = originalText.Replace(normalizedText,
                colorTagStart + normalizedText + colorTagEnd);
        }

        /// <summary>
        /// 使用“当前步骤 / 总步骤数”估算滚动位置，不依赖 textInfo/characterInfo
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

        public void HideCompletionPanel()
        {
            if (completionPanel != null)
                completionPanel.SetActive(false);
        }
    }
}
