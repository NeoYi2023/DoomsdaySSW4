using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 钻头造型配置（从配置表加载）
/// 定义造型的形状、基础攻击强度和特性
/// </summary>
[Serializable]
public class DrillShapeConfig
{
    /// <summary>
    /// 造型唯一ID
    /// </summary>
    public string shapeId;
    
    /// <summary>
    /// 造型名称
    /// </summary>
    public string shapeName;
    
    /// <summary>
    /// 单格基础攻击强度
    /// </summary>
    public int baseAttackStrength;
    
    /// <summary>
    /// 相对于锚点(0,0)的格子坐标列表
    /// 例如：L型造型 = [(0,0), (0,1), (0,2), (1,2)]
    /// </summary>
    public List<Vector2Int> cells = new List<Vector2Int>();
    
    /// <summary>
    /// 造型特性列表
    /// </summary>
    public List<ShapeTraitConfig> traits = new List<ShapeTraitConfig>();
    
    /// <summary>
    /// 造型描述
    /// </summary>
    public string description;
    
    /// <summary>
    /// 获取造型占用的格子数量
    /// </summary>
    public int CellCount => cells?.Count ?? 0;
    
    /// <summary>
    /// 获取旋转后的格子坐标列表
    /// </summary>
    /// <param name="rotation">旋转角度（0/90/180/270）</param>
    /// <returns>旋转后的格子坐标列表</returns>
    public List<Vector2Int> GetRotatedCells(int rotation)
    {
        return DrillShapeRotator.RotateCells(cells, rotation);
    }
}

/// <summary>
/// 造型特性配置
/// 定义造型的被动或条件触发效果
/// </summary>
[Serializable]
public class ShapeTraitConfig
{
    /// <summary>
    /// 特性ID
    /// </summary>
    public string traitId;
    
    /// <summary>
    /// 特性名称
    /// </summary>
    public string traitName;
    
    /// <summary>
    /// 触发条件
    /// "always" - 始终生效
    /// "ore_type:energy" - 挖掘能源矿石时生效
    /// "ore_type:rare" - 挖掘稀有矿石时生效
    /// </summary>
    public string triggerCondition;
    
    /// <summary>
    /// 效果类型
    /// "attack_multiplier" - 攻击力倍率加成
    /// "attack_add" - 攻击力固定加成
    /// </summary>
    public string effectType;
    
    /// <summary>
    /// 效果数值
    /// 对于attack_multiplier，1.1表示+10%
    /// 对于attack_add，5表示+5攻击力
    /// </summary>
    public float effectValue;
    
    /// <summary>
    /// 特性描述
    /// </summary>
    public string description;
}

/// <summary>
/// 造型配置集合（用于JSON反序列化）
/// </summary>
[Serializable]
public class DrillShapeConfigCollection
{
    public List<DrillShapeConfig> shapes = new List<DrillShapeConfig>();
}

/// <summary>
/// 造型旋转工具类
/// </summary>
public static class DrillShapeRotator
{
    /// <summary>
    /// 旋转格子坐标列表
    /// </summary>
    /// <param name="cells">原始格子坐标列表</param>
    /// <param name="degrees">旋转角度（0/90/180/270）</param>
    /// <returns>旋转后的格子坐标列表</returns>
    public static List<Vector2Int> RotateCells(List<Vector2Int> cells, int degrees)
    {
        if (cells == null) return new List<Vector2Int>();
        
        // 标准化角度到0-359范围
        degrees = ((degrees % 360) + 360) % 360;
        
        List<Vector2Int> rotatedCells = new List<Vector2Int>();
        
        foreach (var cell in cells)
        {
            Vector2Int rotated = RotatePoint(cell, degrees);
            rotatedCells.Add(rotated);
        }
        
        return rotatedCells;
    }
    
    /// <summary>
    /// 旋转单个点
    /// 90度顺时针旋转公式：(x, y) -> (y, -x)
    /// </summary>
    private static Vector2Int RotatePoint(Vector2Int point, int degrees)
    {
        switch (degrees)
        {
            case 0:
                return point;
            case 90:
                return new Vector2Int(point.y, -point.x);
            case 180:
                return new Vector2Int(-point.x, -point.y);
            case 270:
                return new Vector2Int(-point.y, point.x);
            default:
                Debug.LogWarning($"不支持的旋转角度: {degrees}，仅支持0/90/180/270");
                return point;
        }
    }
}
