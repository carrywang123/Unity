using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace game_1
{
    public class connect : MonoBehaviour
    {
        private Animator animator;
        private bool isOpen = false;
        public GameObject GameObject;
        public GameObject GameObject1;
        void Start()
        {
            animator = GetComponent<Animator>();
        }

        public void Connct1()
        {
            if (!isOpen)
            {
                animator.SetTrigger("1");
            }
            else
            {
                animator.SetTrigger("还原");
            }
            isOpen = !isOpen;
        }
        public void Connct2() 
        {
            animator.SetTrigger("2");
        }

        public void xiaoS()
        {
            GameObject.SetActive(false);
            GameObject1.SetActive(true);
        }
    }
}
