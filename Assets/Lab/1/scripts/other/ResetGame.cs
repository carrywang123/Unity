using UnityEngine;
using UnityEngine.SceneManagement;

namespace game_1
{
    public class ResetGame : MonoBehaviour
    {
        public void ResetToInitialState()
        {
            // 获取当前场景名称并重新加载
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }}
