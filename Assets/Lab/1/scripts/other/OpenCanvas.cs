using UnityEngine;

namespace game_1
{
    public class OpenCanvas: MonoBehaviour
    {
        public Canvas targetCanvas; // 拖拽Canvas到Inspector中

        private void OnMouseDown()
        {
            if (targetCanvas != null)
            {
                targetCanvas.gameObject.SetActive(true);
            }
        }
    }}
