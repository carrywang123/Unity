// ============================================================
// 文件名：SceneBootstrap.cs
// 功  能：场景启动引导脚本
//         挂载在场景根节点 [Bootstrap] 上
//         负责确保 DataManager 和 UIManager 单例正确初始化
// 作  者：化工虚拟仿真实验平台
// ============================================================

using UnityEngine;
using ChemLab.Managers;

namespace ChemLab.Managers
{
    /// <summary>
    /// 场景启动引导
    /// 将此脚本挂载到场景中的一个空GameObject上（命名为 [Bootstrap]）
    /// 并在 Inspector 中将 DataManager 和 UIManager 的 Prefab 拖入
    /// </summary>
    public class SceneBootstrap : MonoBehaviour
    {
        [Header("=== 管理器预制体 ===")]
        [Tooltip("DataManager 预制体（若场景中已有则留空）")]
        public GameObject dataManagerPrefab;

        [Tooltip("UIManager 预制体（若场景中已有则留空）")]
        public GameObject uiManagerPrefab;

        [Header("=== 调试选项 ===")]
        [Tooltip("是否在控制台打印数据库路径")]
        public bool printDatabasePath = true;

        private void Awake()
        {
            // 确保 DataManager 存在
            if (DataManager.Instance == null && dataManagerPrefab != null)
            {
                Instantiate(dataManagerPrefab);
                Debug.Log("[Bootstrap] DataManager 已实例化。");
            }

            // 确保 UIManager 存在
            if (UIManager.Instance == null && uiManagerPrefab != null)
            {
                Instantiate(uiManagerPrefab);
                Debug.Log("[Bootstrap] UIManager 已实例化。");
            }
        }

        private void Start()
        {
            if (printDatabasePath && DataManager.Instance != null)
            {
                Debug.Log($"[Bootstrap] 数据库路径：{DataManager.Instance.GetDatabasePath()}");
            }
        }
    }
}
