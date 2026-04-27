using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace game_1
{
    public class HoldToRotate : MonoBehaviour
    {
        public float rotationSpeed = 5f; // 旋转速度
        private bool isDragging = false;

        void Update()
        {
            // 按下鼠标右键开始旋转
            if (Input.GetMouseButtonDown(0)) 
            {
                isDragging = true;
            }

            // 松开鼠标右键停止旋转
            if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;
            }

            // 拖拽鼠标旋转
            if (isDragging)
            {
                float mouseX = Input.GetAxis("Mouse X"); 
                transform.Rotate(Vector3.right * mouseX * rotationSpeed, Space.World);
            }
        }
    }
}
