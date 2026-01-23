using System;
using System.Collections.Generic;

/// <summary>
/// 能源系统数据（对应 SPEC 3.18 EnergyData / EnergyUpgrade）
/// </summary>
[Serializable]
public class EnergyData
{
    // 能源值
    public int currentEnergy;                  // 当前能源值
    public int totalEnergyCollected;           // 累计收集的能源值

    // 能源阈值（来自配置）
    public List<int> energyThresholds = new List<int>(); // 能源阈值列表（如50,100,150）
    public int nextThresholdIndex;             // 下一个阈值索引

    // 已获得的升级
    public List<EnergyUpgrade> upgrades = new List<EnergyUpgrade>();

    /// <summary>
    /// 当前是否可以升级（达到阈值）
    /// </summary>
    public bool CanUpgrade
    {
        get
        {
            if (energyThresholds == null || energyThresholds.Count == 0) return false;
            if (nextThresholdIndex < 0 || nextThresholdIndex >= energyThresholds.Count) return false;
            return currentEnergy >= energyThresholds[nextThresholdIndex];
        }
    }
}

[Serializable]
public class EnergyUpgrade
{
    public string upgradeId;                  // 升级ID
    public UpgradeOptionType optionType;      // 升级选项类型
    public int value;                         // 数值
    public long timestamp;                    // 获得时间戳
}

public enum UpgradeOptionType
{
    DrillStrength,      // 挖掘强度提升
    MiningEfficiency,   // 挖掘效率提升
    OreDiscovery,       // 矿石发现能力提升
    OreValueBoost,      // 矿石价值提升
    DrillShapeUnlock,   // 解锁钻头造型（将指定造型加入当前关卡可用库存）
    DrillPlatformUpgrade,  // 钻机平台升级（提升基础强度或增加插槽）
    DrillBitUnlock      // 解锁新钻头
}

