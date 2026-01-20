using System;
using System.Collections.Generic;

/// <summary>
/// 矿石生成配置（从JSON配置表加载）
/// </summary>
[Serializable]
public class OreSpawnConfig
{
    public int layerDepth;            // 层数（深度）
    public List<OreSpawnRule> spawnRules; // 该层的矿石生成规则
}

/// <summary>
/// 矿石生成规则
/// </summary>
[Serializable]
public class OreSpawnRule
{
    public string oreId;              // 矿石ID（来自矿石配置表）
    public int weight;                // 出现权重
    public int maxCount;              // 最大出现数量
    public float spawnProbability;    // 生成概率（可选，基于权重计算）
    public int @default;              // 是否为该层的默认矿石（1=默认，0=非默认）
}

/// <summary>
/// 矿石生成配置集合，便于从JSON反序列化
/// </summary>
[Serializable]
public class OreSpawnConfigCollection
{
    public OreSpawnConfig[] layers;
}
