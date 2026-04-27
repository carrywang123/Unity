using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace game_1
{
    public class jiaobanall1 : MonoBehaviour
    {
        public Animator animator;
        public Animator animator1;
        public Animator animator2;
        public GameObject san;
        public GameObject san1;

        public void S1()
        {
            animator.SetTrigger("上升");
        }

        public void S2()
        {
            animator1.SetTrigger("开始");
        }

        public void S3()
        {
            animator2.SetTrigger("上升");
        }

        public void end1()
        {
            animator1.SetTrigger("结束");
        }
        public void Hide()
        {
            san.SetActive(false);
            san1.SetActive(true);
        }
    }
}
