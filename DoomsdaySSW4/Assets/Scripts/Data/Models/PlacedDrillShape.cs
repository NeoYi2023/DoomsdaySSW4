using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 已放置在钻机平台上的造型实例
/// 记录造型的位置、旋转状态和激活的特性
/// </summary>
[Serializable]
public class PlacedDrillShape
{
    /// <summary>
    /// 实例唯一ID（用于区分同一造型的多个放置实例）
    /// </summary>
    public string instanceId;
    
    /// <summary>
    /// 引用的造型配置ID
    /// </summary>
    public string shapeId;
    
    /// <summary>
    /// 在平台上的锚点位置（左下角为0,0）
    /// </summary>
    public Vector2Int position;
    
    /// <summary>
    /// 旋转状态（0/90/180/270度）
    /// </summary>
    public int rotation;
    
    /// <summary>
    /// 已激活的特性ID列表
    /// </summary>
    public List<string> activeTraits = new List<string>();
    
    /// <summary>
    /// 创建新的放置实例
    /// </summary>
    public PlacedDrillShape()
    {
        instanceId = Guid.NewGuid().ToString();
    }
    
    /// <summary>
    /// 创建新的放置实例
    /// </summary>
    /// <param name="shapeId">造型配置ID</param>
    /// <param name="position">放置位置</param>
    /// <param name="rotation">旋转角度</param>
    public PlacedDrillShape(string shapeId, Vector2Int position, int rotation = 0)
    {
        this.instanceId = Guid.NewGuid().ToString();
        this.shapeId = shapeId;
        this.position = position;
        this.rotation = rotation;
        this.activeTraits = new List<string>();
    }
    
    /// <summary>
    /// 获取该实例在平台上占用的所有格子坐标（应用旋转和位置偏移后）
    /// </summary>
    /// <param name="shapeConfig">造型配置</param>
    /// <returns>占用的格子坐标列表</returns>
    public List<Vector2Int> GetOccupiedCells(DrillShapeConfig shapeConfig)
    {
        if (shapeConfig == null) return new List<Vector2Int>();
        
        List<Vector2Int> rotatedCells = shapeConfig.GetRotatedCells(rotation);
        List<Vector2Int> occupiedCells = new List<Vector2Int>();
        
        foreach (var cell in rotatedCells)
        {
            occupiedCells.Add(position + cell);
        }
        
        return occupiedCells;
    }
    
    /// <summary>
    /// 旋转造型（顺时针90度）
    /// </summary>
    public void RotateClockwise()
    {
        rotation = (rotation + 90) % 360;
    }
    
    /// <summary>
    /// 旋转造型（逆时针90度）
    /// </summary>
    public void RotateCounterClockwise()
    {
        rotation = (rotation + 270) % 360;
    }
    
    /// <summary>
    /// 克隆实例（用于预览等场景）
    /// </summary>
    public PlacedDrillShape Clone()
    {
        return new PlacedDrillShape
        {
            instanceId = this.instanceId,
            shapeId = this.shapeId,
            position = this.position,
            rotation = this.rotation,
            activeTraits = new List<string>(this.activeTraits)
        };
    }
}
