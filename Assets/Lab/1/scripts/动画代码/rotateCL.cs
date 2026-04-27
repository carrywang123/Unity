using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace game_1
{
    public class rotateCL : MonoBehaviour
    {
        public Animator animator; 
        public Animator animator1;

        public bool isOpen = false;   
        public void CL()
        {
            StartCoroutine(PlayAnimation1());
        }

        private IEnumerator PlayAnimation1()
        {
            if (!isOpen)
            {
                animator.SetTrigger("旋转");
                yield return new WaitForSeconds(1f);
                animator1.SetTrigger("旋转");
            }
            else
            {
                animator.SetTrigger("关闭");
                yield return new WaitForSeconds(1f);
            }
            isOpen = !isOpen;
        }


    }
}
