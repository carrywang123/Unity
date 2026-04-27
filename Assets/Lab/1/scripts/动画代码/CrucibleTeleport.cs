using UnityEngine;

namespace game_1
{
    public class CrucibleTeleport : MonoBehaviour
    {
        public GameObject[] crucibles;         // 拖入坩埚对象
        public Transform[] targetPositions;    // 拖入目标位置（空物体）

        public float delayTime = 0.5f;         // 消失后等待多少秒再出现

        public void TeleportCrucibles()
        {
            // 先隐藏所有坩埚
            for (int i = 0; i < crucibles.Length; i++)
            {
                crucibles[i].SetActive(false);
            }

            // 开始延迟传送
            StartCoroutine(TeleportWithDelay());
        }

        private System.Collections.IEnumerator TeleportWithDelay()
        {
            yield return new WaitForSeconds(delayTime);

            for (int i = 0; i < crucibles.Length; i++)
            {
                // 设置新位置
                crucibles[i].transform.position = targetPositions[i].position;

                // 显示坩埚
                crucibles[i].SetActive(true);
            }
        }
    }
}
