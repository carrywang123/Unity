using UnityEngine;

namespace game_1
{
    public class OpenPanel : MonoBehaviour
    {
        public GameObject panel; // 拖入Panel

        public void TogglePanel()
        {
            panel.SetActive(!panel.activeSelf);
        }
    }}
