using System;
using System.Collections.Generic;

/// <summary>
/// 能源升级配置数据结构（从JSON配置表加载）
/// </summary>
[Serializable]
public class EnergyUpgradeConfig
{
    public string upgradeId;          // 升级ID
    public string type;               // 升级类型（字符串，需要转换为UpgradeOptionType）
    public string name;               // 升级名称
    public string description;        // 升级描述
    public int value;                // 升级数值
    public int weight;                // 权重（用于随机选择）
    public string iconPath;          // 图标路径（相对于Resources目录，如"Icons/Upgrades/drill_strength"）
}

/// <summary>
/// 能源升级配置集合，便于从JSON反序列化
/// </summary>
[Serializable]
public class EnergyUpgradeConfigCollection
{
    public EnergyUpgradeConfig[] upgrades;
}
