using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace game_1
{
    public class 视角转换 : MonoBehaviour
    {
        public GameObject Camera;
        public float rotationSpeed = 1000f; // 旋转速度
        public float normalMoveSpeed = 1000f; // 正常移动速度（与旋转速度匹配）
        public float minMoveSpeed = 200f; // 最小移动速度（减速后的最低速度）
        public float slowDownDistance = 1.5f; // 开始减速的距离
        public float speedBoostFactor = 1.5f; // 长按加速时的倍率
        private bool isSpeedBoosted = false;

        // 新增：用于撤销功能的变量
        private Stack<CameraState> stateHistory = new Stack<CameraState>();
        private float lastRecordTime = 0f;
        private float recordInterval = 0.5f; // 记录状态的时间间隔（秒

        void Start()
        {
            // 初始时记录一次状态
            RecordCameraState();
        }


        void Update()
        {

            // 检查是否按住Shift加速
            isSpeedBoosted = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            // 检查附近是否有物体
            float closestDistance = GetClosestObjectDistance();
            float currentMoveSpeed = CalculateMoveSpeed(closestDistance);

            // 应用加速效果
            float finalMoveSpeed = isSpeedBoosted ? currentMoveSpeed * speedBoostFactor : currentMoveSpeed;

            bool cameraMoved = false;

            if (Input.GetKey(KeyCode.W)) //W,前
            {
                Camera.transform.Translate(Vector3.forward * finalMoveSpeed * Time.deltaTime);
                cameraMoved = true;
            }
            if (Input.GetKey(KeyCode.S))  //S，后
            {
                Camera.transform.Translate(Vector3.back * finalMoveSpeed * Time.deltaTime);
                cameraMoved = true;
            }
            if (Input.GetKey(KeyCode.A))  //A，左
            {
                Camera.transform.Translate(Vector3.left * finalMoveSpeed * Time.deltaTime);
                cameraMoved = true;
            }
            if (Input.GetKey(KeyCode.D))  //D，右
            {
                Camera.transform.Translate(Vector3.right * finalMoveSpeed * Time.deltaTime);
                cameraMoved = true;
            }
            if (Input.GetKey(KeyCode.Z) && !Input.GetKey(KeyCode.LeftControl))  //Z，上（且没有按Ctrl）
            {
                Camera.transform.Translate(Vector3.up * finalMoveSpeed * Time.deltaTime);
                cameraMoved = true;
            }
            if (Input.GetKey(KeyCode.X))  //X，下
            {
                Camera.transform.Translate(Vector3.down * finalMoveSpeed * Time.deltaTime);
                cameraMoved = true;
            }

            if (Input.GetKey(KeyCode.Q))  //Q，顺
            {
                RotateClockwise();
                cameraMoved = true;
            }

            if (Input.GetKey(KeyCode.E))  //E，逆
            {
                RotateCounterClockwise();
                cameraMoved = true;
            }
            // 新增：定期记录相机状态
            if (cameraMoved && Time.time - lastRecordTime > recordInterval)
            {
                RecordCameraState();
                lastRecordTime = Time.time;
            }

            // 新增：Ctrl+Z撤销操作
            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) && Input.GetKeyDown(KeyCode.Z))
            {
                UndoLastAction();
            }
        }

        void RotateClockwise()
        {
            transform.Rotate(0, -rotationSpeed * Time.deltaTime, 0);
        }

        void RotateCounterClockwise()
        {
            transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
        }

        // 获取最近物体的距离
        float GetClosestObjectDistance()
        {
            float closestDistance = Mathf.Infinity;

            // 这里假设场景中有多个可交互物体，并且它们都有"Interactable"标签
            // 你可以根据实际情况修改标签或检测方式
            GameObject[] objects = GameObject.FindGameObjectsWithTag("Interactable");

            foreach (GameObject obj in objects)
            {
                float distance = Vector3.Distance(Camera.transform.position, obj.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                }
            }

            return closestDistance;
        }

        // 根据距离计算当前移动速度
        float CalculateMoveSpeed(float distance)
        {
            if (distance > slowDownDistance)
            {
                return normalMoveSpeed;
            }
            else
            {
                // 使用平滑的减速曲线（二次函数）
                float t = Mathf.Clamp01(distance / slowDownDistance);
                float speedFactor = Mathf.Lerp(minMoveSpeed, normalMoveSpeed, t * t);
                return speedFactor;
            }
        }
        // 新增：记录相机状态
        private void RecordCameraState()
        {
            CameraState state = new CameraState
            {
                position = Camera.transform.position,
                rotation = Camera.transform.rotation
            };
            stateHistory.Push(state);

            // 限制历史记录数量，防止内存占用过大
            if (stateHistory.Count > 20)
            {
                // 创建一个新栈，只保留最近的19个状态加上当前状态
                var newStack = new Stack<CameraState>();
                while (stateHistory.Count > 19)
                {
                    stateHistory.Pop();
                }
            }
        }

        // 新增：撤销上一步操作
        private void UndoLastAction()
        {
            if (stateHistory.Count > 1)
            {
                // 弹出当前状态
                stateHistory.Pop();
                // 获取上一个状态
                CameraState previousState = stateHistory.Peek();

                // 应用上一个状态
                Camera.transform.position = previousState.position;
                Camera.transform.rotation = previousState.rotation;
            }
        }

        // 新增：用于存储相机状态的结构
        private struct CameraState
        {
            public Vector3 position;
            public Quaternion rotation;
        }
    }}
