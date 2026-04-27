using UnityEngine;
using UnityEngine.UI;

namespace game_1
{
    public class CameraSwitcher : MonoBehaviour
    {
        public Camera mainCamera;
        public Camera zoomCamera;
        public Canvas uiCanvas;
        private bool isZoomedIn = false;

        void Start()
        {
            zoomCamera.enabled = false; // 默认关闭特写相机
        }

        public void ToggleCamera()
        {
            if (!isZoomedIn)
            {
                mainCamera.enabled = false;
                zoomCamera.enabled = true;
                uiCanvas.worldCamera = zoomCamera;
            }
            else
            {
                zoomCamera.enabled = false;
                mainCamera.enabled = true;
                uiCanvas.worldCamera = mainCamera;
            }

            isZoomedIn = !isZoomedIn;
        }

        void Update()
        {
            if (isZoomedIn && Input.GetKeyDown(KeyCode.Escape))
            {
                zoomCamera.enabled = false;
                mainCamera.enabled = true;
                uiCanvas.worldCamera = mainCamera;
                isZoomedIn = false;
            }
        }
    }
}
