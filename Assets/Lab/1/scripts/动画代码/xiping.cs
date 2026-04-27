using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace game_1
{
    public class xiping : MonoBehaviour
    {
        public Animator animator;


        [Header("撒粉粒子系统")]
        public ParticleSystem powderParticle;


        public void daoru()
        {
            animator.SetTrigger("倒入");
        }

        public void PlayPowder()
        {
            if (powderParticle != null)
            {
                powderParticle.Play();
                Debug.Log("动画事件触发：开始撒粉！");
            }
        }

        public void StopPowder()
        {
            if (powderParticle != null)
            {
                powderParticle.Stop();
                Debug.Log("动画事件触发：停止撒粉！");
            }
        }
    }
}
