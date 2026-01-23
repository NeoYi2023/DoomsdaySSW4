using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// 钻头后效管理器：处理钻头后效（爆炸、连锁等）
/// </summary>
public class DrillBitEffectManager : MonoBehaviour
{
    private static DrillBitEffectManager _instance;
    public static DrillBitEffectManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("DrillBitEffectManager");
                _instance = go.AddComponent<DrillBitEffectManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private ConfigManager _configManager;
    private DrillPlatformManager _platformManager;
    private MiningManager _miningManager;

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
        _platformManager = DrillPlatformManager.Instance;
        _miningManager = MiningManager.Instance;
    }

    /// <summary>
    /// 处理挖掉矿石后的钻头后效
    /// </summary>
    /// <param name="minedPosition">被挖掉的矿石位置</param>
    /// <param name="layerDepth">层数</param>
    /// <param name="minedOreId">被挖掉的矿石ID</param>
    /// <returns>后效产生的额外伤害列表</returns>
    public List<BitEffectDamage> ProcessBitEffects(Vector2Int minedPosition, int layerDepth, string minedOreId)
    {
        List<BitEffectDamage> additionalDamages = new List<BitEffectDamage>();

        if (_platformManager == null || _configManager == null)
        {
            return additionalDamages;
        }

        // 获取影响该位置的钻头
        List<PlacedDrillBit> affectingBits = _platformManager.GetBitsAffectingCell(minedPosition);

        foreach (var bit in affectingBits)
        {
            DrillBitConfig bitConfig = _configManager.GetDrillBitConfig(bit.bitId);
            if (bitConfig == null || bitConfig.effects == null) continue;

            foreach (var effect in bitConfig.effects)
            {
                List<BitEffectDamage> effectDamages = ProcessEffect(
                    effect, 
                    bit, 
                    minedPosition, 
                    layerDepth);
                
                additionalDamages.AddRange(effectDamages);
            }
        }

        return additionalDamages;
    }

    /// <summary>
    /// 处理单个后效
    /// </summary>
    private List<BitEffectDamage> ProcessEffect(
        DrillBitEffect effect, 
        PlacedDrillBit bit, 
        Vector2Int triggerPosition, 
        int layerDepth)
    {
        List<BitEffectDamage> damages = new List<BitEffectDamage>();

        switch (effect.effectType)
        {
            case DrillBitEffectType.Explosion:
                damages.AddRange(ProcessExplosionEffect(effect, bit, triggerPosition, layerDepth));
                break;
            
            case DrillBitEffectType.ChainReaction:
                damages.AddRange(ProcessChainReactionEffect(effect, bit, triggerPosition, layerDepth));
                break;
            
            case DrillBitEffectType.AreaBoost:
                // 区域加成是持续效果，不需要在这里处理
                break;
        }

        return damages;
    }

    /// <summary>
    /// 处理爆炸效果：对周围造成伤害
    /// </summary>
    private List<BitEffectDamage> ProcessExplosionEffect(
        DrillBitEffect effect, 
        PlacedDrillBit bit, 
        Vector2Int centerPosition, 
        int layerDepth)
    {
        List<BitEffectDamage> damages = new List<BitEffectDamage>();

        DrillBitConfig bitConfig = _configManager.GetDrillBitConfig(bit.bitId);
        if (bitConfig == null) return damages;

        // 获取爆炸范围内的所有位置
        List<Vector2Int> affectedPositions = GetPositionsInRange(
            centerPosition, 
            effect.range, 
            bitConfig.includeDiagonal);

        foreach (var pos in affectedPositions)
        {
            // 跳过触发位置本身
            if (pos == centerPosition) continue;

            // 检查位置是否在地图范围内
            if (pos.x < 0 || pos.x >= MiningManager.LAYER_WIDTH ||
                pos.y < 0 || pos.y >= MiningManager.LAYER_HEIGHT)
                continue;

            // 检查该位置是否有矿石
            MiningLayerData layer = _miningManager.GetLayer(layerDepth);
            if (layer == null) continue;

            MiningTileData tile = layer.tiles.FirstOrDefault(
                t => t.x == pos.x && t.y == pos.y && 
                t.tileType == TileType.Ore && !t.isMined);
            
            if (tile != null)
            {
                // 计算伤害值（使用钻头的强度加成作为基础）
                int damage = effect.value;
                if (damage <= 0)
                {
                    // 如果没有指定伤害值，使用钻头的强度加成
                    damage = bitConfig.strengthBonus;
                }

                damages.Add(new BitEffectDamage
                {
                    position = pos,
                    damage = damage,
                    effectType = DrillBitEffectType.Explosion,
                    sourceBitId = bit.bitId
                });
            }
        }

        return damages;
    }

    /// <summary>
    /// 处理连锁反应效果
    /// </summary>
    private List<BitEffectDamage> ProcessChainReactionEffect(
        DrillBitEffect effect, 
        PlacedDrillBit bit, 
        Vector2Int triggerPosition, 
        int layerDepth)
    {
        // 连锁反应：当挖掉矿石时，如果周围有相同类型的矿石，也对它们造成伤害
        List<BitEffectDamage> damages = new List<BitEffectDamage>();

        MiningLayerData layer = _miningManager.GetLayer(layerDepth);
        if (layer == null) return damages;

        // 获取触发位置的矿石类型
        MiningTileData triggerTile = layer.tiles.FirstOrDefault(
            t => t.x == triggerPosition.x && t.y == triggerPosition.y);
        
        if (triggerTile == null || triggerTile.tileType != TileType.Ore) 
            return damages;

        string triggerOreId = GetOreIdFromMineralType(triggerTile.mineralType);

        DrillBitConfig bitConfig = _configManager.GetDrillBitConfig(bit.bitId);
        if (bitConfig == null) return damages;

        // 获取连锁范围内的位置
        List<Vector2Int> chainPositions = GetPositionsInRange(
            triggerPosition, 
            effect.range, 
            bitConfig.includeDiagonal);

        foreach (var pos in chainPositions)
        {
            if (pos == triggerPosition) continue;

            if (pos.x < 0 || pos.x >= MiningManager.LAYER_WIDTH ||
                pos.y < 0 || pos.y >= MiningManager.LAYER_HEIGHT)
                continue;

            MiningTileData tile = layer.tiles.FirstOrDefault(
                t => t.x == pos.x && t.y == pos.y && 
                t.tileType == TileType.Ore && !t.isMined);
            
            if (tile != null)
            {
                string tileOreId = GetOreIdFromMineralType(tile.mineralType);
                
                // 检查是否是相同类型的矿石
                if (tileOreId == triggerOreId)
                {
                    int damage = effect.value;
                    if (damage <= 0)
                    {
                        damage = bitConfig.strengthBonus;
                    }

                    damages.Add(new BitEffectDamage
                    {
                        position = pos,
                        damage = damage,
                        effectType = DrillBitEffectType.ChainReaction,
                        sourceBitId = bit.bitId
                    });
                }
            }
        }

        return damages;
    }

    /// <summary>
    /// 获取范围内的所有位置
    /// </summary>
    private List<Vector2Int> GetPositionsInRange(Vector2Int center, int range, bool includeDiagonal)
    {
        List<Vector2Int> positions = new List<Vector2Int>();

        for (int dx = -range; dx <= range; dx++)
        {
            for (int dy = -range; dy <= range; dy++)
            {
                if (dx == 0 && dy == 0) continue;

                int distance;
                if (includeDiagonal)
                {
                    // 切比雪夫距离（包括斜角）
                    distance = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
                }
                else
                {
                    // 曼哈顿距离（不包括斜角）
                    distance = Mathf.Abs(dx) + Mathf.Abs(dy);
                }

                if (distance <= range)
                {
                    positions.Add(center + new Vector2Int(dx, dy));
                }
            }
        }

        return positions;
    }

    /// <summary>
    /// 获取矿石ID（从矿物类型）
    /// </summary>
    private string GetOreIdFromMineralType(MineralType mineralType)
    {
        // 简化实现，实际应该从配置管理器获取
        switch (mineralType)
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
/// 钻头后效产生的伤害信息
/// </summary>
[System.Serializable]
public class BitEffectDamage
{
    public Vector2Int position;           // 受伤害的位置
    public int damage;                    // 伤害值
    public DrillBitEffectType effectType; // 效果类型
    public string sourceBitId;           // 来源钻头ID
}
