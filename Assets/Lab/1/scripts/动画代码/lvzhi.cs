using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace game_1
{
    public class lvzhi : MonoBehaviour
    {
        public Animator animator;
        public Transform holdPoint;
        public GameObject paper;

        public void yidong()
        {
            animator.SetTrigger("移动");
        }
        public void setparent()
        {
            paper.transform.SetParent(holdPoint);
        }
    }
}
