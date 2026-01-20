using System;
using UnityEngine;

/// <summary>
/// 单个矿石的数据（对应 SPEC 3.17 OreData / OreConfig）
/// </summary>
[Serializable]
public class OreData
{
    // 矿石基本信息
    public string oreId;                      // 矿石ID
    public string oreName;                    // 名称
    public OreType oreType;                   // 类型
    public MineralType mineralType;           // 对应挖矿网格中的矿物类型（用于统计）

    // 血量系统（攻击值/血量机制）
    public int maxHardness;                   // 最大硬度（初始血量）
    public int currentHardness;               // 当前硬度

    // 额外属性要求（如需要特定钻头属性）
    // 简化实现时可以忽略或用字符串标记
    public string requiredAttributeKey;
    public int requiredAttributeValue;

    // 价值与能源
    public int value;                         // 金钱价值
    public bool isEnergyOre;                  // 是否为能源矿石
    public int energyValue;                   // 能源值

    // 视觉相关
    public string spritePath;                 // 矿石图标路径（飞行动画用）
    public string latticeSpritePath;          // 矿石格子路径（地图显示用）

    // 位置信息
    public Vector2Int position;               // 在地图中的坐标
    public int depth;                         // 所在层数
    public bool isMined;                      // 是否已被挖掉
}

public enum OreType
{
    Common,      // 普通矿石
    Rare,        // 稀有矿石
    Energy,      // 能源矿石
    Special      // 特殊矿石（需要额外属性）
}

/// <summary>
/// 矿石静态配置（用于JSON配置）
/// </summary>
[Serializable]
public class OreConfig
{
    public string oreId;
    public string oreName;
    public OreType oreType;
    public int hardness;                      // 初始硬度（血量）
    public string requiredAttributeKey;
    public int requiredAttributeValue;
    public int value;
    public bool isEnergyOre;
    public int energyValue;
    public int minDepth;
    public int maxDepth;
    public float spawnProbability;            // 出现概率（权重化前可选）
    public string spritePath;                 // 矿石图标路径（飞行动画用）
    public string latticeSpritePath;          // 矿石格子路径（地图显示用）
}

/// <summary>
/// 矿石配置集合，便于从JSON反序列化
/// </summary>
[Serializable]
public class OreConfigCollection
{
    public OreConfig[] ores;
}

