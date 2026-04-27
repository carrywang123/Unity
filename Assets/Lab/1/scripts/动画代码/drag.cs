using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace game_1
{
    public class drag : MonoBehaviour
    {
        private Rigidbody rb;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
        }

        // 当药粉与其他物体发生碰撞时，执行此函数
        void OnCollisionEnter(Collision collision)
        {
            // 判断是否碰到了目标物体（比如杯子、坩埚等）
            if (collision.gameObject.CompareTag("Target"))
            {
                Destroy(gameObject);
            }
        }
    }
}
