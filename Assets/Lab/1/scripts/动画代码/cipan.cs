using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace game_1
{
    public class cipan : MonoBehaviour
    {
        public Animator animator;

        public void yidong()
        {
            animator.SetTrigger("移动");
        }
    }
}
