using System;
using UnityEngine;

/// <summary>
/// 钻头插槽配置（在造型配置中定义）
/// </summary>
[Serializable]
public class DrillSlotConfig
{
    /// <summary>
    /// 插槽在造型中的相对位置（相对于锚点(0,0)）
    /// </summary>
    public Vector2Int position;
    
    /// <summary>
    /// 插槽类型（1格、4格等）
    /// </summary>
    public DrillSlotType slotType;
    
    /// <summary>
    /// 插槽唯一ID（可选，如果为空则自动生成）
    /// </summary>
    public string slotId;
}

/// <summary>
/// 插槽类型枚举
/// </summary>
public enum DrillSlotType
{
    Single,      // 1格插槽
    Quad,        // 4格插槽（2x2）
    // 可扩展其他类型
}
