using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace game_1
{
    public class choulvSwitch : MonoBehaviour
    {
        public Animator animator;
        public GameObject targetObject;
        public Animator animator1;
        public Color newColor = Color.red;
        private Color originalColor;
        private bool isOpen = false;

        void Start()
        {
            Renderer renderer = targetObject.GetComponent<Renderer>();
            originalColor = renderer.material.color;
        }
        public void switch1()
        {
            StartCoroutine(switch2());
        }
        private IEnumerator switch2()
        {
            if (!isOpen)
            {
                animator.SetTrigger("开");
                animator1.SetTrigger("开");
            }
            else
            {
                animator.SetTrigger("关");
                animator1.SetTrigger("关");
            }
            yield return new WaitForSeconds(2f);
            Renderer renderer = targetObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = isOpen ? originalColor : newColor;
            }
            isOpen = !isOpen;
        }
    }
}
