using UnityEngine;

namespace game_1
{
    public class ToggleChildrenVisibility : MonoBehaviour
    {
        public GameObject parentPanel; // 这是“视角”Panel
        private bool isExpanded = false;

        public void Toggle()
        {
            isExpanded = !isExpanded;

            // 从第一个子物体开始遍历（不包含自己）
            for (int i = 1; i < parentPanel.transform.childCount; i++)
            {
                Transform child = parentPanel.transform.GetChild(i);
                // 跳过第一个作为标题（可选）
                if (child.name == "视角") continue;

                child.gameObject.SetActive(isExpanded);
            }
        }
    }
}
