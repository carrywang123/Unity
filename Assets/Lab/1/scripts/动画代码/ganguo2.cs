using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace game_1
{
    public class ganguo2 : MonoBehaviour
    {
        public GameObject crucible;
        public GameObject[] crucible1;

        public void daoru()
        {
            for(int i = 0; i < crucible1.Length; i++)
            {
                crucible1[i].SetActive(false);
            }
            crucible.SetActive(true);
        }
    }
}
