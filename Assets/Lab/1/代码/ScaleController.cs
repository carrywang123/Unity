using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace game_1
{
    using TMPro;  // 需要导入TextMeshPro命名空间

    public class ScaleController : MonoBehaviour
    {
        public TextMeshPro weightText; // 电子秤的 UI 文本
        public GameObject powerButton; // 物理按钮
        public GameObject screenObject; // 电子秤的屏幕物体
        public float longPressTime = 1.0f; // 长按时间（秒）
        private bool isPowerOn = false; // 电子秤是否开机
        private bool isPressing = false; // 是否正在按住按钮
        private float pressStartTime; // 记录按住时间
        private HashSet<Rigidbody> objectsOnScale = new HashSet<Rigidbody>(); // 记录秤上的物品
        private Renderer buttonRenderer; // 物理按钮的材质
        private Renderer screenRenderer; // 屏幕物体的 Renderer
        private Material screenMaterial; // 屏幕的材质


        void Start()
        {
            weightText.text = "MT.GREEN"; // 初始状态不显示重量
            buttonRenderer = powerButton.GetComponent<Renderer>(); // 获取按钮的材质
            screenRenderer = screenObject.GetComponent<Renderer>(); // 获取屏幕物体的 Renderer
            screenMaterial = screenRenderer.material; // 获取屏幕的材质
        }


        private void OnTriggerEnter(Collider other)
        {
            if (isPowerOn && other.attachedRigidbody)
            {
                objectsOnScale.Add(other.attachedRigidbody);
                UpdateWeightDisplay();
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (isPowerOn && other.attachedRigidbody)
            {
                objectsOnScale.Remove(other.attachedRigidbody);
                UpdateWeightDisplay();
            }
        }

        private void UpdateWeightDisplay()
        {
            if (!isPowerOn) return;

            float totalMass = 0f;
            foreach (Rigidbody rb in objectsOnScale)
            {
                totalMass += rb.mass;
            }

            weightText.text =  totalMass.ToString("F4") ; // 保留2位小数
        }

        void Update()
        {
            // 检测鼠标点击
            if (Input.GetMouseButtonDown(0)) // 0 = 左键
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.gameObject == powerButton)
                {
                    isPressing = true;
                    pressStartTime = Time.time;
                }
            }

            // 检测鼠标松开
            if (Input.GetMouseButtonUp(0) && isPressing)
            {
                isPressing = false;
                float pressDuration = Time.time - pressStartTime;

                if (pressDuration >= longPressTime)
                {
                    TogglePower(); // 长按：开机/关机
                }
                else
                {
                    ResetScale(); // 短按：清零
                }
            }
        }

        // 长按：开机/关机
        private void TogglePower()
        {
            isPowerOn = !isPowerOn;
            if (isPowerOn)
            {
                weightText.text = "000.0000";
                SetScreenEmission(true); // 开机时屏幕变亮
            }
            else
            {
                weightText.text = "MT.GREEN";
                SetScreenEmission(false); // 关机时屏幕恢复原色
            }
        }

        private void SetScreenEmission(bool isOn)
        {
            if (isOn)
            {
                // 设置发光颜色为白色，启用发光效果
                screenMaterial.SetColor("_EmissionColor", Color.white);
                screenMaterial.EnableKeyword("_EMISSION"); // 启用发光
            }
            else
            {
                // 设置发光颜色为黑色，禁用发光效果
                screenMaterial.SetColor("_EmissionColor", Color.black);
                screenMaterial.DisableKeyword("_EMISSION"); // 禁用发光
            }
        }

        // 短按：清零
        private void ResetScale()
        {
            if (!isPowerOn) return; // 关机状态不清零

            objectsOnScale.Clear();
            weightText.text = "0.0000";
        }

        public void resetscale()
        {
            if (!isPowerOn) return;
            weightText.text = "0.0000";
        }

        public void togglePower()
        {
            isPowerOn = !isPowerOn;
            if (isPowerOn)
            {
                weightText.text = "000.0000";
                SetScreenEmission(true); // 开机时屏幕变亮
            }
            else
            {
                weightText.text = "MT.GREEN";
                SetScreenEmission(false); // 关机时屏幕恢复原色
            }
        }
    }

}
