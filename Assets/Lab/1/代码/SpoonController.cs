using UnityEngine;
using System.Collections;
using Unity.VisualScripting.Antlr3.Runtime;

namespace game_1
{
    public class SpoonController : MonoBehaviour
    {
        private Animator animator;
        public Animator animator1;
        public GameObject childObject; // 在 Inspector 里拖入子物体
        public GameObject childObject1;


        public void ShowChild()
        {
            childObject.SetActive(true);
        }

        public void HideChild()
        {
            childObject.SetActive(false);
        }

        public void Show()
        {
            childObject1.SetActive(true);
        }

        public void Hide()
        {
            childObject1.SetActive(false);
        }

        [Header("撒粉粒子系统")]
        public ParticleSystem powderParticle;

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        public void PlayBottle1Animation()
        {
            animator.SetTrigger("舀药");

        }

        public void PlayBottle1Animation1()
        {
            animator.SetTrigger("舀药1");

        }

        public void PlayBottleAnimation2()
        {
            StartCoroutine(PlayAnimations());
        }
        private IEnumerator PlayAnimations()
        {

                animator1.SetTrigger("开盖1");
                yield return new WaitForSeconds(1f);
                animator.SetTrigger("舀药2");

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

        public void ting()
        {
            animator.speed = 0f;
        }

        public void dong()
        {
            animator.speed = 1f;
        }
    }
}
