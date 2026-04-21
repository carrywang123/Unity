// ExperimentMode.cs
public static class ExperimentMode
{
    // 定义两种模式
    public enum Mode
    {
        Training,   // 训练模式
        Scoring     // 评分模式
    }

    // 当前模式，默认训练模式
    public static Mode CurrentMode = Mode.Training;
}
