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
    public int positionX;           // 在9x9平台上的X坐标（0-8）
    public int positionY;           // 在9x9平台上的Y坐标（0-8）
    public int rotation;            // 旋转角度（0/90/180/270）
    
    /// <summary>
    /// 获取位置向量
    /// </summary>
    public Vector2Int GetPosition()
    {
        return new Vector2Int(positionX, positionY);
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
