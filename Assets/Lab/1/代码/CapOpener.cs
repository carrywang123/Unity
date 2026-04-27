using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace game_1
{
    public class CapOpener : MonoBehaviour
    {
        public Animator animator;
        private bool isOpen = false;

        void Start()
        {
            animator = GetComponent<Animator>();
        }

        public void PlayAnimation()
        {
            if (isOpen)
            {
                animator.SetTrigger("返回");
            }
            else
            {
                animator.SetTrigger("瓶子");
            }
            isOpen = !isOpen;
        }
    }}
