using System;

/// <summary>
/// 硬度颜色配置（从JSON配置表加载）
/// </summary>
[Serializable]
public class TileHardnessColorConfigCollection
{
    public TileHardnessColorThreshold[] thresholds;
}

/// <summary>
/// 硬度区间颜色阈值
/// </summary>
[Serializable]
public class TileHardnessColorThreshold
{
    public int maxHardness; // 硬度上限（含）
    public string colorHex; // Hex颜色（不含#，如E3C176）
}
