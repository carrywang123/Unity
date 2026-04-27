using UnityEngine;
using UnityEngine.UI;

namespace game_1
{
    [System.Serializable]
    public class ExperimentStep
    {
        public string instruction; // 步骤说明文本
        public AudioClip audioClip; // 步骤对应的音频
        public Button[] requiredButtons; // 此步骤需要按顺序点击的按钮
        [HideInInspector] public int currentButtonIndex = 0; // 当前已点击的按钮索引
        public string hintText;
    }}
