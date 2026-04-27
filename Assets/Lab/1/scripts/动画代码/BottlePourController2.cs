using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace game_1
{
    public class BottlePourController2 : MonoBehaviour
    {
        public Animator beakerAnimator;

        [Header("NaOH液体")]
        public Material beakerLiquidMaterial;
        public float beakerStartLevel = 1.0f;
        public float beakerMinLevel = 0.2f;
        public float fillSpeed = 0.5f;

        [Header("安全瓶液体")]
        public Material flaskLiquidMaterial;
        public float flaskStartLevel = 0.2f;
        public float flaskMaxLevel = 1.0f;

        public float pourSpeed = 0.1f;

        private float beakerCurrentLevel = 13;
        private float flaskCurrentLevel = 0;

        private bool isPouring = false;


        // 播放烧杯倒液动画
        public void Pour()
        {
            beakerAnimator.SetTrigger("开始");
        }

        // 动画事件触发
        public void StartPouring()
        {
            isPouring = true;
        }

        public void StopPouring()
        {
            isPouring = false;
        }
        public void hight()
        {
            beakerLiquidMaterial.SetFloat("_LiquidLevel", 10);
        }
        void Update()
        {
            if (isPouring)
            {
                beakerCurrentLevel -= fillSpeed * Time.deltaTime;
                flaskCurrentLevel += pourSpeed * Time.deltaTime;

                beakerCurrentLevel = Mathf.Max(beakerCurrentLevel, beakerMinLevel);
                flaskCurrentLevel = Mathf.Min(flaskCurrentLevel, flaskMaxLevel);

                UpdateLiquidLevels();
            }
        }

        void UpdateLiquidLevels()
        {
            if (beakerLiquidMaterial != null)
                beakerLiquidMaterial.SetFloat("_LiquidLevel", beakerCurrentLevel);

            if (flaskLiquidMaterial != null)
                flaskLiquidMaterial.SetFloat("_LiquidLevel", flaskCurrentLevel);
        }

    }
}
