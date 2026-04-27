using UnityEngine;

namespace game_1
{
    using UnityEngine.UI; // 引入 UI 命名空间

    public class 切换摄像机 : MonoBehaviour
    {
        public Camera mainCamera;   // 引用主摄像机
        public Camera uiCamera;    // 引用UI摄像机
        public Canvas canvas;      // 引用Canvas
        public Button exitButton;  // 引用退出按钮

        private void Start()
        {
            // 初始化：默认使用主摄像机
            SwitchToMainCamera();

            // 为退出按钮添加点击事件监听
            if (exitButton != null)
            {
                exitButton.onClick.AddListener(OnExitButtonClicked);
            }
        }

        private void Update()
        {
            // 检测鼠标左键点击
            if (Input.GetMouseButtonDown(0))
            {
                HandleMouseClick();
            }
        }

        // 处理鼠标点击事件
        private void HandleMouseClick()
        {
            // 从主摄像机发射射线
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            // 检测射线是否击中物体
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                // 检查击中的物体是否带有"Clickable"标签
                if (hit.collider.CompareTag("Clickable"))
                {
                    // 如果击中物体，切换到UI摄像机
                    SwitchToUICamera(Input.mousePosition);
                }
            }
        }

        // 退出按钮点击事件处理
        private void OnExitButtonClicked()
        {
            SwitchToMainCamera();
        }

        // 切换到主摄像机
        public void SwitchToMainCamera()
        {
            // 启用主摄像机，禁用UI摄像机
            mainCamera.gameObject.SetActive(true);
            uiCamera.gameObject.SetActive(false);

            // 将Canvas的渲染摄像机设置为主摄像机
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = mainCamera;

            Debug.Log("切换到主摄像机。");
        }

        // 切换到UI摄像机
        private void SwitchToUICamera(Vector3 mousePosition)
        {
            // 启用UI摄像机，主摄像机仍然启用
            mainCamera.gameObject.SetActive(true);
            uiCamera.gameObject.SetActive(true);

            // 将Canvas的渲染摄像机设置为UI摄像机
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = uiCamera;

            // 将Canvas显示在鼠标点击的位置
            MoveCanvasToMousePosition(mousePosition);

            Debug.Log("切换到UI摄像机。");
        }

        // 将Canvas移动到鼠标点击的位置
        private void MoveCanvasToMousePosition(Vector3 mousePosition)
        {
            // 将屏幕坐标转换为Canvas坐标
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.GetComponent<RectTransform>(),
                mousePosition,
                uiCamera,
                out Vector2 localPoint
            );

            // 设置Canvas位置
            canvas.GetComponent<RectTransform>().anchoredPosition = localPoint;
        }
    }}
