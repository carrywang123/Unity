using UnityEngine;
using UnityEngine.SceneManagement;
using ChemLab.Managers;

namespace game_1
{
    public class ExitGame : MonoBehaviour
    {
        public void QuitGame()
        {
            // 返回主界面 Main，并尽量跳过登录：
            // - 若当前已有登录态（DataManager.CurrentUser != null），Main 场景会直接进入对应面板
            // - 若从实验场景直接启动导致无登录态，则自动用默认管理员 222/222 登录并进入主界面
            var runner = new GameObject("ReturnToMainRunner");
            DontDestroyOnLoad(runner);
            runner.AddComponent<ReturnToMainRunner>().Begin();
        }

        private sealed class ReturnToMainRunner : MonoBehaviour
        {
            public void Begin()
            {
                StartCoroutine(Run());
            }

            private System.Collections.IEnumerator Run()
            {
                const string mainSceneName = "Main";

                if (!Application.CanStreamedLevelBeLoaded(mainSceneName))
                {
                    Debug.LogError($"[ExitGame] 未能加载场景：{mainSceneName}，请确认已加入 Build Settings。");
                    Destroy(gameObject);
                    yield break;
                }

                SceneManager.LoadScene(mainSceneName);

                // 等待一帧让 DataManager/UIManager 完成 Awake/Start
                yield return null;

                if (DataManager.Instance != null && DataManager.Instance.CurrentUser == null)
                {
                    if (DataManager.Instance.Login("222", "222", out _))
                    {
                        if (UIManager.Instance != null)
                            UIManager.Instance.NavigateByRole();
                    }
                }

                Destroy(gameObject);
            }
        }
    }}
