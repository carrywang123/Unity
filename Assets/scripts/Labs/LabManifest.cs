using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChemLab.Labs
{
    [CreateAssetMenu(fileName = "LabManifest", menuName = "ChemLab/Labs/Lab Manifest")]
    public class LabManifest : ScriptableObject
    {
        [Header("=== Lab 根目录 ===")]
        [Tooltip("默认：Assets/Lab（注意大小写）")]
        public string labsRootFolder = "Assets/Lab";

        [Header("=== 运行时下载配置 ===")]
        [Tooltip("WebGL 发布后 AB 下载根地址，例如：https://your.cdn.com/chemlab/ab/webgl")]
        public string baseUrl = "";

        [Tooltip("是否在运行时打印下载/加载日志")]
        public bool verboseLog = true;

        [Header("=== 实验列表 ===")]
        public List<LabEntry> labs = new List<LabEntry>();

        public LabEntry FindByName(string labName)
        {
            if (string.IsNullOrWhiteSpace(labName) || labs == null) return null;
            for (int i = 0; i < labs.Count; i++)
            {
                var e = labs[i];
                if (e != null && string.Equals(e.labName, labName, StringComparison.OrdinalIgnoreCase))
                    return e;
            }
            return null;
        }
    }

    [Serializable]
    public class LabEntry
    {
        [Tooltip("实验名称（默认=文件夹名）")]
        public string labName;

        [Tooltip("实验文件夹（Unity 工程路径，例如 Assets/Lab/2）")]
        public string folderPath;

        [Tooltip("实验场景路径（Unity 工程路径，例如 Assets/Lab/2/实验场景.unity）")]
        public string scenePath;

        [Header("=== AssetBundle（自动生成） ===")]
        [Tooltip("用于构建/下载的 AssetBundle 名称（不含扩展名）")]
        public string bundleName;

        [Tooltip("构建输出的文件名（通常等于 bundleName；可用于拼接URL）")]
        public string bundleFileName;

        [Tooltip("上一次构建时记录的 Hash（可用于缓存/更新判断）")]
        public string lastBuildHash;
    }
}

