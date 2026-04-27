using UnityEngine;

namespace game_1
{
    public class PowderCollisionHandler : MonoBehaviour
    {
        public GameObject powderPilePrefab; // 拖入你的粉末预设
        public int maxPerFrame = 5;         // 每帧最多生成多少堆

        void OnParticleCollision(GameObject other)
        {
            ParticleSystem ps = GetComponent<ParticleSystem>();
            ParticleCollisionEvent[] events = new ParticleCollisionEvent[16];
            int count = ps.GetCollisionEvents(other, events);

            int created = 0;

            for (int i = 0; i < count && created < maxPerFrame; i++)
            {
                Vector3 pos = events[i].intersection;

                if (powderPilePrefab != null)
                {
                    GameObject powder = Instantiate(
                        powderPilePrefab,
                        pos,
                        Quaternion.Euler(0, Random.Range(0, 360), 0)
                    );
                    powder.transform.SetParent(other.transform); // 设置为碰撞对象的子物体
                    created++;
                }
            }
        }
    }}
