using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace ChemLab.Labs
{
    public class LabSceneLoader : MonoBehaviour
    {
        [Header("=== Manifest（建议放到 Resources/LabManifest.asset）===")]
        public LabManifest manifest;

        private void Awake()
        {
            if (manifest == null)
            {
                manifest = Resources.Load<LabManifest>("LabManifest");
            }
        }

        public void LoadLabByName(string labName, Action<float, string> onProgress = null, Action<bool, string> onDone = null)
        {
            StartCoroutine(LoadLabCoroutine(labName, onProgress, onDone));
        }

        private IEnumerator LoadLabCoroutine(string labName, Action<float, string> onProgress, Action<bool, string> onDone)
        {
            if (manifest == null)
            {
                onDone?.Invoke(false, "未找到 LabManifest（请放置 Resources/LabManifest.asset 或在 Inspector 绑定）");
                yield break;
            }

            var entry = manifest.FindByName(labName);
            if (entry == null)
            {
                onDone?.Invoke(false, $"清单中不存在实验：{labName}");
                yield break;
            }

            if (string.IsNullOrWhiteSpace(entry.bundleFileName) && !string.IsNullOrWhiteSpace(entry.bundleName))
                entry.bundleFileName = entry.bundleName;

            if (string.IsNullOrWhiteSpace(manifest.baseUrl))
            {
                onDone?.Invoke(false, "Manifest.baseUrl 为空，无法下载 AssetBundle");
                yield break;
            }

            if (string.IsNullOrWhiteSpace(entry.bundleFileName))
            {
                onDone?.Invoke(false, $"实验 {labName} 未配置 bundleFileName（请先在编辑器生成清单并构建 AB）");
                yield break;
            }

            string url = CombineUrl(manifest.baseUrl, entry.bundleFileName);
            if (manifest.verboseLog) Debug.Log($"[LabSceneLoader] 下载 AB: {url}");

            onProgress?.Invoke(0f, "下载资源中...");

            // WebGL 支持缓存（若服务器正确设置缓存头，会更友好）
            using (var req = UnityWebRequestAssetBundle.GetAssetBundle(url))
            {
                var op = req.SendWebRequest();
                while (!op.isDone)
                {
                    onProgress?.Invoke(Mathf.Clamp01(op.progress * 0.9f), "下载资源中...");
                    yield return null;
                }

                if (req.result != UnityWebRequest.Result.Success)
                {
                    onDone?.Invoke(false, "下载失败：" + req.error);
                    yield break;
                }

                var bundle = DownloadHandlerAssetBundle.GetContent(req);
                if (bundle == null)
                {
                    onDone?.Invoke(false, "下载成功但解析 AssetBundle 失败");
                    yield break;
                }

                try
                {
                    string[] scenes = bundle.GetAllScenePaths();
                    if (scenes == null || scenes.Length == 0)
                    {
                        onDone?.Invoke(false, $"AB 中未包含场景（bundle={entry.bundleFileName}）");
                        yield break;
                    }

                    // 约定：每个实验 AB 只放 1 个场景；若多个，取第一个
                    string scenePathInBundle = scenes[0];
                    if (manifest.verboseLog) Debug.Log($"[LabSceneLoader] 准备加载场景：{scenePathInBundle}");

                    onProgress?.Invoke(0.95f, "加载场景中...");
                    var loadOp = SceneManager.LoadSceneAsync(scenePathInBundle, LoadSceneMode.Single);
                    while (!loadOp.isDone)
                    {
                        // loadOp.progress 最大通常到 0.9（激活前），这里做一个平滑映射
                        float p = 0.95f + Mathf.Clamp01(loadOp.progress / 0.9f) * 0.05f;
                        onProgress?.Invoke(p, "加载场景中...");
                        yield return null;
                    }

                    onProgress?.Invoke(1f, "完成");
                    onDone?.Invoke(true, "");
                }
                finally
                {
                    // 重要：不要 Unload(true) 否则场景资源可能丢失
                    bundle.Unload(false);
                }
            }
        }

        private static string CombineUrl(string baseUrl, string fileName)
        {
            baseUrl = (baseUrl ?? "").Trim();
            fileName = (fileName ?? "").Trim();
            if (baseUrl.EndsWith("/")) baseUrl = baseUrl.Substring(0, baseUrl.Length - 1);
            if (fileName.StartsWith("/")) fileName = fileName.Substring(1);
            return $"{baseUrl}/{fileName}";
        }
    }
}

