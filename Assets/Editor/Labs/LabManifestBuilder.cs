using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ChemLab.Labs;
using UnityEditor;
using UnityEngine;

namespace ChemLab.EditorTools.Labs
{
    public static class LabManifestBuilder
    {
        private const string DefaultLabsRoot = "Assets/Lab";
        private const string DefaultManifestResourcesPath = "Assets/Resources/LabManifest.asset";

        [MenuItem("ChemLab/Labs/Generate LabManifest (scan Assets/Lab)")]
        public static void GenerateManifestOnly()
        {
            var manifest = LoadOrCreateManifestAsset(DefaultManifestResourcesPath);
            manifest.labsRootFolder = DefaultLabsRoot;

            ScanAndFill(manifest, setAssetBundleName: false);
            EditorUtility.SetDirty(manifest);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[LabManifestBuilder] 已生成/更新清单：{DefaultManifestResourcesPath}（实验数：{manifest.labs.Count}）");
            Selection.activeObject = manifest;
        }

        [MenuItem("ChemLab/Labs/Auto Configure AB Names (labs scenes)")]
        public static void AutoConfigureAssetBundleNames()
        {
            var manifest = LoadOrCreateManifestAsset(DefaultManifestResourcesPath);
            if (string.IsNullOrWhiteSpace(manifest.labsRootFolder))
                manifest.labsRootFolder = DefaultLabsRoot;

            ScanAndFill(manifest, setAssetBundleName: true);
            EditorUtility.SetDirty(manifest);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[LabManifestBuilder] 已自动配置 AB 名称并更新清单（实验数：{manifest.labs.Count}）");
            Selection.activeObject = manifest;
        }

        [MenuItem("ChemLab/Labs/Build AssetBundles (WebGL) + Update Manifest")]
        public static void BuildWebGLBundlesAndUpdateManifest()
        {
            var manifest = LoadOrCreateManifestAsset(DefaultManifestResourcesPath);
            if (string.IsNullOrWhiteSpace(manifest.labsRootFolder))
                manifest.labsRootFolder = DefaultLabsRoot;

            ScanAndFill(manifest, setAssetBundleName: true);

            // 输出目录（工程外也可以，但这里放工程内便于查看；你上传时取该目录内容）
            string outDir = Path.Combine("Build", "LabAssetBundles", "WebGL");
            Directory.CreateDirectory(outDir);

            // 构建
            var buildManifest = BuildPipeline.BuildAssetBundles(
                outDir,
                BuildAssetBundleOptions.ChunkBasedCompression,
                BuildTarget.WebGL
            );

            if (buildManifest == null)
            {
                Debug.LogError("[LabManifestBuilder] BuildAssetBundles 失败（返回 null）");
                return;
            }

            // 写回 hash
            var hashByBundle = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var b in buildManifest.GetAllAssetBundles())
            {
                Hash128 h = buildManifest.GetAssetBundleHash(b);
                hashByBundle[b] = h.ToString();
            }

            for (int i = 0; i < manifest.labs.Count; i++)
            {
                var e = manifest.labs[i];
                if (e == null) continue;
                if (!string.IsNullOrWhiteSpace(e.bundleName) && hashByBundle.TryGetValue(e.bundleName, out var hash))
                    e.lastBuildHash = hash;
            }

            EditorUtility.SetDirty(manifest);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[LabManifestBuilder] 构建完成：{outDir}（实验数：{manifest.labs.Count}）");
            Debug.Log("[LabManifestBuilder] 上传提示：将该目录下生成的 AB 文件上传到你的服务器/CDN，并把 Manifest.baseUrl 指向该目录的 URL。");

            Selection.activeObject = manifest;
        }

        private static LabManifest LoadOrCreateManifestAsset(string assetPath)
        {
            var manifest = AssetDatabase.LoadAssetAtPath<LabManifest>(assetPath);
            if (manifest != null) return manifest;

            // 确保目录存在
            string dir = Path.GetDirectoryName(assetPath)?.Replace("\\", "/") ?? "Assets";
            if (!AssetDatabase.IsValidFolder(dir))
            {
                // 只处理 Assets/Resources 这种一层
                string parent = "Assets";
                string folderName = dir.StartsWith("Assets/") ? dir.Substring("Assets/".Length) : dir;
                if (!AssetDatabase.IsValidFolder(dir))
                    AssetDatabase.CreateFolder(parent, folderName);
            }

            manifest = ScriptableObject.CreateInstance<LabManifest>();
            AssetDatabase.CreateAsset(manifest, assetPath);
            AssetDatabase.SaveAssets();
            return manifest;
        }

        private static void ScanAndFill(LabManifest manifest, bool setAssetBundleName)
        {
            string root = string.IsNullOrWhiteSpace(manifest.labsRootFolder) ? DefaultLabsRoot : manifest.labsRootFolder.Trim();
            if (!AssetDatabase.IsValidFolder(root))
            {
                Debug.LogError($"[LabManifestBuilder] 找不到目录：{root}（请确认是 Assets/Lab，注意大小写）");
                manifest.labs.Clear();
                return;
            }

            // 收集子文件夹
            string absRoot = Path.GetFullPath(root);
            var subDirs = Directory.GetDirectories(absRoot)
                .Select(d => d.Replace("\\", "/"))
                .ToArray();

            var newList = new List<LabEntry>();
            foreach (var absDir in subDirs)
            {
                string folderName = Path.GetFileName(absDir);
                string unityFolderPath = $"{root}/{folderName}";

                // 找场景：优先 *.unity；若多个，优先包含“场景”关键词，否则第一个
                string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { unityFolderPath });
                string scenePath = PickScenePath(sceneGuids);
                if (string.IsNullOrWhiteSpace(scenePath))
                {
                    Debug.LogWarning($"[LabManifestBuilder] 跳过（未找到场景）：{unityFolderPath}");
                    continue;
                }

                string bundleName = $"lab_{SanitizeBundleToken(folderName)}";

                if (setAssetBundleName)
                    SetSceneAssetBundleName(scenePath, bundleName);

                newList.Add(new LabEntry
                {
                    labName = folderName,
                    folderPath = unityFolderPath,
                    scenePath = scenePath,
                    bundleName = bundleName,
                    bundleFileName = bundleName,
                    lastBuildHash = ""
                });
            }

            // 稳定排序（按实验名）
            newList.Sort((a, b) => string.CompareOrdinal(a?.labName, b?.labName));
            manifest.labs = newList;
        }

        private static string PickScenePath(string[] guids)
        {
            if (guids == null || guids.Length == 0) return "";
            var paths = guids
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !string.IsNullOrWhiteSpace(p) && p.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (paths.Count == 0) return "";

            // 优先：文件名包含“场景”
            for (int i = 0; i < paths.Count; i++)
            {
                string name = Path.GetFileNameWithoutExtension(paths[i]);
                if (name != null && name.Contains("场景", StringComparison.OrdinalIgnoreCase))
                    return paths[i];
            }

            return paths[0];
        }

        private static void SetSceneAssetBundleName(string scenePath, string bundleName)
        {
            var importer = AssetImporter.GetAtPath(scenePath);
            if (importer == null)
            {
                Debug.LogWarning($"[LabManifestBuilder] 无法获取 AssetImporter：{scenePath}");
                return;
            }

            if (!string.Equals(importer.assetBundleName, bundleName, StringComparison.OrdinalIgnoreCase))
            {
                importer.assetBundleName = bundleName;
                importer.SaveAndReimport();
            }
        }

        private static string SanitizeBundleToken(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "unknown";
            s = s.Trim();
            var chars = s.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                char c = chars[i];
                bool ok = (c >= 'a' && c <= 'z') ||
                          (c >= 'A' && c <= 'Z') ||
                          (c >= '0' && c <= '9') ||
                          c == '_' || c == '-';
                if (!ok) chars[i] = '_';
            }
            return new string(chars).ToLowerInvariant();
        }
    }
}

