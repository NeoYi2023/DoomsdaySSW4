using System;
using UnityEngine;

/// <summary>
/// 钻头配置数据结构（从JSON配置表加载）
/// </summary>
[Serializable]
public class DrillConfig
{
    public string drillId;
    public string drillName;
    public int miningStrength;      // 基础挖掘强度（攻击值）
    public int miningRangeX;        // 基础挖掘范围（长）
    public int miningRangeY;        // 基础挖掘范围（宽）
    public string description;
}

/// <summary>
/// 钻头配置集合，便于从JSON反序列化
/// </summary>
[Serializable]
public class DrillConfigCollection
{
    public DrillConfig[] drills;
}
