using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 船只初始钻头配置（定义船只初始时平台上已放置的钻头造型）
/// </summary>
[Serializable]
public class ShipInitialDrillConfig
{
    public string shipId;           // 船只ID
    public string shapeId;          // 造型ID
    public int positionX;           // 在平台上的X坐标（有效范围由 DrillPlatformData.PLATFORM_SIZE 决定，默认 9x9 配置下为 0-8）
    public int positionY;           // 在平台上的Y坐标（有效范围由 DrillPlatformData.PLATFORM_SIZE 决定，默认 9x9 配置下为 0-8）
    public int rotation;            // 旋转角度（0/90/180/270）
    /// <summary>平台旋转中心 X 坐标（0~PLATFORM_SIZE-1），-1 表示未配置</summary>
    public int rotationCenterX = -1;
    /// <summary>平台旋转中心 Y 坐标（0~PLATFORM_SIZE-1），-1 表示未配置</summary>
    public int rotationCenterY = -1;

    /// <summary>
    /// 获取位置向量
    /// </summary>
    public Vector2Int GetPosition()
    {
        return new Vector2Int(positionX, positionY);
    }

    /// <summary>
    /// 配置中是否包含有效的旋转中心（在平台边界内且非 -1）
    /// </summary>
    public bool HasValidRotationCenter()
    {
        if (rotationCenterX < 0 || rotationCenterY < 0) return false;
        return DrillPlatformData.IsWithinBounds(new Vector2Int(rotationCenterX, rotationCenterY));
    }
}

/// <summary>
/// 船只初始钻头配置集合
/// </summary>
[Serializable]
public class ShipInitialDrillConfigCollection
{
    public List<ShipInitialDrillConfig> configs = new List<ShipInitialDrillConfig>();
}

/// <summary>
/// 按船只分组的初始钻头配置
/// </summary>
[Serializable]
public class ShipInitialDrillsData
{
    public string shipId;
    public List<ShipInitialDrillConfig> drills = new List<ShipInitialDrillConfig>();
}
