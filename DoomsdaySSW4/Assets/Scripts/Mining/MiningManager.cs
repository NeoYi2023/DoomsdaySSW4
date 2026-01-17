using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

/// <summary>
/// 挖矿管理器：负责挖矿地图生成、矿石生成、攻击机制、挖掘逻辑
/// </summary>
public class MiningManager : MonoBehaviour
{
    private static MiningManager _instance;
    public static MiningManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("MiningManager");
                _instance = go.AddComponent<MiningManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // 地图尺寸常量
    public const int LAYER_WIDTH = 9;
    public const int LAYER_HEIGHT = 9;

    // 当前挖矿数据
    private MiningData _miningData;
    private ConfigManager _configManager;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        _configManager = ConfigManager.Instance;
    }

    /// <summary>
    /// 确保 ConfigManager 已初始化（延迟初始化）
    /// </summary>
    private void EnsureConfigManager()
    {
        if (_configManager == null)
        {
            _configManager = ConfigManager.Instance;
        }
    }

    /// <summary>
    /// 初始化挖矿地图
    /// </summary>
    public void InitializeMiningMap(int maxDepth, int seed)
    {
        // 确保 ConfigManager 已初始化（防止在 Start() 之前调用）
        EnsureConfigManager();

        _miningData = new MiningData
        {
            currentDepth = 1, // 从第1层开始
            maxDepth = maxDepth,
            layers = new List<MiningLayerData>(),
            minerals = new Dictionary<MineralType, int>(),
            energy = 0,
            maxEnergy = 100,
            currentTurn = 0,
            maxTurns = 0
        };

        // 生成各层地图
        for (int depth = 1; depth <= maxDepth; depth++)
        {
            MiningLayerData layer = GenerateLayer(depth, seed + depth);
            _miningData.layers.Add(layer);
        }

        Debug.Log($"挖矿地图初始化完成，共 {maxDepth} 层");
    }

    /// <summary>
    /// 生成指定层的地图
    /// </summary>
    private MiningLayerData GenerateLayer(int layerDepth, int seed)
    {
        MiningLayerData layer = new MiningLayerData
        {
            layerDepth = layerDepth,
            tiles = new List<MiningTileData>(),
            drillCenter = new Vector2Int(4, 4) // 9x9的中心点
        };

        // 获取该层的矿石生成规则
        OreSpawnConfig spawnConfig = _configManager.GetOreSpawnConfig(layerDepth);

        // 使用种子初始化随机数生成器
        System.Random random = new System.Random(seed);

        int oreCount = 0;
        int rockCount = 0;
        
        // 用于跟踪每种矿石类型的当前数量（在生成过程中）
        Dictionary<string, int> oreTypeCounts = new Dictionary<string, int>();

        // 生成所有格子
        for (int x = 0; x < LAYER_WIDTH; x++)
        {
            for (int y = 0; y < LAYER_HEIGHT; y++)
            {
                MiningTileData tile = new MiningTileData
                {
                    x = x,
                    y = y,
                    tileType = TileType.Rock,
                    mineralType = MineralType.None,
                    hardness = 0,
                    maxHardness = 0,
                    isRevealed = false,
                    isMined = false
                };

                // 根据生成规则随机生成矿石（使用临时计数器，而不是CountOresInLayer）
                OreData ore = GenerateOreAtPositionWithCounter(x, y, layerDepth, spawnConfig, random, oreTypeCounts);
                if (ore != null)
                {
                    tile.tileType = TileType.Ore;
                    tile.mineralType = GetMineralTypeFromOreId(ore.oreId);
                    tile.hardness = ore.currentHardness;
                    tile.maxHardness = ore.maxHardness;
                    oreCount++;
                }
                else
                {
                    rockCount++;
                }

                layer.tiles.Add(tile);
            }
        }

        return layer;
    }

    /// <summary>
    /// 在指定位置生成矿石（使用计数器跟踪数量，用于生成过程中）
    /// </summary>
    private OreData GenerateOreAtPositionWithCounter(int x, int y, int layerDepth, OreSpawnConfig spawnConfig, System.Random random, Dictionary<string, int> oreTypeCounts)
    {
        if (spawnConfig == null || spawnConfig.spawnRules == null || spawnConfig.spawnRules.Count == 0)
        {
            return null;
        }

        // 计算总权重
        int totalWeight = spawnConfig.spawnRules.Sum(rule => rule.weight);
        if (totalWeight == 0)
        {
            return null;
        }

        // 随机选择矿石类型（基于权重）
        int randomValue = random.Next(0, totalWeight);
        int currentWeight = 0;

        OreSpawnRule selectedRule = null;
        foreach (var rule in spawnConfig.spawnRules)
        {
            currentWeight += rule.weight;
            if (randomValue < currentWeight)
            {
                selectedRule = rule;
                break;
            }
        }

        if (selectedRule == null) return null;

        // 检查该层该类型矿石是否已达到最大数量（使用传入的计数器）
        if (!oreTypeCounts.ContainsKey(selectedRule.oreId))
        {
            oreTypeCounts[selectedRule.oreId] = 0;
        }
        if (oreTypeCounts[selectedRule.oreId] >= selectedRule.maxCount)
        {
            return null;
        }

        // 获取矿石配置
        OreConfig oreConfig = _configManager.GetOreConfig(selectedRule.oreId);
        if (oreConfig == null) return null;

        // 检查深度范围
        if (layerDepth < oreConfig.minDepth || layerDepth > oreConfig.maxDepth)
        {
            return null;
        }

        // 增加计数器
        oreTypeCounts[selectedRule.oreId]++;

        // 创建矿石数据
        OreData ore = new OreData
        {
            oreId = oreConfig.oreId,
            oreName = oreConfig.oreName,
            oreType = oreConfig.oreType,
            maxHardness = oreConfig.hardness,
            currentHardness = oreConfig.hardness,
            requiredAttributeKey = oreConfig.requiredAttributeKey,
            requiredAttributeValue = oreConfig.requiredAttributeValue,
            value = oreConfig.value,
            isEnergyOre = oreConfig.isEnergyOre,
            energyValue = oreConfig.energyValue,
            position = new Vector2Int(x, y),
            depth = layerDepth,
            isMined = false
        };

        return ore;
    }

    /// <summary>
    /// 在指定位置生成矿石（用于其他场景，使用CountOresInLayer统计）
    /// </summary>
    private OreData GenerateOreAtPosition(int x, int y, int layerDepth, OreSpawnConfig spawnConfig, System.Random random)
    {
        if (spawnConfig == null || spawnConfig.spawnRules == null || spawnConfig.spawnRules.Count == 0)
        {
            return null;
        }

        // 计算总权重
        int totalWeight = spawnConfig.spawnRules.Sum(rule => rule.weight);
        if (totalWeight == 0)
        {
            return null;
        }

        // 随机选择矿石类型（基于权重）
        int randomValue = random.Next(0, totalWeight);
        int currentWeight = 0;

        OreSpawnRule selectedRule = null;
        foreach (var rule in spawnConfig.spawnRules)
        {
            currentWeight += rule.weight;
            if (randomValue < currentWeight)
            {
                selectedRule = rule;
                break;
            }
        }

        if (selectedRule == null) return null;

        // 检查该层该类型矿石是否已达到最大数量
        int currentCount = CountOresInLayer(layerDepth, selectedRule.oreId);
        if (currentCount >= selectedRule.maxCount)
        {
            return null;
        }

        // 获取矿石配置
        OreConfig oreConfig = _configManager.GetOreConfig(selectedRule.oreId);
        if (oreConfig == null) return null;

        // 检查深度范围
        if (layerDepth < oreConfig.minDepth || layerDepth > oreConfig.maxDepth)
        {
            return null;
        }

        // 创建矿石数据
        OreData ore = new OreData
        {
            oreId = oreConfig.oreId,
            oreName = oreConfig.oreName,
            oreType = oreConfig.oreType,
            maxHardness = oreConfig.hardness,
            currentHardness = oreConfig.hardness,
            requiredAttributeKey = oreConfig.requiredAttributeKey,
            requiredAttributeValue = oreConfig.requiredAttributeValue,
            value = oreConfig.value,
            isEnergyOre = oreConfig.isEnergyOre,
            energyValue = oreConfig.energyValue,
            position = new Vector2Int(x, y),
            depth = layerDepth,
            isMined = false
        };

        return ore;
    }

    /// <summary>
    /// 统计指定层指定矿石类型的数量
    /// </summary>
    private int CountOresInLayer(int layerDepth, string oreId)
    {
        if (_miningData == null || _miningData.layers == null)
            return 0;

        MiningLayerData layer = _miningData.layers.FirstOrDefault(l => l.layerDepth == layerDepth);
        if (layer == null || layer.tiles == null)
            return 0;

        // 直接通过MineralType统计，避免重复调用GetOreAtPosition
        MineralType targetType = GetMineralTypeFromOreId(oreId);
        return layer.tiles.Count(t => 
            t.tileType == TileType.Ore && 
            !t.isMined &&
            t.mineralType == targetType);
    }

    /// <summary>
    /// 获取指定位置的矿石数据
    /// </summary>
    private OreData GetOreAtPosition(int layerDepth, int x, int y)
    {
        MiningLayerData layer = GetLayer(layerDepth);
        if (layer == null) return null;

        MiningTileData tile = layer.tiles.FirstOrDefault(t => t.x == x && t.y == y);
        if (tile == null || tile.tileType != TileType.Ore || tile.isMined)
            return null;

        // 从配置重建矿石数据
        OreConfig config = _configManager.GetOreConfig(GetOreIdFromMineralType(tile.mineralType));
        if (config == null) return null;

        return new OreData
        {
            oreId = config.oreId,
            oreName = config.oreName,
            oreType = config.oreType,
            mineralType = tile.mineralType,
            maxHardness = tile.maxHardness,
            currentHardness = tile.hardness,
            value = config.value,
            isEnergyOre = config.isEnergyOre,
            energyValue = config.energyValue,
            position = new Vector2Int(x, y),
            depth = layerDepth,
            isMined = tile.isMined
        };
    }

    /// <summary>
    /// 获取将要被攻击的格子列表（不造成伤害，用于动效）
    /// </summary>
    public List<AttackedTileInfo> GetTilesToAttack(DrillData drill, int layerDepth)
    {
        List<AttackedTileInfo> tilesToAttack = new List<AttackedTileInfo>();
        
        if (_miningData == null || drill == null)
        {
            return tilesToAttack;
        }

        MiningLayerData layer = GetLayer(layerDepth);
        if (layer == null)
        {
            return tilesToAttack;
        }

        Vector2Int drillCenter = layer.drillCenter;
        Vector2Int range = drill.GetEffectiveRange();
        int attackValue = drill.GetEffectiveStrength();

        // 计算攻击范围（以钻头中心为基准）
        int halfRangeX = range.x / 2;
        int halfRangeY = range.y / 2;

        int minX = Mathf.Max(0, drillCenter.x - halfRangeX);
        int maxX = Mathf.Min(LAYER_WIDTH - 1, drillCenter.x + halfRangeX);
        int minY = Mathf.Max(0, drillCenter.y - halfRangeY);
        int maxY = Mathf.Min(LAYER_HEIGHT - 1, drillCenter.y + halfRangeY);

        // 遍历范围内的所有格子，只记录不造成伤害
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                MiningTileData tile = layer.tiles.FirstOrDefault(t => t.x == x && t.y == y);
                if (tile == null || tile.tileType != TileType.Ore || tile.isMined)
                    continue;

                // 只记录要攻击的格子信息，不造成伤害
                tilesToAttack.Add(new AttackedTileInfo
                {
                    position = new Vector2Int(x, y),
                    attackStrength = attackValue
                });
            }
        }

        return tilesToAttack;
    }

    /// <summary>
    /// 攻击范围内的矿石（每回合执行）
    /// </summary>
    public MiningResult AttackOresInRange(DrillData drill, int layerDepth)
    {
        if (_miningData == null)
        {
            Debug.LogError("挖矿数据未初始化");
            return new MiningResult();
        }

        MiningLayerData layer = GetLayer(layerDepth);
        if (layer == null)
        {
            Debug.LogError($"未找到层 {layerDepth}");
            return new MiningResult();
        }

        MiningResult result = new MiningResult
        {
            success = true,
            minedOres = new List<OreData>(),
            moneyGained = 0,
            energyGained = 0,
            partiallyDamagedOres = new List<OreData>()
        };

        Vector2Int drillCenter = layer.drillCenter;
        Vector2Int range = drill.GetEffectiveRange();
        int attackValue = drill.GetEffectiveStrength();

        // 计算攻击范围（以钻头中心为基准）
        int halfRangeX = range.x / 2;
        int halfRangeY = range.y / 2;

        int minX = Mathf.Max(0, drillCenter.x - halfRangeX);
        int maxX = Mathf.Min(LAYER_WIDTH - 1, drillCenter.x + halfRangeX);
        int minY = Mathf.Max(0, drillCenter.y - halfRangeY);
        int maxY = Mathf.Min(LAYER_HEIGHT - 1, drillCenter.y + halfRangeY);

        // 遍历范围内的所有格子
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                MiningTileData tile = layer.tiles.FirstOrDefault(t => t.x == x && t.y == y);
                if (tile == null || tile.tileType != TileType.Ore || tile.isMined)
                    continue;

                // 对矿石造成伤害
                tile.hardness -= attackValue;

                // 记录被攻击的格子信息（用于动效）
                result.attackedTiles.Add(new AttackedTileInfo
                {
                    position = new Vector2Int(x, y),
                    attackStrength = attackValue
                });

                // 如果硬度归零，挖掉矿石
                if (tile.hardness <= 0)
                {
                    OreData ore = GetOreAtPosition(layerDepth, x, y);
                    if (ore != null)
                    {
                        // 检查是否满足额外属性要求（简化实现，暂时跳过）
                        bool canMine = true; // 简化：暂时都允许挖掘

                        if (canMine)
                        {
                            tile.isMined = true;
                            ore.isMined = true;
                            result.minedOres.Add(ore);

                            // 累计矿物
                            if (!_miningData.minerals.ContainsKey(ore.mineralType))
                                _miningData.minerals[ore.mineralType] = 0;
                            _miningData.minerals[ore.mineralType]++;

                            // 累计金钱和能源
                            if (ore.isEnergyOre)
                            {
                                result.energyGained += ore.energyValue;
                            }
                            else
                            {
                                result.moneyGained += ore.value;
                            }
                        }
                    }
                }
                else
                {
                    // 部分受损的矿石
                    OreData ore = GetOreAtPosition(layerDepth, x, y);
                    if (ore != null)
                    {
                        ore.currentHardness = tile.hardness;
                        result.partiallyDamagedOres.Add(ore);
                    }
                }
            }
        }


        return result;
    }

    /// <summary>
    /// 检查指定层是否完全挖完（攻击范围内的所有矿石都被挖掉）
    /// </summary>
    public bool IsLayerFullyMined(int layerDepth)
    {
        if (_miningData == null)
            return false;

        MiningLayerData layer = GetLayer(layerDepth);
        if (layer == null || layer.tiles == null)
            return false;

        // 获取当前钻头信息
        DrillManager drillManager = DrillManager.Instance;
        DrillData drill = drillManager?.GetCurrentDrill();
        if (drill == null)
        {
            // 如果没有钻头，检查全层（向后兼容）
            bool hasUnminedOre = layer.tiles.Any(t => t.tileType == TileType.Ore && !t.isMined);
            return !hasUnminedOre;
        }

        // 计算攻击范围（只检查攻击范围内的矿石）
        Vector2Int drillCenter = layer.drillCenter;
        Vector2Int range = drill.GetEffectiveRange();
        int halfRangeX = range.x / 2;
        int halfRangeY = range.y / 2;

        int minX = Mathf.Max(0, drillCenter.x - halfRangeX);
        int maxX = Mathf.Min(LAYER_WIDTH - 1, drillCenter.x + halfRangeX);
        int minY = Mathf.Max(0, drillCenter.y - halfRangeY);
        int maxY = Mathf.Min(LAYER_HEIGHT - 1, drillCenter.y + halfRangeY);

        // 只检查攻击范围内是否还有未挖的矿石
        bool hasUnminedOreInRange = layer.tiles.Any(t => 
            t.tileType == TileType.Ore && 
            !t.isMined &&
            t.x >= minX && t.x <= maxX &&
            t.y >= minY && t.y <= maxY);
        
        return !hasUnminedOreInRange;
    }

    /// <summary>
    /// 切换到下一层（如果当前层已挖完）
    /// </summary>
    public bool TrySwitchToNextLayer()
    {
        if (_miningData == null)
            return false;

        int currentLayer = _miningData.currentDepth;

        // 检查当前层是否挖完
        if (!IsLayerFullyMined(currentLayer))
        {
            return false;
        }

        // 检查是否还有下一层
        if (currentLayer >= _miningData.maxDepth)
        {
            return false;
        }

        // 切换到下一层
        int nextLayer = currentLayer + 1;
        SetCurrentDepth(nextLayer);

        return true;
    }

    /// <summary>
    /// 获取指定层的数据
    /// </summary>
    public MiningLayerData GetLayer(int layerDepth)
    {
        if (_miningData == null || _miningData.layers == null)
            return null;

        return _miningData.layers.FirstOrDefault(l => l.layerDepth == layerDepth);
    }

    /// <summary>
    /// 额外发现矿石（矿石发现能力加成）
    /// </summary>
    public void DiscoverAdditionalOres(int layerDepth, int count)
    {
        if (_miningData == null)
            return;

        MiningLayerData layer = GetLayer(layerDepth);
        if (layer == null || layer.tiles == null)
            return;

        // 获取所有未揭示的矿石
        List<MiningTileData> unrevealedOres = layer.tiles
            .Where(t => t.tileType == TileType.Ore && !t.isRevealed && !t.isMined)
            .ToList();

        if (unrevealedOres.Count == 0)
            return;

        // 随机选择要揭示的矿石
        System.Random random = new System.Random();
        int revealCount = Mathf.Min(count, unrevealedOres.Count);
        
        for (int i = 0; i < revealCount; i++)
        {
            int index = random.Next(0, unrevealedOres.Count);
            unrevealedOres[index].isRevealed = true;
            unrevealedOres.RemoveAt(index);
        }

        Debug.Log($"额外发现 {revealCount} 个矿石（矿石发现能力加成）");
    }

    /// <summary>
    /// 获取当前层的矿石网格（用于UI显示）
    /// </summary>
    public MiningTileData[,] GetLayerTileGrid(int layerDepth)
    {
        MiningLayerData layer = GetLayer(layerDepth);
        if (layer == null) return null;

        MiningTileData[,] grid = new MiningTileData[LAYER_WIDTH, LAYER_HEIGHT];
        foreach (var tile in layer.tiles)
        {
            grid[tile.x, tile.y] = tile;
        }

        return grid;
    }

    /// <summary>
    /// 获取当前挖矿数据
    /// </summary>
    public MiningData GetMiningData()
    {
        return _miningData;
    }

    /// <summary>
    /// 设置当前深度
    /// </summary>
    public void SetCurrentDepth(int depth)
    {
        if (_miningData != null)
        {
            _miningData.currentDepth = depth;
        }
    }

    // 辅助方法：MineralType 和 OreId 的转换
    private MineralType GetMineralTypeFromOreId(string oreId)
    {
        switch (oreId)
        {
            case "iron": return MineralType.Iron;
            case "gold": return MineralType.Gold;
            case "diamond": return MineralType.Diamond;
            case "crystal": return MineralType.Crystal;
            case "energy_core": return MineralType.EnergyCore;
            default: return MineralType.None;
        }
    }

    private string GetOreIdFromMineralType(MineralType type)
    {
        switch (type)
        {
            case MineralType.Iron: return "iron";
            case MineralType.Gold: return "gold";
            case MineralType.Diamond: return "diamond";
            case MineralType.Crystal: return "crystal";
            case MineralType.EnergyCore: return "energy_core";
            default: return "";
        }
    }
}

/// <summary>
/// 被攻击的格子信息（用于动效）
/// </summary>
[System.Serializable]
public class AttackedTileInfo
{
    public Vector2Int position;       // 格子坐标
    public int attackStrength;       // 攻击强度值
}

/// <summary>
/// 挖矿结果
/// </summary>
[System.Serializable]
public class MiningResult
{
    public bool success;
    public List<OreData> minedOres = new List<OreData>();
    public int moneyGained;
    public int energyGained;
    public List<OreData> partiallyDamagedOres = new List<OreData>();
    public List<AttackedTileInfo> attackedTiles = new List<AttackedTileInfo>(); // 被攻击的格子信息（用于动效）
}
