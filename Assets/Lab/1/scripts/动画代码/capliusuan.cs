using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace game_1
{
    public class capliusuan : MonoBehaviour
    {
        public Animator animator;
        private bool isOpen = false;

        void Start()
        {
            animator = GetComponent<Animator>();
        }

        public void liusuangai()
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
