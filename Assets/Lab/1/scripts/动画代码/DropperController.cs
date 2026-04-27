using UnityEngine;

namespace game_1
{
    public class DropperController : MonoBehaviour
    {
        public Animator dropperAnimator;

        [Header("量筒液体控制")]
        public Material cylinderLiquidMaterial;
        public float cylinderStartLevel = 0.3f;
        public float cylinderMaxLevel = 1.0f;

        [Header("瓶中液体控制")]
        public Material bottleLiquidMaterial;
        public float bottleStartLevel = 1.2f;
        public float bottleMinLevel = 0.3f;

        [Header("胶头滴管液体控制")]
        public Material dropperMaterial;
        public float dropperStartLevel = 0.3f;  
        public float dropperMaxLevel = 1.2f;

        public float fillSpeed = 0.5f;    
        public float Speed = 1.0f;        
        public float dropSpeed = 1.0f;

        private float cylinderCurrentLevel = 4.333562f;
        private float bottleCurrentLevel = 12.29791f;
        private float dropperCurrentLevel = 0;

        private bool isAbsorbing = false;
        private bool isPouring = false;

        void Start()
        {
            dropperAnimator = GetComponent<Animator>();


        }

        public void AbsorbLiquid()   // 动画事件触发开始吸液
        {
            dropperAnimator.SetTrigger("开始");

        }

        public void Startabsorbing()
        {
            isAbsorbing = true;
        }

        public void Stopabsorbing()
        {
            isAbsorbing = false;
        }
        public void StartPouring()   // 动画事件触发开始倒液
        {
            isPouring = true;
        }

        public void StopPouring()    // 动画事件触发停止倒液
        {
            isPouring = false;
        }

        void Update()
        {
            if (isAbsorbing)
            {
                // 瓶子液面降低，滴管液面升高，量筒液面不变
                bottleCurrentLevel -= Speed * Time.deltaTime;
                bottleCurrentLevel = Mathf.Clamp(bottleCurrentLevel, bottleMinLevel, bottleStartLevel);

                dropperCurrentLevel += dropSpeed * Time.deltaTime;
                dropperCurrentLevel = Mathf.Clamp(dropperCurrentLevel, dropperStartLevel, dropperMaxLevel);

                UpdateLiquidLevels();


            }
            else if (isPouring)
            {
                // 滴管液面降低，量筒液面升高，瓶子液面不变
                dropperCurrentLevel -= dropSpeed * Time.deltaTime;
                dropperCurrentLevel = Mathf.Clamp(dropperCurrentLevel, dropperStartLevel, dropperMaxLevel);

                cylinderCurrentLevel += fillSpeed  * Time.deltaTime;
                cylinderCurrentLevel = Mathf.Clamp(cylinderCurrentLevel, cylinderStartLevel, cylinderMaxLevel);

                UpdateLiquidLevels();
            }
        }

        private void UpdateLiquidLevels()
        {
            if (cylinderLiquidMaterial != null)
                cylinderLiquidMaterial.SetFloat("_LiquidLevel", cylinderCurrentLevel);

            if (bottleLiquidMaterial != null)
                bottleLiquidMaterial.SetFloat("_LiquidLevel", bottleCurrentLevel);

            if (dropperMaterial != null)
                dropperMaterial.SetFloat("_LiquidLevel", dropperCurrentLevel);
        }
    }
}
