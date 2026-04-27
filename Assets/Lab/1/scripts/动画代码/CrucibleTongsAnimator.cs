using System.Collections;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

namespace game_1
{
    public class TongsCrucibleController : MonoBehaviour
    {
        public Animator animator;
        public GameObject[] crucibles;
        public GameObject[] crucibles1;
        public GameObject[] crucibles2;
        public GameObject[] crucibles3;
        public GameObject transferTarget;
        public GameObject transferSource;
        public GameObject transferOther;
        public GameObject transferOther1;
        public Transform holdPoint;
        private GameObject crucible;
        private int choice = 0;
        private int i = 0; 
        private bool isOpen = false;
        public Animator animator1;

        void Start()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
        }

        // 播放动画
        public void ganguoqian()
        {
            if(choice == 0)
            {
                animator.SetTrigger("开始");
            }
        
            if(choice == 1)
            {
                animator.SetTrigger("结束");
                crucible = null;
            }
            if (choice >= 2 && choice!=4 && choice <= 12)
            {
                if (!isOpen)
                {
                    crucibles[i].SetActive(true);
                    crucibles1[i].SetActive(false);
                    animator.SetTrigger("2");
                    crucible = null;
                }
                else
                {
                    animator.SetTrigger("其他坩埚取出");
                    crucible = null;
                }
                isOpen = !isOpen;
            }
            if (choice == 4)
            {
                animator.SetTrigger("取出一号坩埚");
                crucible = null;
            }
            choice++;
        }
    
        public void putG()
        {
            int i = 0;
            for (i = 0; i < 6; i++)
            {
                crucibles3[i].SetActive(true);
                crucibles2[i].SetActive(false);
            }
        }


        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Target") && crucible == null)
            {
                crucible = other.gameObject;
                crucible.transform.SetParent(holdPoint);
            }
        }
        public void zhuanyi()
        {
            crucible.transform.SetParent(transferTarget.transform);
        }

        public void zhuanyi1()
        {
            crucible.transform.SetParent(transferSource.transform);

        }
        public void zhuanyi2()
        {
            crucible.transform.SetParent(transferOther.transform);
        }

        public void zhuanyi3()
        {
            crucible.transform.SetParent(transferOther1.transform);
        }

        public void yidong()
        {
            crucibles[i].SetActive(false);
            crucibles1[i].SetActive(true);
            i++;
        }


    }
}
