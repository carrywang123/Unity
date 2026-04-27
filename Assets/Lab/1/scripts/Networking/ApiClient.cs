using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace game_1
{
    public sealed class ApiClient
    {
        private readonly string _baseUrl;

        public ApiClient(string baseUrl)
        {
            _baseUrl = (baseUrl ?? "").Trim().TrimEnd('/');
        }

        public IEnumerator Get(string path, Action<bool, string, string> onDone)
        {
            string url = _baseUrl + path;
            using (var req = UnityWebRequest.Get(url))
            {
                req.SetRequestHeader("Content-Type", "application/json");
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    onDone?.Invoke(false, req.error, null);
                    yield break;
                }
                onDone?.Invoke(true, "", req.downloadHandler != null ? req.downloadHandler.text : "");
            }
        }

        public IEnumerator PostJson(string path, string jsonBody, Action<bool, string, string> onDone)
        {
            string url = _baseUrl + path;
            byte[] body = System.Text.Encoding.UTF8.GetBytes(jsonBody ?? "{}");
            using (var req = new UnityWebRequest(url, "POST"))
            {
                req.uploadHandler = new UploadHandlerRaw(body);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
                yield return req.SendWebRequest();
                if (req.result != UnityWebRequest.Result.Success)
                {
                    onDone?.Invoke(false, req.error, null);
                    yield break;
                }
                onDone?.Invoke(true, "", req.downloadHandler != null ? req.downloadHandler.text : "");
            }
        }
    }
}

