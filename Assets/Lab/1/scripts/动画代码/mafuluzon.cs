using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace game_1
{
    public class mafuluzon : MonoBehaviour
    {
        public Animator animator;
        private bool isOpen = false;
        public Color newColor = Color.red;
        private Color originalColor;
        public GameObject targetObject;

        void Start()
        {
            animator = GetComponent<Animator>();
            Renderer renderer = targetObject.GetComponent<Renderer>();
            originalColor = renderer.material.color;
        }

        public void mafulu1()
        {
            StartCoroutine(mafulu2());
        }
        private IEnumerator mafulu2()
        {
            if (isOpen)
            {
                animator.SetTrigger("结束");
            }
            else
            {
                animator.SetTrigger("开始");

            }
            yield return new WaitForSeconds(1.2f);
            Renderer renderer = targetObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = isOpen ? originalColor : newColor;
            }
            isOpen = !isOpen;
        }
    }
}
