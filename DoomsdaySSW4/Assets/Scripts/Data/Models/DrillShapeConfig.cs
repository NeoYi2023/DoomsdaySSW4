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
    /// 该造型上的钻头插槽配置列表
    /// </summary>
    public List<DrillSlotConfig> slots = new List<DrillSlotConfig>();
    
    /// <summary>
    /// 获取造型占用的格子数量
    /// </summary>
    public int CellCount => cells?.Count ?? 0;
    
    /// <summary>
    /// 获取旋转后的格子坐标列表
    /// </summary>
    /// <param name="rotation">旋转角度（0/60/120/180/240/300）</param>
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
    /// <param name="degrees">旋转角度（0/60/120/180/240/300 等，支持 60 的倍数）</param>
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
    /// 旋转单个点（顺时针）。支持 0/60/120/180/240/300 等 60 的倍数。
    /// 60° 步长使用六边形轴向旋转，与平台 HexLayoutGroup（odd-r 平顶）一致，避免笛卡尔取整导致奇数次旋转造型错位。
    /// </summary>
    private static Vector2Int RotatePoint(Vector2Int point, int degrees)
    {
        switch (degrees)
        {
            case 0:
                return point;
            case 60:
                return HexRotate60Clockwise(point);
            case 120:
                return HexRotate60Clockwise(HexRotate60Clockwise(point));
            case 180:
                return HexRotate60Clockwise(HexRotate60Clockwise(HexRotate60Clockwise(point)));
            case 240:
                return HexRotate60Clockwise(HexRotate60Clockwise(HexRotate60Clockwise(HexRotate60Clockwise(point))));
            case 300:
                return HexRotate60Clockwise(HexRotate60Clockwise(HexRotate60Clockwise(HexRotate60Clockwise(HexRotate60Clockwise(point)))));
            default:
                // 兼容旧 90° 步长及任意角度（按浮点旋转后取整）
                return RotatePointByAngle(point, (float)degrees);
        }
    }

    /// <summary>
    /// 轴向坐标下顺时针 60° 旋转。
    /// 配置 cells 本身是 axial (q,r)，GetOccupiedCells 也按 axial 消费，
    /// 因此无需 offset↔axial 转换，直接做轴向旋转: (q,r)→(q+r,-q)。
    /// </summary>
    private static Vector2Int HexRotate60Clockwise(Vector2Int point)
    {
        int q = point.x;
        int r = point.y;
        return new Vector2Int(q + r, -q);
    }

    /// <summary>
    /// 按任意角度顺时针旋转单点（弧度旋转后四舍五入到整数格）。仅用于非 60° 倍数的兼容路径。
    /// </summary>
    private static Vector2Int RotatePointByAngle(Vector2Int point, float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        int x2 = Mathf.RoundToInt(point.x * cos + point.y * sin);
        int y2 = Mathf.RoundToInt(-point.x * sin + point.y * cos);
        return new Vector2Int(x2, y2);
    }

    /// <summary>
    /// 绕指定中心点顺时针旋转单个点（当前按轴向公式，适用于造型相对坐标 axial）
    /// </summary>
    /// <param name="point">待旋转的点</param>
    /// <param name="center">旋转中心</param>
    /// <param name="degrees">旋转角度（0/60/120/180/240/300 等）</param>
    public static Vector2Int RotatePointAroundCenter(Vector2Int point, Vector2Int center, int degrees)
    {
        int dx = point.x - center.x;
        int dy = point.y - center.y;
        Vector2Int rotated = RotatePoint(new Vector2Int(dx, dy), degrees);
        return new Vector2Int(center.x + rotated.x, center.y + rotated.y);
    }

    /// <summary>
    /// odd-r 平顶六边形下 60° 顺时针的 6 个方向（轴向坐标），与 odd-r 偏移的 6 邻格一致。
    /// 顺序：(-1,0), (0,1), (1,1), (1,0), (1,-1), (0,-1)；60° CW 为下一项，对应偏移增量 (-1,0),(0,1),(1,1),(1,0),(1,-1),(0,-1)。
    /// </summary>
    private static readonly int[] Offset60DirCol = { -1, 0, 1, 1, 1, 0 };
    private static readonly int[] Offset60DirRow = { 0, 1, 1, 0, -1, -1 };

    /// <summary>
    /// 绕指定中心点顺时针旋转单个点（odd-r 偏移坐标，用于平台/地图格）。
    /// 使用与钻头编辑一致的 6 方向 60° 顺序，保证 (-1,0) 旋转到 (0,1) 等与视觉一致。
    /// </summary>
    public static Vector2Int RotateOffsetPointAroundCenter(Vector2Int point, Vector2Int center, int degrees)
    {
        degrees = ((degrees % 360) + 360) % 360;
        int dx = point.x - center.x;
        int dy = point.y - center.y;
        if (dx == 0 && dy == 0) return point;
        int q = dx - (dy - (dy & 1)) / 2;
        int r = dy;
        int ring = (Math.Abs(q) + Math.Abs(r) + Math.Abs(q + r)) / 2;
        if (ring == 0) return point;
        int steps = degrees / 60;
        int best = 0;
        int bestDot = q * Offset60DirCol[0] + r * Offset60DirRow[0];
        for (int i = 1; i < 6; i++)
        {
            int d = q * Offset60DirCol[i] + r * Offset60DirRow[i];
            if (d > bestDot) { bestDot = d; best = i; }
        }
        int j = (best + steps) % 6;
        int q2 = Offset60DirCol[j] * ring;
        int r2 = Offset60DirRow[j] * ring;
        // odd-r 轴向→偏移：col 依赖「结果行」center.y + r2 的奇偶性，不能只用 r2
        int outRow = center.y + r2;
        int col2 = q2 + (outRow - (outRow & 1)) / 2 - (center.y - (center.y & 1)) / 2;
        int row2 = r2;
        return new Vector2Int(center.x + col2, center.y + row2);
    }

    /// <summary>
    /// 绕指定中心点顺时针旋转格子坐标列表（用于旋转挖掘等）
    /// </summary>
    /// <param name="cells">原始格子坐标列表</param>
    /// <param name="center">旋转中心</param>
    /// <param name="degrees">旋转角度（0/60/120/180/240/300 等）</param>
    public static List<Vector2Int> RotateCellsAroundCenter(List<Vector2Int> cells, Vector2Int center, int degrees)
    {
        if (cells == null) return new List<Vector2Int>();
        degrees = ((degrees % 360) + 360) % 360;
        List<Vector2Int> result = new List<Vector2Int>();
        foreach (var cell in cells)
            result.Add(RotatePointAroundCenter(cell, center, degrees));
        return result;
    }
}
