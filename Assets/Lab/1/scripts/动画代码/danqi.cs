using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace game_1
{
    public class danqi : MonoBehaviour
    {
        public Animator animator;
        public Animator animator1;
        private bool isOpen = false;

        public void openN2()
        {
            if (!isOpen)
            {
                animator.SetTrigger("打开");
                animator1.SetTrigger("开");
            }
            else
            {
                animator.SetTrigger("关闭");
                animator1.SetTrigger("关");
            }
            isOpen = !isOpen;
        }
    }
}
