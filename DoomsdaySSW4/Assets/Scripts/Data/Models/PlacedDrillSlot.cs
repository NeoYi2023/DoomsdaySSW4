using System;
using UnityEngine;

/// <summary>
/// 已放置的插槽实例（在平台上）
/// </summary>
[Serializable]
public class PlacedDrillSlot
{
    /// <summary>
    /// 插槽唯一ID
    /// </summary>
    public string slotId;
    
    /// <summary>
    /// 在9x9平台上的绝对位置
    /// </summary>
    public Vector2Int platformPosition;
    
    /// <summary>
    /// 插槽类型
    /// </summary>
    public DrillSlotType slotType;
    
    /// <summary>
    /// 插入的钻头ID（如果为空则表示未插入）
    /// </summary>
    public string insertedBitId;
    
    /// <summary>
    /// 所属造型实例ID
    /// </summary>
    public string shapeInstanceId;
    
    /// <summary>
    /// 检查插槽是否已插入钻头
    /// </summary>
    public bool IsOccupied => !string.IsNullOrEmpty(insertedBitId);
    
    /// <summary>
    /// 克隆插槽实例
    /// </summary>
    public PlacedDrillSlot Clone()
    {
        return new PlacedDrillSlot
        {
            slotId = this.slotId,
            platformPosition = this.platformPosition,
            slotType = this.slotType,
            insertedBitId = this.insertedBitId,
            shapeInstanceId = this.shapeInstanceId
        };
    }
}
