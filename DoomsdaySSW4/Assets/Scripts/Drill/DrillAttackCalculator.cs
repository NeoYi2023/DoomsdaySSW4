using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 钻头攻击计算器：计算攻击范围和每个格子的攻击强度
/// </summary>
public class DrillAttackCalculator : MonoBehaviour
{
    private static DrillAttackCalculator _instance;
    public static DrillAttackCalculator Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("DrillAttackCalculator");
                _instance = go.AddComponent<DrillAttackCalculator>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private DrillPlatformManager _platformManager;
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
        _platformManager = DrillPlatformManager.Instance;
        _configManager = ConfigManager.Instance;
    }

    /// <summary>
    /// 获取攻击范围内所有格子及其攻击强度
    /// </summary>
    /// <param name="drillData">钻头数据（用于获取永久加成）</param>
    /// <returns>格子坐标到攻击信息的映射</returns>
    public Dictionary<Vector2Int, CellAttackInfo> CalculateAttackMap(DrillData drillData = null)
    {
        EnsureManagers();
        
        Dictionary<Vector2Int, CellAttackInfo> attackMap = new Dictionary<Vector2Int, CellAttackInfo>();
        
        List<PlacedDrillShape> placedShapes = _platformManager.GetPlacedShapes();
        
        foreach (var placedShape in placedShapes)
        {
            DrillShapeConfig config = _configManager.GetDrillShapeConfig(placedShape.shapeId);
            // #region agent log
            try { System.IO.File.AppendAllText(@"e:\Work\Cursor\DoomsdaySSW4\.cursor\debug.log", $"{{\"timestamp\":\"{System.DateTime.Now:o}\",\"location\":\"DrillAttackCalculator:63\",\"hypothesisId\":\"I\",\"message\":\"GetDrillShapeConfig result\",\"data\":{{\"shapeId\":\"{placedShape.shapeId}\",\"config_is_null\":{(config == null).ToString().ToLower()}}}}}\n"); } catch { }
            // #endregion
            if (config == null) continue;
            
            List<Vector2Int> occupiedCells = placedShape.GetOccupiedCells(config);
            
            // 计算该造型的攻击强度
            float attackStrength = CalculateShapeAttackStrength(config, placedShape, drillData);
            
            foreach (var cell in occupiedCells)
            {
                if (!attackMap.ContainsKey(cell))
                {
                    attackMap[cell] = new CellAttackInfo
                    {
                        position = cell,
                        attackStrength = Mathf.RoundToInt(attackStrength),
                        sourceShapeId = placedShape.shapeId,
                        sourceInstanceId = placedShape.instanceId
                    };
                }
                // 注意：根据规则不允许重叠，所以这里不会有重复的格子
            }
        }
        
        return attackMap;
    }

    /// <summary>
    /// 获取攻击范围（仅坐标）
    /// </summary>
    public HashSet<Vector2Int> GetAttackRange()
    {
        EnsureManagers();
        return _platformManager.GetAllOccupiedCells();
    }

    /// <summary>
    /// 计算单个造型的攻击强度
    /// </summary>
    /// <param name="config">造型配置</param>
    /// <param name="placedShape">放置实例</param>
    /// <param name="drillData">钻头数据（可选）</param>
    /// <param name="targetOreType">目标矿石类型（可选，用于条件特性）</param>
    /// <returns>计算后的攻击强度</returns>
    public float CalculateShapeAttackStrength(DrillShapeConfig config, PlacedDrillShape placedShape, DrillData drillData = null, string targetOreType = null)
    {
        if (config == null) return 0f;
        
        float baseStrength = config.baseAttackStrength;
        float traitMultiplier = 1f;
        float traitAddition = 0f;
        
        // 计算造型特性加成
        if (config.traits != null)
        {
            foreach (var trait in config.traits)
            {
                // 检查特性是否激活
                if (!IsTraitActive(trait, placedShape, targetOreType)) continue;
                
                switch (trait.effectType)
                {
                    case "attack_multiplier":
                        traitMultiplier *= trait.effectValue;
                        break;
                    case "attack_add":
                        traitAddition += trait.effectValue;
                        break;
                }
            }
        }
        
        // 计算永久加成
        float permanentMultiplier = 1f;
        int permanentAddition = 0;
        
        if (drillData != null)
        {
            permanentMultiplier = drillData.permanentAttackMultiplier;
            permanentAddition = drillData.permanentStrengthBonus;
        }
        
        // 最终攻击强度 = (基础攻击 + 固定加成) × 造型特性倍率 × 永久倍率 + 永久固定加成
        float finalStrength = (baseStrength + traitAddition) * traitMultiplier * permanentMultiplier + permanentAddition;
        
        return finalStrength;
    }

    /// <summary>
    /// 检查特性是否激活
    /// </summary>
    private bool IsTraitActive(ShapeTraitConfig trait, PlacedDrillShape placedShape, string targetOreType)
    {
        if (trait == null) return false;
        
        string condition = trait.triggerCondition;
        
        if (string.IsNullOrEmpty(condition) || condition == "always")
        {
            return true;
        }
        
        // 检查是否在已激活列表中
        if (placedShape.activeTraits != null && placedShape.activeTraits.Contains(trait.traitId))
        {
            return true;
        }
        
        // 检查矿石类型条件
        if (condition.StartsWith("ore_type:"))
        {
            string requiredType = condition.Substring("ore_type:".Length);
            return targetOreType != null && targetOreType.ToLower() == requiredType.ToLower();
        }
        
        return false;
    }

    /// <summary>
    /// 计算对特定矿石的攻击强度（考虑矿石类型触发的特性）
    /// </summary>
    /// <param name="position">攻击位置</param>
    /// <param name="oreType">矿石类型</param>
    /// <param name="drillData">钻头数据</param>
    /// <returns>攻击强度</returns>
    public int CalculateAttackStrengthForOre(Vector2Int position, string oreType, DrillData drillData = null)
    {
        EnsureManagers();
        
        PlacedDrillShape shape = _platformManager.GetShapeAtPosition(position);
        if (shape == null) return 0;
        
        DrillShapeConfig config = _configManager.GetDrillShapeConfig(shape.shapeId);
        if (config == null) return 0;
        
        float strength = CalculateShapeAttackStrength(config, shape, drillData, oreType);
        return Mathf.RoundToInt(strength);
    }

    /// <summary>
    /// 获取攻击信息列表（用于挖矿动画等）
    /// </summary>
    /// <param name="drillData">钻头数据</param>
    /// <returns>被攻击的格子信息列表</returns>
    public List<AttackedTileInfo> GetAttackedTileInfoList(DrillData drillData = null)
    {
        List<AttackedTileInfo> result = new List<AttackedTileInfo>();
        
        Dictionary<Vector2Int, CellAttackInfo> attackMap = CalculateAttackMap(drillData);
        
        foreach (var kvp in attackMap)
        {
            result.Add(new AttackedTileInfo
            {
                position = kvp.Key,
                attackStrength = kvp.Value.attackStrength
            });
        }
        
        return result;
    }

    /// <summary>
    /// 检查指定位置是否在攻击范围内
    /// </summary>
    public bool IsInAttackRange(Vector2Int position)
    {
        EnsureManagers();
        return _platformManager.IsCellOccupied(position);
    }

    /// <summary>
    /// 获取攻击范围的边界框
    /// </summary>
    public (Vector2Int min, Vector2Int max) GetAttackBounds()
    {
        HashSet<Vector2Int> cells = GetAttackRange();
        
        if (cells.Count == 0)
        {
            return (Vector2Int.zero, Vector2Int.zero);
        }
        
        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;
        
        foreach (var cell in cells)
        {
            minX = Mathf.Min(minX, cell.x);
            minY = Mathf.Min(minY, cell.y);
            maxX = Mathf.Max(maxX, cell.x);
            maxY = Mathf.Max(maxY, cell.y);
        }
        
        return (new Vector2Int(minX, minY), new Vector2Int(maxX, maxY));
    }

    private void EnsureManagers()
    {
        if (_platformManager == null)
        {
            _platformManager = DrillPlatformManager.Instance;
        }
        if (_configManager == null)
        {
            _configManager = ConfigManager.Instance;
        }
    }
}

/// <summary>
/// 格子攻击信息
/// </summary>
[Serializable]
public class CellAttackInfo
{
    public Vector2Int position;
    public int attackStrength;
    public string sourceShapeId;
    public string sourceInstanceId;
}
