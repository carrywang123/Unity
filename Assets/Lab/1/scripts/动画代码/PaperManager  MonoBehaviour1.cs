using System.Collections.Generic;
using UnityEngine;

namespace game_1
{
    public class PaperManager1 : MonoBehaviour
    {
        public List<GameObject> paperList; // 拖入多个称量纸对象
        private int currentIndex = 0;

        public void UseNextPaper()
        {
            if (currentIndex >= paperList.Count)
            {
                Debug.Log("所有称量纸都已使用完！");
                return;
            }

            GameObject currentPaper = paperList[currentIndex];
            currentPaper.SetActive(true); // 激活这张纸（在动画中移动）

            paper paperScript = currentPaper.GetComponent<paper>();
            if (paperScript != null)
            {
                paperScript.Getpaper(); // 播放药纸动画
            }

            currentIndex++; // 下次用下一张纸
        }
    }
}
