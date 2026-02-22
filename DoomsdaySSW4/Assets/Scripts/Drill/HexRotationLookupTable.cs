using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 六边形旋转预计算查表：按旋转中心与平台尺寸构建 forward/inverse 表，
/// 用纯轴向旋转生成 (col', row')，避免 odd-r 奇偶行格子数不同时的公式错格。
/// </summary>
public static class HexRotationLookupTable
{
    private const int GridSize = DrillPlatformData.PLATFORM_SIZE;
    private static readonly int[] Angles = { 0, 60, 120, 180, 240, 300 };
    private const int AngleCount = 6;

    private static Vector2Int _cachedCenter = new Vector2Int(int.MinValue, int.MinValue);
    private static Vector2Int[,,] _forwardTable;
    private static Dictionary<int, Dictionary<Vector2Int, Vector2Int>> _inverseTable;

    /// <summary>
    /// 顺时针 60° 轴向旋转一次：(q,r)→(q+r,-q)。
    /// </summary>
    private static void HexRotate60Clockwise(ref int q, ref int r)
    {
        int nq = q + r;
        int nr = -q;
        q = nq;
        r = nr;
    }

    /// <summary>
    /// odd-r 偏移坐标转轴向 (q, r)。
    /// </summary>
    private static void OffsetToAxial(int col, int row, out int q, out int r)
    {
        r = row;
        q = col - (row - (row & 1)) / 2;
    }

    /// <summary>
    /// odd-r 轴向 (q, r) 转偏移坐标。
    /// </summary>
    private static void AxialToOffset(int q, int r, out int col, out int row)
    {
        row = r;
        col = q + (r - (r & 1)) / 2;
    }

    /// <summary>
    /// 对给定中心与网格尺寸，用纯轴向旋转计算 (col, row) 绕中心顺时针旋转 angle 后的偏移坐标。
    /// 不依赖「方向取整」，适用于奇偶行格子数不同的阵列。
    /// </summary>
    private static Vector2Int RotateOffsetAroundCenterByAxial(int col, int row, int cx, int cy, int angle)
    {
        angle = ((angle % 360) + 360) % 360;
        int steps = angle / 60;
        if (steps == 0) return new Vector2Int(col, row);

        OffsetToAxial(col, row, out int q, out int r);
        OffsetToAxial(cx, cy, out int qc, out int rc);
        int dq = q - qc;
        int dr = r - rc;
        for (int i = 0; i < steps; i++)
            HexRotate60Clockwise(ref dq, ref dr);
        int q2 = qc + dq;
        int r2 = rc + dr;
        AxialToOffset(q2, r2, out int col2, out int row2);
        return new Vector2Int(col2, row2);
    }

    private static void EnsureTableForCenter(Vector2Int center)
    {
        if (_forwardTable != null && _cachedCenter.x == center.x && _cachedCenter.y == center.y)
            return;

        _cachedCenter = center;
        int cx = center.x;
        int cy = center.y;
        _forwardTable = new Vector2Int[GridSize, GridSize, AngleCount];
        _inverseTable = new Dictionary<int, Dictionary<Vector2Int, Vector2Int>>();
        for (int a = 0; a < AngleCount; a++)
            _inverseTable[a] = new Dictionary<Vector2Int, Vector2Int>();

        for (int col = 0; col < GridSize; col++)
        {
            for (int row = 0; row < GridSize; row++)
            {
                for (int ai = 0; ai < AngleCount; ai++)
                {
                    Vector2Int rotated = RotateOffsetAroundCenterByAxial(col, row, cx, cy, Angles[ai]);
                    _forwardTable[col, row, ai] = rotated;
                    Vector2Int key = rotated;
                    if (!_inverseTable[ai].ContainsKey(key))
                        _inverseTable[ai][key] = new Vector2Int(col, row);
                }
            }
        }
    }

    /// <summary>
    /// 获取平台格 (col, row) 绕 center 顺时针旋转 angle 后的坐标 (col', row')。
    /// 若 (col, row) 在平台范围内则查表，否则回退公式。
    /// </summary>
    public static Vector2Int GetRotated(Vector2Int center, int col, int row, int angle)
    {
        angle = ((angle % 360) + 360) % 360;
        int ai = angle / 60;
        if (ai >= AngleCount) ai = 0;
        bool inBounds = col >= 0 && col < GridSize && row >= 0 && row < GridSize;
        if (inBounds)
        {
            EnsureTableForCenter(center);
            return _forwardTable[col, row, ai];
        }
        return RotateOffsetAroundCenterByAxial(col, row, center.x, center.y, angle);
    }

    /// <summary>
    /// 获取「旋转后落在 (colPrime, rowPrime)」的平台格 (col, row)；即逆查表。
    /// 传入的 angle 为「逆旋转角度」（如 300 表示相对 60° 的逆），内部转换为正向角度再查表。
    /// 若无表项则回退 DrillShapeRotator.RotateOffsetPointAroundCenter(position, center, angle)。
    /// </summary>
    public static Vector2Int GetInverseRotated(Vector2Int center, int colPrime, int rowPrime, int inverseAngle)
    {
        inverseAngle = ((inverseAngle % 360) + 360) % 360;
        int forwardAngle = (360 - inverseAngle) % 360;
        int ai = forwardAngle / 60;
        if (ai >= AngleCount) ai = 0;
        EnsureTableForCenter(center);
        var key = new Vector2Int(colPrime, rowPrime);
        if (_inverseTable[ai].TryGetValue(key, out Vector2Int platformCell))
            return platformCell;
        return DrillShapeRotator.RotateOffsetPointAroundCenter(key, center, inverseAngle);
    }
}
