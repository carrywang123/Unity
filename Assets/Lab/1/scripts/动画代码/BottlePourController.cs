using UnityEngine;

namespace game_1
{
    public class BottlePourController : MonoBehaviour
    {
        public Animator bottleAnimator;

        [Header("量筒液体控制")]
        public Material cylinderLiquidMaterial;
        public float cylinderStartLevel = 0.3f;
        public float cylinderMaxLevel = 1.0f;
        public float Speed = 1.0f;

        [Header("瓶中液体控制")]
        public Material bottleLiquidMaterial;
        public float bottleStartLevel = 1.2f;
        public float bottleMinLevel = 0.3f;

        public float fillSpeed = 0.01f;

        private float cylinderCurrentLevel;
        private float bottleCurrentLevel;
        private bool isPouring = false;

        void Start()
        {
            bottleAnimator = GetComponent<Animator>();

            cylinderCurrentLevel = cylinderStartLevel;
            bottleCurrentLevel = bottleStartLevel;

            if (cylinderLiquidMaterial != null)
                cylinderLiquidMaterial.SetFloat("_LiquidLevel", cylinderCurrentLevel);
            if (bottleLiquidMaterial != null)
                bottleLiquidMaterial.SetFloat("_LiquidLevel", bottleCurrentLevel);
        }

        public void daoshui()
        {
            bottleAnimator.SetTrigger("倒水");
        }

        public void StartPouring()
        {
            isPouring = true;
        }

        public void StopPouring()
        {
            isPouring = false;
        }

        void Update()
        {
            if (isPouring)
            {
                // 液面同步变化
                cylinderCurrentLevel += Speed * Time.deltaTime;
                bottleCurrentLevel -= fillSpeed * Time.deltaTime;

                // Clamp 范围
                cylinderCurrentLevel = Mathf.Clamp(cylinderCurrentLevel, cylinderStartLevel, cylinderMaxLevel);
                bottleCurrentLevel = Mathf.Clamp(bottleCurrentLevel, bottleMinLevel, bottleStartLevel);

                if (cylinderLiquidMaterial != null)
                    cylinderLiquidMaterial.SetFloat("_LiquidLevel", cylinderCurrentLevel);
                if (bottleLiquidMaterial != null)
                    bottleLiquidMaterial.SetFloat("_LiquidLevel", bottleCurrentLevel);
            }
        }
    }
}
