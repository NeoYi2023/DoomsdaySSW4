using System;

/// <summary>
/// 船只配置数据结构（从JSON配置表加载）
/// </summary>
[Serializable]
public class ShipConfig
{
    public string shipId;
    public string shipName;
    public int initialDebt;        // 初始债务
    public string description;
}

/// <summary>
/// 船只配置集合，便于从JSON反序列化
/// </summary>
[Serializable]
public class ShipConfigCollection
{
    public ShipConfig[] ships;
}
