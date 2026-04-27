    using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace game_1
{
    public class ganguo : MonoBehaviour
    {
        public Animator animator;
        private bool isOpen = false;



        void Start()
        {
            animator = GetComponent<Animator>();
        }

        public void ganguo1()
        {
            if (!isOpen)
            {
                animator.SetTrigger("开盖");
            }
            if (isOpen)
            {
                animator.SetTrigger("关盖");
            }
            isOpen = !isOpen;
        }

    }
}
