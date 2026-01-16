using System;

/// <summary>
/// 能源阈值配置数据结构（从JSON配置表加载）
/// </summary>
[Serializable]
public class EnergyThresholdConfig
{
    public string shipId;              // 船只ID（关联ShipConfig）
    public int[] energyThresholds;     // 能源阈值数组（如 [50, 100, 150]）
}

/// <summary>
/// 能源阈值配置集合，便于从JSON反序列化
/// </summary>
[Serializable]
public class EnergyThresholdConfigCollection
{
    public EnergyThresholdConfig[] thresholds;
}
