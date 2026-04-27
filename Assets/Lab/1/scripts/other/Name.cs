using UnityEngine;

namespace game_1
{
    public class Name : MonoBehaviour
    {
        private bool isShowTip;
        private Collider objectCollider;

        void Start()
        {
            isShowTip = false;
            objectCollider = GetComponent<Collider>();

            // 确保物体有Collider组件
            if (objectCollider == null)
            {
                Debug.LogError("No Collider component found on this object. Adding a BoxCollider.");
                gameObject.AddComponent<BoxCollider>();
                objectCollider = GetComponent<Collider>();
            }
        }

        void Update()
        {
            // 没有主相机时，直接返回，不报错
            if (Camera.main == null)
            {
                // 可选：如果你连提示都不想要，这行可以删掉
                // Debug.LogWarning("No Main Camera in scene, skip raycast.");
                return;
            }

            // 碰撞体如果在运行时被移除，也防一下
            if (objectCollider == null)
            {
                objectCollider = GetComponent<Collider>();
                if (objectCollider == null) return;
            }

            // 创建从摄像机到鼠标位置的射线
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // 检测射线是否击中当前物体的碰撞体
            if (objectCollider.Raycast(ray, out hit, Mathf.Infinity))
            {
                isShowTip = true;
            }
            else
            {
                isShowTip = false;
            }
        }

        void OnGUI()
        {
            if (isShowTip)
            {
                GUIStyle style = new GUIStyle()
                {
                    fontSize = 15,
                    fontStyle = FontStyle.Bold
                };
                style.normal.textColor = Color.blue;

                // 在鼠标位置附近显示标签（使用物体自身的名字）
                Vector2 mousePosition = Event.current.mousePosition;
                GUI.Label(new Rect(mousePosition.x + 15, mousePosition.y - 10, 200, 30), gameObject.name, style);
            }
        }
    }
}
