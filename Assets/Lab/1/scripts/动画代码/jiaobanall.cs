using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace game_1
{
    public class jiaobanall : MonoBehaviour
    {
        public Animator animator;
        public Animator animator1;
        public Animator animator2;

        public void Start1()
        {
            animator.SetTrigger("开始");
        }

        public void Start2()
        {
            animator1.SetTrigger("开始");
        }

        public void Start3()
        {
            animator2.SetTrigger("开始");
        }

        public void end()
        {
            animator1.SetTrigger("结束");
        }
    }
}
