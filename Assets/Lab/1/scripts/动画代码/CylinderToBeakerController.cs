using UnityEngine;

namespace game_1
{
    public class CylinderToBeakerController : MonoBehaviour
    {
        public Animator cylinderAnimator;  // 量筒的 Animator

        [Header("量筒液体")]
        public Material cylinderLiquidMaterial;
        public float cylinderStartLevel = 1.0f;
        public float cylinderMinLevel = 0.3f;
        public float fillSpeed = 0.5f;

        [Header("烧杯液体")]
        public Material beakerLiquidMaterial;
        public float beakerStartLevel = 0.2f;
        public float beakerMaxLevel = 1.0f;

        public float pourSpeed = 0.1f;

        private float cylinderCurrentLevel = 8.93f;
        private float beakerCurrentLevel = -5f;

        private bool isPouring = false;

        void Start()
        {
            cylinderAnimator = GetComponent<Animator>();
        }

        public void Pour()
        {
            cylinderAnimator.SetTrigger("开始"); // 播放量筒倒液动画
        }

        // 动画事件中调用此函数
        public void StartPouring()
        {
            isPouring = true;
        }

        // 动画事件中调用此函数
        public void StopPouring()
        {
            isPouring = false;
        }

        void Update()
        {
            if (isPouring)
            {
                // 液面变化
                cylinderCurrentLevel -= fillSpeed * Time.deltaTime;
                beakerCurrentLevel += pourSpeed * Time.deltaTime;

                // 限制在最小/最大范围
                cylinderCurrentLevel = Mathf.Max(cylinderCurrentLevel, cylinderMinLevel);
                beakerCurrentLevel = Mathf.Min(beakerCurrentLevel, beakerMaxLevel);

                UpdateLiquidLevels();
            }
        }

        void UpdateLiquidLevels()
        {
            if (cylinderLiquidMaterial != null)
                cylinderLiquidMaterial.SetFloat("_LiquidLevel", cylinderCurrentLevel);

            if (beakerLiquidMaterial != null)
                beakerLiquidMaterial.SetFloat("_LiquidLevel", beakerCurrentLevel);
        }
    }
}
