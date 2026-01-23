using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 钻头配置（从配置表加载）
/// </summary>
[Serializable]
public class DrillBitConfig
{
    /// <summary>
    /// 钻头唯一ID
    /// </summary>
    public string bitId;
    
    /// <summary>
    /// 钻头名称
    /// </summary>
    public string bitName;
    
    /// <summary>
    /// 描述
    /// </summary>
    public string description;
    
    /// <summary>
    /// 需要的插槽类型（1格或4格）
    /// </summary>
    public DrillSlotType requiredSlotType;
    
    /// <summary>
    /// 钻探强度加成（固定值）
    /// </summary>
    public int strengthBonus;
    
    /// <summary>
    /// 钻探强度倍率（如1.2表示+20%）
    /// </summary>
    public float strengthMultiplier = 1f;
    
    /// <summary>
    /// 影响范围（格子数，如1表示相邻格子）
    /// </summary>
    public int effectRange = 1;
    
    /// <summary>
    /// 是否包括斜角（对角线方向）
    /// </summary>
    public bool includeDiagonal = false;
    
    /// <summary>
    /// 后效列表（如爆炸、连锁等）
    /// </summary>
    public List<DrillBitEffect> effects = new List<DrillBitEffect>();
    
    /// <summary>
    /// 图标路径（相对于Resources目录）
    /// </summary>
    public string iconPath;
}

/// <summary>
/// 钻头后效配置
/// </summary>
[Serializable]
public class DrillBitEffect
{
    /// <summary>
    /// 效果类型
    /// </summary>
    public DrillBitEffectType effectType;
    
    /// <summary>
    /// 效果数值（如爆炸伤害值）
    /// </summary>
    public int value;
    
    /// <summary>
    /// 影响范围（格子数）
    /// </summary>
    public int range;
    
    /// <summary>
    /// 效果描述
    /// </summary>
    public string description;
}

/// <summary>
/// 钻头后效类型枚举
/// </summary>
public enum DrillBitEffectType
{
    Explosion,           // 爆炸：挖掉矿石时对周围造成伤害
    ChainReaction,       // 连锁反应
    AreaBoost,           // 区域加成
    // 可扩展其他效果
}

/// <summary>
/// 钻头配置集合（用于JSON反序列化）
/// </summary>
[Serializable]
public class DrillBitConfigCollection
{
    public List<DrillBitConfig> bits = new List<DrillBitConfig>();
}
