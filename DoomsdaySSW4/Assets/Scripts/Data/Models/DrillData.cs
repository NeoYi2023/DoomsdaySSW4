using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 钻头数据（对应 SPEC 3.11 DrillData）
/// 支持造型系统：通过platformData管理放置在9x9平台上的钻头造型
/// </summary>
[Serializable]
public class DrillData
{
    // 钻头基本信息
    public string drillId;                       // 钻头唯一标识
    public string drillName;                     // 名称
    public DrillType drillType;                  // 类型（默认 / 升级 / 失落装备等）

    // 钻机平台数据（造型系统）
    public DrillPlatformData platformData = new DrillPlatformData();

    // [已弃用] 以下字段保留用于兼容性，新代码应使用platformData
    [Obsolete("使用 platformData 替代")]
    public int miningStrength;                   // 挖掘强度（攻击值）
    [Obsolete("使用 platformData 替代")]
    public Vector2Int miningRange = new Vector2Int(5, 5); // 挖掘范围（长宽格子数，默认5x5）
    [Obsolete("使用 platformData 替代")]
    public Vector2Int drillCenter = new Vector2Int(4, 4); // 默认在9x9地图中心

    // 额外属性（用于挖掘特殊矿石）
    public Dictionary<string, int> additionalAttributes =
        new Dictionary<string, int>();

    // 本关内升级
    public int currentLevel;                     // 当前等级（本关内）
    public List<DrillUpgrade> upgrades = new List<DrillUpgrade>();

    // 永久属性（由信用积分增强）
    public int permanentStrengthBonus;           // 永久强度加成
    public float permanentAttackMultiplier = 1f; // 永久攻击倍率加成
    [Obsolete("使用 permanentAttackMultiplier 替代")]
    public Vector2Int permanentRangeBonus;       // 永久范围加成（已弃用）

    /// <summary>
    /// 获取当前实际挖掘强度（包含永久加成）
    /// 注意：此方法已弃用，新代码应使用DrillAttackCalculator计算每个格子的攻击强度
    /// </summary>
    [Obsolete("使用 DrillAttackCalculator.CalculateAttackStrength 替代")]
    public int GetEffectiveStrength()
    {
        #pragma warning disable 612, 618
        return miningStrength + permanentStrengthBonus;
        #pragma warning restore 612, 618
    }

    /// <summary>
    /// 获取当前实际挖掘范围（包含永久加成）
    /// 注意：此方法已弃用，新代码应使用platformData获取攻击范围
    /// </summary>
    [Obsolete("使用 platformData.GetAllOccupiedCells 替代")]
    public Vector2Int GetEffectiveRange()
    {
        #pragma warning disable 612, 618
        return miningRange + permanentRangeBonus;
        #pragma warning restore 612, 618
    }
    
    /// <summary>
    /// 检查是否使用造型系统
    /// </summary>
    public bool UsesShapeSystem()
    {
        return platformData != null && platformData.placedShapes.Count > 0;
    }
}

public enum DrillType
{
    Default,      // 默认钻头
    Upgraded,     // 升级后钻头
    LostEquipment // 失落的装备
}

[Serializable]
public class DrillUpgrade
{
    public string upgradeId;         // 升级ID
    public UpgradeType type;         // 升级类型
    public int value;                // 升级数值
    public string description;       // 升级描述
    public bool isPermanent;         // 是否永久（本游戏中一般为false，仅本关生效）
}

public enum UpgradeType
{
    StrengthBoost,    // 强度提升
    RangeBoost,       // 范围提升（已弃用，改用NewShape）
    NewShape,         // 获得新造型
    MiningEfficiency, // 挖掘效率提升
    EnergyBonus       // 能源加成
}

