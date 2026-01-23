using System;
using UnityEngine;

/// <summary>
/// 已插入的钻头实例
/// </summary>
[Serializable]
public class PlacedDrillBit
{
    /// <summary>
    /// 钻头配置ID
    /// </summary>
    public string bitId;
    
    /// <summary>
    /// 插入的插槽ID
    /// </summary>
    public string slotId;
    
    /// <summary>
    /// 在平台上的位置（与插槽位置相同）
    /// </summary>
    public Vector2Int platformPosition;
    
    /// <summary>
    /// 实例唯一ID
    /// </summary>
    public string instanceId;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public PlacedDrillBit()
    {
        instanceId = System.Guid.NewGuid().ToString();
    }
    
    /// <summary>
    /// 克隆钻头实例
    /// </summary>
    public PlacedDrillBit Clone()
    {
        return new PlacedDrillBit
        {
            bitId = this.bitId,
            slotId = this.slotId,
            platformPosition = this.platformPosition,
            instanceId = this.instanceId
        };
    }
}
