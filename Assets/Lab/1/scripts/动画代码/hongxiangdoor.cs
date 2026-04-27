using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace game_1
{
    public class hongxiangdoor : MonoBehaviour
    {
        public Animator animator;

        private bool isOpen = false;

        public void door()
        {
            if (!isOpen)
            {
                animator.SetTrigger("开");
            }
            else
            {
                animator.SetTrigger("关");
            }
            isOpen = !isOpen;
        }
    }
}
