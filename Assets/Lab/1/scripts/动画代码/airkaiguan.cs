using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace game_1
{
    public class airkaiguan : MonoBehaviour
    {
        public Animator animator;
        private bool isOpen = false;

        void Start()
        {
            animator = GetComponent<Animator>();
        }

        public void mafulu2()
        {
            if (isOpen)
            {
                animator.SetTrigger("关");
            }
            else
            {
                animator.SetTrigger("开");

            }
            isOpen = !isOpen;
        }
    }
}
