using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace game_1
{
    public class paper : MonoBehaviour
    {
        private Animator animator;
        public Animator animator1;
        public GameObject childObject;
        public ParticleSystem powderParticle;

        public void HideChild()
        {
            childObject.SetActive(false);
        }
        void Start()
        {
            animator = GetComponent<Animator>();
        }

        // 播放药纸倾斜动画
        public void Movepaper()
        {
            StartCoroutine(PlayAnimations());
        }
        private IEnumerator PlayAnimations()
        {
            animator1.SetTrigger("开始");
            yield return new WaitForSeconds(1f);
            animator.SetTrigger("药纸");
        }
        // 播放返回动画
        public void Getpaper()
        {
            animator.SetTrigger("返回");
        }
        public void OnAnimationEnd()
        {
            gameObject.SetActive(false); // 动画播放完就隐藏自己
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
