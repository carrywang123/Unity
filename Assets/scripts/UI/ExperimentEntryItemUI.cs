using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ChemLab.UI
{
    /// <summary>
    /// 实验入口条目预制体的绑定脚本：
    /// text1=实验名称，text2=实验描述，image=实验图片。
    /// </summary>
    public sealed class ExperimentEntryItemUI : MonoBehaviour
    {
        [Header("=== 绑定组件（可不填，将自动尝试查找） ===")]
        public Button button;

        [Tooltip("实验名称 Text（UGUI）")]
        public Text text1;

        [Tooltip("实验描述 Text（UGUI）")]
        public Text text2;

        [Tooltip("实验名称 TMP_Text（可选）")]
        public TMP_Text tmpText1;

        [Tooltip("实验描述 TMP_Text（可选）")]
        public TMP_Text tmpText2;

        [Tooltip("实验图片 Image（可选）")]
        public Image image;

        private void Awake()
        {
            if (button == null) button = GetComponent<Button>();
            if (image == null) image = GetComponentInChildren<Image>(true);

            if (text1 == null || text2 == null)
            {
                var texts = GetComponentsInChildren<Text>(true);
                if (text1 == null && texts.Length > 0) text1 = texts[0];
                if (text2 == null && texts.Length > 1) text2 = texts[1];
            }

            if (tmpText1 == null || tmpText2 == null)
            {
                var tmps = GetComponentsInChildren<TMP_Text>(true);
                if (tmpText1 == null && tmps.Length > 0) tmpText1 = tmps[0];
                if (tmpText2 == null && tmps.Length > 1) tmpText2 = tmps[1];
            }
        }

        public void SetTexts(string name, string desc)
        {
            if (text1 != null) text1.text = name ?? "";
            if (text2 != null) text2.text = desc ?? "";
            if (tmpText1 != null) tmpText1.text = name ?? "";
            if (tmpText2 != null) tmpText2.text = desc ?? "";
        }

        public void SetSprite(Sprite sprite)
        {
            if (image == null) return;
            image.sprite = sprite;
            image.enabled = sprite != null;
        }
    }
}

