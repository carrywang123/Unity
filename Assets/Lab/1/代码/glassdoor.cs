using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

namespace game_1
{
    public class glassdoor : MonoBehaviour
    {
        public Animator animator;
        private bool isOpen = false;

        void Start()
        {
            animator = GetComponent<Animator>();
        }

        public void Glassdoor()
        {
            if (isOpen)
            {
                // 🔹 玻璃门是打开状态，点击后关闭
                animator.SetTrigger("玻璃门返回");
                Debug.Log("玻璃门返回！");
            }
            else
            {
                // 🔹 玻璃门是关闭状态，点击后打开
                animator.SetTrigger("玻璃门移动");
                Debug.Log("玻璃门移动！");

            }
            Debug.Log("当前状态: " + (isOpen ? "打开 → 关闭" : "关闭 → 打开"));

            // 切换状态
            isOpen = !isOpen;
        }

    }
}
