using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace game_1
{
    public class mafuludoor : MonoBehaviour
    {
        public Animator animator;
        public Animator animator1;
        public Animator animator2;
        private bool isOpen = false;

        public void mafulu1()
        {
            StartCoroutine(PlayAnimations());
        }

        private IEnumerator PlayAnimations()
        {
            if (isOpen)
            {
                animator.SetTrigger("关");
                yield return new WaitForSeconds(2f);
                animator1.SetTrigger("guan");
                yield return new WaitForSeconds(1f);
                animator2.SetTrigger("guan");
            }
            else
            {
                animator1.SetTrigger("kai");
                yield return new WaitForSeconds(1f);
                animator2.SetTrigger("kai");
                yield return new WaitForSeconds(1f);
                animator.SetTrigger("开");
            }

            isOpen = !isOpen;
        }
    }
}
