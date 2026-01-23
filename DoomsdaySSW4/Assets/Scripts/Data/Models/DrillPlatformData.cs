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
    /// 已放置的插槽列表（从已放置的造型中提取）
    /// </summary>
    public List<PlacedDrillSlot> placedSlots = new List<PlacedDrillSlot>();
    
    /// <summary>
    /// 已插入的钻头列表
    /// </summary>
    public List<PlacedDrillBit> insertedBits = new List<PlacedDrillBit>();
    
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
    /// 根据插槽ID查找插槽
    /// </summary>
    public PlacedDrillSlot FindSlotById(string slotId)
    {
        return placedSlots.Find(s => s.slotId == slotId);
    }
    
    /// <summary>
    /// 根据位置查找插槽
    /// </summary>
    public PlacedDrillSlot FindSlotAtPosition(Vector2Int position)
    {
        return placedSlots.Find(s => s.platformPosition == position);
    }
    
    /// <summary>
    /// 根据钻头实例ID查找钻头
    /// </summary>
    public PlacedDrillBit FindBitByInstanceId(string instanceId)
    {
        return insertedBits.Find(b => b.instanceId == instanceId);
    }
    
    /// <summary>
    /// 根据插槽ID查找插入的钻头
    /// </summary>
    public PlacedDrillBit FindBitBySlotId(string slotId)
    {
        return insertedBits.Find(b => b.slotId == slotId);
    }
    
    /// <summary>
    /// 获取影响指定格子的所有钻头
    /// </summary>
    /// <param name="position">格子位置</param>
    /// <param name="getBitConfig">获取钻头配置的委托</param>
    /// <returns>影响该格子的钻头列表</returns>
    public List<PlacedDrillBit> GetBitsAffectingCell(Vector2Int position, Func<string, DrillBitConfig> getBitConfig)
    {
        List<PlacedDrillBit> affectingBits = new List<PlacedDrillBit>();
        
        foreach (var bit in insertedBits)
        {
            DrillBitConfig config = getBitConfig?.Invoke(bit.bitId);
            if (config == null) continue;
            
            // 计算钻头影响范围
            int distance = GetManhattanDistance(bit.platformPosition, position);
            if (config.includeDiagonal)
            {
                // 包括斜角，使用切比雪夫距离
                distance = GetChebyshevDistance(bit.platformPosition, position);
            }
            
            if (distance <= config.effectRange)
            {
                affectingBits.Add(bit);
            }
        }
        
        return affectingBits;
    }
    
    /// <summary>
    /// 计算曼哈顿距离（不包括斜角）
    /// </summary>
    private int GetManhattanDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }
    
    /// <summary>
    /// 计算切比雪夫距离（包括斜角）
    /// </summary>
    private int GetChebyshevDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
    }
    
    /// <summary>
    /// 清空所有插槽和钻头
    /// </summary>
    public void ClearSlotsAndBits()
    {
        placedSlots.Clear();
        insertedBits.Clear();
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
        
        foreach (var slot in this.placedSlots)
        {
            clone.placedSlots.Add(slot.Clone());
        }
        
        foreach (var bit in this.insertedBits)
        {
            clone.insertedBits.Add(bit.Clone());
        }
        
        return clone;
    }
}
