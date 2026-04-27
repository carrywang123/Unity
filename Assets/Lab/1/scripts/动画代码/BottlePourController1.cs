using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace game_1
{
    public class BottlePourController1 : MonoBehaviour
    {
        public Animator beakerAnimator;
        public GameObject Object1;

        [Header("三颈烧瓶液体")]
        public Material beakerLiquidMaterial;
        public float beakerStartLevel = 1.0f;
        public float beakerMinLevel = 0.2f;
        public float fillSpeed = 0.5f;

        [Header("烧瓶液体")]
        public Material flaskLiquidMaterial;
        public float flaskStartLevel = 0.2f;
        public float flaskMaxLevel = 1.0f;

        public float pourSpeed = 0.1f;

        private float beakerCurrentLevel = 8.5f;
        private float flaskCurrentLevel = -2;

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
        public void chuxian()
        {
            Object1.SetActive(true); 
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
