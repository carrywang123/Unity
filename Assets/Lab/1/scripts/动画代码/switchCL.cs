using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

namespace game_1
{
    public class switchCL : MonoBehaviour
    {
        public Animator animator;
        public Animator animator1;
        private bool isOpen = false;
        public void CL()
        {
            StartCoroutine(PlayAnimations());
        }

        private IEnumerator PlayAnimations()
        {
            if (isOpen)
            {
                animator1.SetTrigger("结束");
                yield return new WaitForSeconds(1f);
                animator.SetTrigger("结束");

            }
            else
            {
                animator.SetTrigger("开始");
                yield return new WaitForSeconds(1f);
                animator1.SetTrigger("开始");
            }

            isOpen = !isOpen;
        }
    }
}
