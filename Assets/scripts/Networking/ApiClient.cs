using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ChemLab.Networking
{
    /// <summary>
    /// 极简HTTP客户端：自动携带/保存 session cookie（express-session）。
    /// </summary>
    public class ApiClient : MonoBehaviour
    {
        public static ApiClient Instance { get; private set; }

        [SerializeField] private ApiConfig config;

        private string _cookie; // connect.sid=...

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetConfig(ApiConfig apiConfig) => config = apiConfig;

        public string BaseUrl => config != null ? config.baseUrl : "http://localhost:3000";

        public IEnumerator Get(string path, Action<long, string> onDone)
        {
            yield return Send(UnityWebRequest.kHttpVerbGET, path, null, onDone);
        }

        public IEnumerator PostJson(string path, string jsonBody, Action<long, string> onDone)
        {
            yield return Send(UnityWebRequest.kHttpVerbPOST, path, jsonBody, onDone);
        }

        public IEnumerator PutJson(string path, string jsonBody, Action<long, string> onDone)
        {
            yield return Send(UnityWebRequest.kHttpVerbPUT, path, jsonBody, onDone);
        }

        public IEnumerator Delete(string path, Action<long, string> onDone)
        {
            yield return Send(UnityWebRequest.kHttpVerbDELETE, path, null, onDone);
        }

        private IEnumerator Send(string method, string path, string jsonBody, Action<long, string> onDone)
        {
            string url = CombineUrl(BaseUrl, path);

            UnityWebRequest req;
            if (method == UnityWebRequest.kHttpVerbGET || method == UnityWebRequest.kHttpVerbDELETE)
            {
                req = new UnityWebRequest(url, method);
                req.downloadHandler = new DownloadHandlerBuffer();
            }
            else
            {
                byte[] bodyRaw = string.IsNullOrEmpty(jsonBody) ? Array.Empty<byte>() : Encoding.UTF8.GetBytes(jsonBody);
                req = new UnityWebRequest(url, method);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
            }

            if (!string.IsNullOrEmpty(_cookie))
                req.SetRequestHeader("Cookie", _cookie);

            int timeout = config != null ? Mathf.Max(1, config.timeoutSeconds) : 10;
            req.timeout = timeout;

            yield return req.SendWebRequest();

            string setCookie = req.GetResponseHeader("Set-Cookie");
            if (!string.IsNullOrEmpty(setCookie))
            {
                int semi = setCookie.IndexOf(';');
                _cookie = semi > 0 ? setCookie.Substring(0, semi) : setCookie;
            }

            long code = req.responseCode;
            string text = req.downloadHandler != null ? req.downloadHandler.text : "";

            if (req.result != UnityWebRequest.Result.Success && code == 0)
                text = string.IsNullOrEmpty(req.error) ? "Network error" : req.error;

            onDone?.Invoke(code, text);
        }

        private static string CombineUrl(string baseUrl, string path)
        {
            if (string.IsNullOrEmpty(baseUrl)) return path ?? "";
            if (string.IsNullOrEmpty(path)) return baseUrl;
            if (baseUrl.EndsWith("/")) baseUrl = baseUrl.TrimEnd('/');
            if (!path.StartsWith("/")) path = "/" + path;
            return baseUrl + path;
        }
    }
}

