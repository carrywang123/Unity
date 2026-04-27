using UnityEngine;
using UnityEngine.UI;

namespace game_1
{
    public class FanSliderControl : MonoBehaviour
    {
        public Slider slider;            // 拖入 UI 滑条
        public Animator knobAnimator;   // 按钮动画器
        public Transform fan;           // 风扇模型
        public float maxSpeed = 800f;   // 风扇最大转速

        private float currentSpeed = 0f;

        void Start()
        {
            // 初始化按钮动画播放状态
            knobAnimator.Play("Knob_Rotate", 0, 0f);
            knobAnimator.speed = 0f;

            // 注册滑条事件
            slider.onValueChanged.AddListener(OnSliderChanged);
        }

        void OnSliderChanged(float value)
        {
            // 控制按钮旋转动画进度（0~1）
            knobAnimator.Play("Knob_Rotate", 0, value);

            // 设置风扇当前转速
            currentSpeed = Mathf.Lerp(0f, maxSpeed, value);
        }

        void Update()
        {
            // 沿 Y 轴旋转风扇
            fan.Rotate(Vector3.up, currentSpeed * Time.deltaTime);
        }
    }
}
