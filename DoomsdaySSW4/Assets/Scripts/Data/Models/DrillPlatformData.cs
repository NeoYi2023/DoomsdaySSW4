using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 钻机平台数据
/// 管理9x9平台上所有已放置的造型和可用的造型库存
/// </summary>
[Serializable]
public class DrillPlatformData
{
    /// <summary>
    /// 平台尺寸（9x9）
    /// </summary>
    public const int PLATFORM_SIZE = 9;
    
    /// <summary>
    /// 已放置的造型列表
    /// </summary>
    public List<PlacedDrillShape> placedShapes = new List<PlacedDrillShape>();
    
    /// <summary>
    /// 可用的造型ID列表（库存，未放置在平台上的造型）
    /// </summary>
    public List<string> availableShapeIds = new List<string>();
    
    /// <summary>
    /// 获取平台上所有被占用的格子坐标
    /// </summary>
    /// <param name="getShapeConfig">获取造型配置的委托</param>
    /// <returns>被占用的格子坐标集合</returns>
    public HashSet<Vector2Int> GetAllOccupiedCells(Func<string, DrillShapeConfig> getShapeConfig)
    {
        HashSet<Vector2Int> occupiedCells = new HashSet<Vector2Int>();
        
        foreach (var placedShape in placedShapes)
        {
            DrillShapeConfig config = getShapeConfig?.Invoke(placedShape.shapeId);
            if (config != null)
            {
                List<Vector2Int> cells = placedShape.GetOccupiedCells(config);
                foreach (var cell in cells)
                {
                    occupiedCells.Add(cell);
                }
            }
        }
        
        return occupiedCells;
    }
    
    /// <summary>
    /// 检查指定位置是否在平台边界内
    /// </summary>
    public static bool IsWithinBounds(Vector2Int position)
    {
        return position.x >= 0 && position.x < PLATFORM_SIZE &&
               position.y >= 0 && position.y < PLATFORM_SIZE;
    }
    
    /// <summary>
    /// 检查一组格子是否都在平台边界内
    /// </summary>
    public static bool AreAllWithinBounds(List<Vector2Int> cells)
    {
        foreach (var cell in cells)
        {
            if (!IsWithinBounds(cell))
                return false;
        }
        return true;
    }
    
    /// <summary>
    /// 根据实例ID查找已放置的造型
    /// </summary>
    public PlacedDrillShape FindPlacedShapeByInstanceId(string instanceId)
    {
        return placedShapes.Find(s => s.instanceId == instanceId);
    }
    
    /// <summary>
    /// 根据格子坐标查找该位置上的造型
    /// </summary>
    /// <param name="position">格子坐标</param>
    /// <param name="getShapeConfig">获取造型配置的委托</param>
    /// <returns>该位置上的造型，如果没有则返回null</returns>
    public PlacedDrillShape FindShapeAtPosition(Vector2Int position, Func<string, DrillShapeConfig> getShapeConfig)
    {
        foreach (var placedShape in placedShapes)
        {
            DrillShapeConfig config = getShapeConfig?.Invoke(placedShape.shapeId);
            if (config != null)
            {
                List<Vector2Int> cells = placedShape.GetOccupiedCells(config);
                if (cells.Contains(position))
                {
                    return placedShape;
                }
            }
        }
        return null;
    }
    
    /// <summary>
    /// 清空平台上的所有造型（将它们移回库存）
    /// </summary>
    public void ClearPlatform()
    {
        foreach (var shape in placedShapes)
        {
            if (!availableShapeIds.Contains(shape.shapeId))
            {
                availableShapeIds.Add(shape.shapeId);
            }
        }
        placedShapes.Clear();
    }
    
    /// <summary>
    /// 克隆平台数据（用于预览等场景）
    /// </summary>
    public DrillPlatformData Clone()
    {
        DrillPlatformData clone = new DrillPlatformData();
        clone.availableShapeIds = new List<string>(this.availableShapeIds);
        
        foreach (var shape in this.placedShapes)
        {
            clone.placedShapes.Add(shape.Clone());
        }
        
        return clone;
    }
}
