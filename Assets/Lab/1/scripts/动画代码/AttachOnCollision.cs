using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace game_1
{
    public class AttachOnCollision : MonoBehaviour
    {
        public Animator animator;
        public GameObject transferTarget;
        public Transform holdPoint;
        private GameObject crucible;

        void Start()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
        }

        // 播放动画
        public void jiaodongbang()
        {
            animator.SetTrigger("开始");
        }

        // 使用触发器方式夹住坩埚
        void OnTriggerEnter(Collider collision)
        {
            if (collision.gameObject.CompareTag("saoping") )
            {
                crucible = collision.gameObject;
                crucible.transform.SetParent(holdPoint);
            }
        }

        public void zhuanyi()
        {
            crucible.transform.SetParent(transferTarget.transform);
        }

    }
}
