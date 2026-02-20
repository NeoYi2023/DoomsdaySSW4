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
    /// 平台旋转中心（与挖掘地图层中心一致，默认 9×9 下为 (4,4)）
    /// </summary>
    private static Vector2Int GetPlatformCenter()
    {
        int c = (DrillPlatformData.PLATFORM_SIZE - 1) / 2;
        return new Vector2Int(c, c);
    }

    /// <summary>
    /// 根据当前回合计算挖掘旋转角度（顺时针度数）。第1回合0°，第2回合90°，第3回合180°，第4回合270°，循环。
    /// </summary>
    public static int GetMiningRotationDegreesFromTurn(int currentTurn)
    {
        if (currentTurn <= 0) return 0;
        return ((currentTurn - 1) % 4) * 90;
    }

    /// <summary>
    /// 获取攻击范围内所有格子及其攻击强度
    /// </summary>
    /// <param name="drillData">钻头数据（用于获取永久加成）</param>
    /// <param name="miningRotationDegrees">挖掘旋转角度（0/90/180/270），null 表示不旋转（与平台布局一致）</param>
    /// <returns>格子坐标到攻击信息的映射</returns>
    public Dictionary<Vector2Int, CellAttackInfo> CalculateAttackMap(DrillData drillData = null, int? miningRotationDegrees = null)
    {
        EnsureManagers();
        Dictionary<Vector2Int, CellAttackInfo> attackMap = new Dictionary<Vector2Int, CellAttackInfo>();
        List<PlacedDrillShape> placedShapes = _platformManager.GetPlacedShapes();

        foreach (var placedShape in placedShapes)
        {
            DrillShapeConfig config = _configManager.GetDrillShapeConfig(placedShape.shapeId);
            if (config == null) continue;

            List<Vector2Int> occupiedCells = placedShape.GetOccupiedCells(config);

            foreach (var cell in occupiedCells)
            {
                int attackStrength = CalculateCellAttackStrength(cell, config, placedShape, drillData);
                Vector2Int pos = cell;
                if (miningRotationDegrees.HasValue && miningRotationDegrees.Value != 0)
                {
                    Vector2Int center = GetPlatformCenter();
                    pos = DrillShapeRotator.RotatePointAroundCenter(cell, center, miningRotationDegrees.Value);
                }
                if (!attackMap.ContainsKey(pos))
                {
                    attackMap[pos] = new CellAttackInfo
                    {
                        position = pos,
                        attackStrength = attackStrength,
                        sourceShapeId = placedShape.shapeId,
                        sourceInstanceId = placedShape.instanceId
                    };
                }
            }
        }
        return attackMap;
    }

    /// <summary>
    /// 获取攻击范围（仅坐标）。可传入挖掘旋转角度以得到当回合旋转后的范围。
    /// </summary>
    /// <param name="miningRotationDegrees">挖掘旋转角度（0/90/180/270），null 表示不旋转</param>
    public HashSet<Vector2Int> GetAttackRange(int? miningRotationDegrees = null)
    {
        EnsureManagers();
        HashSet<Vector2Int> cells = _platformManager.GetAllOccupiedCells();
        if (!miningRotationDegrees.HasValue || miningRotationDegrees.Value == 0)
            return cells;
        Vector2Int center = GetPlatformCenter();
        HashSet<Vector2Int> rotated = new HashSet<Vector2Int>();
        foreach (var cell in cells)
            rotated.Add(DrillShapeRotator.RotatePointAroundCenter(cell, center, miningRotationDegrees.Value));
        return rotated;
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
    /// 计算指定格子的攻击强度（考虑钻头加成）
    /// </summary>
    /// <param name="position">格子位置</param>
    /// <param name="shapeConfig">造型配置</param>
    /// <param name="placedShape">放置的造型实例</param>
    /// <param name="drillData">钻头数据</param>
    /// <param name="targetOreType">目标矿石类型（可选）</param>
    /// <returns>攻击强度</returns>
    public int CalculateCellAttackStrength(
        Vector2Int position, 
        DrillShapeConfig shapeConfig, 
        PlacedDrillShape placedShape, 
        DrillData drillData = null, 
        string targetOreType = null)
    {
        // 计算基础强度（来自造型）
        float baseStrength = CalculateShapeAttackStrength(shapeConfig, placedShape, drillData, targetOreType);
        
        // 获取影响该格子的钻头
        List<PlacedDrillBit> affectingBits = _platformManager.GetBitsAffectingCell(position);
        
        // 计算钻头加成
        int totalBonus = 0;
        float totalMultiplier = 1f;
        foreach (var bit in affectingBits)
        {
            DrillBitConfig bitConfig = _configManager?.GetDrillBitConfig(bit.bitId);
            if (bitConfig != null)
            {
                totalBonus += bitConfig.strengthBonus;
                totalMultiplier *= bitConfig.strengthMultiplier;
            }
        }
        
        // 最终强度 = (基础强度 + 加成) × 倍率
        return Mathf.RoundToInt((baseStrength + totalBonus) * totalMultiplier);
    }

    /// <summary>
    /// 计算对特定矿石的攻击强度（考虑矿石类型触发的特性和钻头加成）
    /// </summary>
    /// <param name="position">攻击位置（若启用挖掘旋转则为旋转后的挖掘坐标）</param>
    /// <param name="oreType">矿石类型</param>
    /// <param name="drillData">钻头数据</param>
    /// <param name="miningRotationDegrees">挖掘旋转角度，非 null 时将 position 逆旋转到平台坐标再查造型</param>
    /// <returns>攻击强度</returns>
    public int CalculateAttackStrengthForOre(Vector2Int position, string oreType, DrillData drillData = null, int? miningRotationDegrees = null)
    {
        EnsureManagers();

        Vector2Int platformPos = position;
        if (miningRotationDegrees.HasValue && miningRotationDegrees.Value != 0)
        {
            Vector2Int center = GetPlatformCenter();
            int inverseDegrees = (360 - miningRotationDegrees.Value) % 360;
            platformPos = DrillShapeRotator.RotatePointAroundCenter(position, center, inverseDegrees);
        }

        PlacedDrillShape shape = _platformManager.GetShapeAtPosition(platformPos);
        if (shape == null) return 0;

        DrillShapeConfig config = _configManager.GetDrillShapeConfig(shape.shapeId);
        if (config == null) return 0;

        return CalculateCellAttackStrength(platformPos, config, shape, drillData, oreType);
    }

    /// <summary>
    /// 获取攻击信息列表（用于挖矿动画等）
    /// </summary>
    /// <param name="drillData">钻头数据</param>
    /// <param name="miningRotationDegrees">挖掘旋转角度，null 表示不旋转</param>
    public List<AttackedTileInfo> GetAttackedTileInfoList(DrillData drillData = null, int? miningRotationDegrees = null)
    {
        List<AttackedTileInfo> result = new List<AttackedTileInfo>();
        Dictionary<Vector2Int, CellAttackInfo> attackMap = CalculateAttackMap(drillData, miningRotationDegrees);
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
    /// <param name="miningRotationDegrees">挖掘旋转角度，null 表示不旋转</param>
    public bool IsInAttackRange(Vector2Int position, int? miningRotationDegrees = null)
    {
        EnsureManagers();
        if (!miningRotationDegrees.HasValue || miningRotationDegrees.Value == 0)
            return _platformManager.IsCellOccupied(position);
        return GetAttackRange(miningRotationDegrees).Contains(position);
    }

    /// <summary>
    /// 获取攻击范围的边界框
    /// </summary>
    /// <param name="miningRotationDegrees">挖掘旋转角度，null 表示不旋转</param>
    public (Vector2Int min, Vector2Int max) GetAttackBounds(int? miningRotationDegrees = null)
    {
        HashSet<Vector2Int> cells = GetAttackRange(miningRotationDegrees);
        
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
