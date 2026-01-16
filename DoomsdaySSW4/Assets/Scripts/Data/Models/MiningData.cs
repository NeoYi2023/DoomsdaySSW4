using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 挖矿系统整体数据（当前关卡）
/// 对应 SPEC 3.10 MiningData / MiningLayer / MiningTile
/// </summary>
[Serializable]
public class MiningData
{
    // 挖矿状态
    public int currentDepth;                 // 当前挖掘深度（层数，下标从0或1视生成逻辑而定）
    public int maxDepth;                     // 最大挖掘深度
    public List<MiningLayerData> layers;     // 各层挖矿地图

    // 资源相关
    public Dictionary<MineralType, int> minerals =
        new Dictionary<MineralType, int>();  // 已挖掘的矿物累计数量

    public int energy;                       // 当前关卡内累计能源值
    public int maxEnergy;                    // 最大能源（如用于体力/行动限制，可选）

    // 回合相关
    public int currentTurn;                  // 当前回合数
    public int maxTurns;                     // 最大回合数（可由任务决定）
}

/// <summary>
/// 单层挖矿地图数据
/// </summary>
[Serializable]
public class MiningLayerData
{
    public int layerDepth;                   // 层数（深度）

    // 为了便于序列化，这里使用一维数组模拟 9x9 网格
    // 网格尺寸在运行时由常量提供
    public List<MiningTileData> tiles = new List<MiningTileData>();

    public Vector2Int drillCenter;           // 钻头中心点位置（默认在层中心：4,4）
}

/// <summary>
/// 单个瓦片的数据（岩石 / 矿石 / 空等）
/// </summary>
[Serializable]
public class MiningTileData
{
    public int x;
    public int y;

    public TileType tileType;               // 瓦片类型（岩石、矿物、空等）
    public MineralType mineralType;         // 矿物类型（如适用）

    public int hardness;                    // 当前硬度（血量）
    public int maxHardness;                 // 最大硬度（初始血量）

    public bool isRevealed;                 // 是否已揭示
    public bool isMined;                    // 是否已挖掘
}

/// <summary>
/// 地图瓦片类型
/// </summary>
public enum TileType
{
    Empty,
    Rock,
    Ore
}

/// <summary>
/// 矿物类型（示例，与 SPEC 中的 MineralType 对应）
/// </summary>
public enum MineralType
{
    None,
    Iron,           // 铁
    Gold,           // 金
    Diamond,        // 钻石
    Crystal,        // 水晶
    EnergyCore      // 能量核心
}

