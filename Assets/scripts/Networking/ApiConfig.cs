using UnityEngine;

namespace ChemLab.Networking
{
    /// <summary>
    /// 后端API配置（默认使用本机 Node/Express 服务，由服务端连接 MySQL:3306）。
    /// </summary>
    [CreateAssetMenu(fileName = "ApiConfig", menuName = "ChemLab/ApiConfig")]
    public class ApiConfig : ScriptableObject
    {
        [Header("Base URL")]
        public string baseUrl = "http://localhost:3000";

        [Header("Request")]
        [Tooltip("请求超时时间（秒）")]
        public int timeoutSeconds = 10;
    }
}

