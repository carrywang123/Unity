using System;

namespace ChemLab.Models
{
    [Serializable]
    public class ExperimentModel
    {
        public string experimentId;        // 实验ID
        public string experimentName;      // 实验名称
        public string experimentDescription; // 实验描述
        public string experimentImage;     // 实验图片（资源路径/URL/文件路径，按项目约定存字符串）

        public ExperimentModel()
        {
        }

        public ExperimentModel(string experimentId, string experimentName, string experimentDescription, string experimentImage)
        {
            this.experimentId = experimentId;
            this.experimentName = experimentName;
            this.experimentDescription = experimentDescription;
            this.experimentImage = experimentImage;
        }
    }
}

