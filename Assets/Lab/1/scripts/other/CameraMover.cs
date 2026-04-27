using UnityEngine;

namespace game_1
{
    public class CameraMover : MonoBehaviour
    {
        public Transform targetPosition; // 目标位置和旋转
        public float moveSpeed = 5f;    // 移动速度
        public float rotationSpeed = 2f; // 旋转速度
        private bool shouldMove = false; // 是否应该移动

        void Update()
        {
            if (shouldMove)
            {
                // 平滑移动位置
                Camera.main.transform.position = Vector3.Lerp(
                    Camera.main.transform.position,
                    targetPosition.position,
                    moveSpeed * Time.deltaTime
                );

                // 平滑旋转
                Camera.main.transform.rotation = Quaternion.Slerp(
                    Camera.main.transform.rotation,
                    targetPosition.rotation,
                    rotationSpeed * Time.deltaTime
                );

                // 检查是否接近目标位置和旋转
                float positionDistance = Vector3.Distance(Camera.main.transform.position, targetPosition.position);
                float angleDistance = Quaternion.Angle(Camera.main.transform.rotation, targetPosition.rotation);

                if (positionDistance < 0.1f && angleDistance < 1f)
                {
                    shouldMove = false;
                    Camera.main.transform.position = targetPosition.position; // 确保精确到达
                    Camera.main.transform.rotation = targetPosition.rotation; // 确保精确旋转
                }
            }
        }

        // 这个方法将被按钮调用
        public void MoveCameraToPosition()
        {
            shouldMove = true;
        }
    }}
