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
    /// 获取旋转挖掘中心点（从平台数据读取，默认 (4,5)）
    /// </summary>
    private Vector2Int GetPlatformCenter()
    {
        if (_platformManager != null)
        {
            DrillPlatformData data = _platformManager.GetPlatformData();
            if (data != null)
            {
                return data.rotationCenter;
            }
        }
        return new Vector2Int(DrillPlatformData.PLATFORM_SIZE / 2, DrillPlatformData.PLATFORM_SIZE / 2);
    }

    /// <summary>
    /// 圆环扫掠匹配容差（已废弃：半径现按格子数/六边形圈数定义，参见 GetGridRingDistance）
    /// </summary>
    [System.Obsolete("半径已改为格子数定义，不再使用欧氏容差")]
    public const float RADIUS_TOLERANCE = 0.71f;

    /// <summary>
    /// 计算格子相对中心的「圈数」（半径，单位：格子数）。与 PlacedDrillShape 的 odd-r 约定一致。
    /// 0=中心格，1=第一圈 6 格，2=第二圈 12 格，以此类推。
    /// </summary>
    /// <param name="center">旋转中心 (col, row)</param>
    /// <param name="cell">待判定格子 (col, row)</param>
    /// <returns>六边形步数/圈数</returns>
    public static int GetGridRingDistance(Vector2Int center, Vector2Int cell)
    {
        // odd-r: (x,y) = (col, row) → axial q = col - (row - (row & 1)) / 2, r = row
        int q0 = center.x - (center.y - (center.y & 1)) / 2;
        int r0 = center.y;
        int q1 = cell.x - (cell.y - (cell.y & 1)) / 2;
        int r1 = cell.y;
        int dq = q1 - q0;
        int dr = r1 - r0;
        // 六边形轴向距离: (|dq| + |dr| + |dq+dr|) / 2
        int dqpr = dq + dr;
        return (Math.Abs(dq) + Math.Abs(dr) + Math.Abs(dqpr)) / 2;
    }

    /// <summary>
    /// 判断旋转中心所在行是否为「偶数行」（本行格子数比上一行、下一行都少 1）。
    /// 与 HexLayoutGroup 约定一致：行号 0,2,4,… 用 countEven，行号 1,3,5,… 用 countOdd。
    /// 若每行格子数相同（如矩形平台 countEven==countOdd），则永远返回 false。
    /// </summary>
    /// <param name="centerY">中心点行号</param>
    /// <param name="countEven">偶数行号（0,2,4,…）每行格子数</param>
    /// <param name="countOdd">奇数行号（1,3,5,…）每行格子数</param>
    public static bool IsCenterRowEvenRow(int centerY, int countEven, int countOdd)
    {
        int countThisRow = (centerY % 2 == 0) ? countEven : countOdd;
        int countAbove = ((centerY - 1) % 2 == 0) ? countEven : countOdd;
        int countBelow = ((centerY + 1) % 2 == 0) ? countEven : countOdd;
        return countThisRow < countAbove && countThisRow < countBelow;
    }

    /// <summary>
    /// [Obsolete] 根据当前回合计算挖掘旋转角度（顺时针度数）。已被圆环扫掠系统替代。
    /// </summary>
    [System.Obsolete("使用圆环扫掠攻击系统替代，参见 CalculateCircularSweepAttackMap")]
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
            platformPos = HexRotationLookupTable.GetInverseRotated(center, position.x, position.y, inverseDegrees);
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

    /// <summary>
    /// 每 60° 扫掠：以平台相对旋转中心为起点，在 0°/60°/120°/180°/240°/300° 各计算一次攻击图。
    /// 用于「每 60° 计算一次挖矿」逻辑及动画按角度触发。
    /// </summary>
    /// <param name="drillData">钻头数据（用于获取永久加成）</param>
    /// <param name="mapWidth">矿石地图宽度</param>
    /// <param name="mapHeight">矿石地图高度</param>
    /// <returns>每 60° 扫掠结果（6 个角度的 attackMap + angleToTargets）</returns>
    public CircularSweepResultPer60 CalculateCircularSweepAttackMapPer60(DrillData drillData = null, int mapWidth = 9, int mapHeight = 11)
    {
        EnsureManagers();

        CircularSweepResultPer60 result = new CircularSweepResultPer60();
        Vector2Int center = GetPlatformCenter();
        List<PlacedDrillShape> placedShapes = _platformManager.GetPlacedShapes();

        int[] angles = { 0, 60, 120, 180, 240, 300 };

        foreach (int angle in angles)
        {
            Dictionary<Vector2Int, CellAttackInfo> attackMapAtAngle = BuildAttackMapAtAngle(placedShapes, center, angle, drillData);
            result.attackMapsByAngle[angle] = attackMapAtAngle;

            foreach (var kvp in attackMapAtAngle)
            {
                Vector2Int pos = kvp.Key;
                if (pos.x >= 0 && pos.x < mapWidth && pos.y >= 0 && pos.y < mapHeight)
                {
                    result.sweepRange.Add(pos);
                    float angleKey = (float)angle;
                    if (!result.angleToTargets.ContainsKey(angleKey))
                        result.angleToTargets[angleKey] = new List<Vector2Int>();
                    if (!result.angleToTargets[angleKey].Contains(pos))
                        result.angleToTargets[angleKey].Add(pos);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 在指定角度下构建攻击图（平台格绕旋转中心旋转后落点，同格累加攻击力）
    /// </summary>
    private Dictionary<Vector2Int, CellAttackInfo> BuildAttackMapAtAngle(
        List<PlacedDrillShape> placedShapes,
        Vector2Int center,
        int angle,
        DrillData drillData)
    {
        var attackMap = new Dictionary<Vector2Int, CellAttackInfo>();

        foreach (var placedShape in placedShapes)
        {
            DrillShapeConfig config = _configManager.GetDrillShapeConfig(placedShape.shapeId);
            if (config == null) continue;

            List<Vector2Int> occupiedCells = placedShape.GetOccupiedCells(config);
            foreach (var cell in occupiedCells)
            {
                int strength = CalculateCellAttackStrength(cell, config, placedShape, drillData);
                Vector2Int pos = HexRotationLookupTable.GetRotated(center, cell.x, cell.y, angle);

                if (attackMap.TryGetValue(pos, out CellAttackInfo existing))
                {
                    existing.attackStrength += strength;
                }
                else
                {
                    attackMap[pos] = new CellAttackInfo
                    {
                        position = pos,
                        attackStrength = strength,
                        sourceShapeId = placedShape.shapeId,
                        sourceInstanceId = placedShape.instanceId
                    };
                }
            }
        }

        return attackMap;
    }

    /// <summary>
    /// 计算圆环扫掠攻击映射。半径单位：格子数/六边形圈数；同圈数的地图格与钻头格匹配。
    /// </summary>
    [System.Obsolete("请使用 CalculateCircularSweepAttackMapPer60，按每 60° 计算并以相对旋转中心为起点")]
    public CircularSweepResult CalculateCircularSweepAttackMap(DrillData drillData = null, int mapWidth = 9, int mapHeight = 11)
    {
        EnsureManagers();

        CircularSweepResult result = new CircularSweepResult();
        Vector2Int center = GetPlatformCenter();
        List<PlacedDrillShape> placedShapes = _platformManager.GetPlacedShapes();

        // 收集所有钻头占用格的圈数（格子数半径）和攻击强度
        var drillCellInfos = new List<(Vector2Int platformPos, int ring, int strength, string shapeId, string instanceId)>();

        foreach (var placedShape in placedShapes)
        {
            DrillShapeConfig config = _configManager.GetDrillShapeConfig(placedShape.shapeId);
            if (config == null) continue;

            List<Vector2Int> occupiedCells = placedShape.GetOccupiedCells(config);
            foreach (var cell in occupiedCells)
            {
                int ring = GetGridRingDistance(center, cell);
                int strength = CalculateCellAttackStrength(cell, config, placedShape, drillData);
                drillCellInfos.Add((cell, ring, strength, placedShape.shapeId, placedShape.instanceId));
                result.attackRadii.Add(ring);
            }
        }

        // 遍历矿石地图，同圈数则命中（半径=格子数）
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                Vector2Int mapPos = new Vector2Int(x, y);
                int mapRing = GetGridRingDistance(center, mapPos);

                int totalStrength = 0;
                string bestShapeId = null;
                string bestInstanceId = null;
                bool hit = false;

                foreach (var info in drillCellInfos)
                {
                    if (mapRing == info.ring)
                    {
                        totalStrength += info.strength;
                        if (bestShapeId == null)
                        {
                            bestShapeId = info.shapeId;
                            bestInstanceId = info.instanceId;
                        }
                        hit = true;
                    }
                }

                if (hit)
                {
                    result.sweepRange.Add(mapPos);
                    result.attackMap[mapPos] = new CellAttackInfo
                    {
                        position = mapPos,
                        attackStrength = totalStrength,
                        sourceShapeId = bestShapeId,
                        sourceInstanceId = bestInstanceId
                    };

                    if (mapPos == center)
                    {
                        for (int k = 0; k < 6; k++)
                        {
                            float angle = k * 60f;
                            if (!result.angleToTargets.ContainsKey(angle))
                                result.angleToTargets[angle] = new List<Vector2Int>();
                            if (!result.angleToTargets[angle].Contains(center))
                                result.angleToTargets[angle].Add(center);
                        }
                    }
                    else
                    {
                        float angle = GetCellAngleFromCenter(mapPos, center);
                        float roundedAngle = Mathf.Round(angle);
                        if (!result.angleToTargets.ContainsKey(roundedAngle))
                            result.angleToTargets[roundedAngle] = new List<Vector2Int>();
                        result.angleToTargets[roundedAngle].Add(mapPos);
                    }
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 获取圆环扫掠覆盖的所有格子坐标（半径单位：格子数/六边形圈数）。
    /// 范围 = 以旋转中心为圆心、半径 R 的六边形圆盘，R = 所有钻头占用格相对中心的最大圈数。
    /// </summary>
    public HashSet<Vector2Int> GetCircularSweepRange(int mapWidth = 9, int mapHeight = 11)
    {
        EnsureManagers();

        Vector2Int center = GetPlatformCenter();
        int maxRing = 0;
        List<PlacedDrillShape> placedShapes = _platformManager.GetPlacedShapes();
        foreach (var placedShape in placedShapes)
        {
            DrillShapeConfig config = _configManager.GetDrillShapeConfig(placedShape.shapeId);
            if (config == null) continue;
            foreach (var cell in placedShape.GetOccupiedCells(config))
            {
                int ring = GetGridRingDistance(center, cell);
                if (ring > maxRing) maxRing = ring;
            }
        }

        HashSet<Vector2Int> range = new HashSet<Vector2Int>();
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (GetGridRingDistance(center, pos) <= maxRing)
                    range.Add(pos);
            }
        }
        return range;
    }

    /// <summary>
    /// 计算格子相对于旋转中心的角度（0~360度，顺时针，12点钟方向为0度）
    /// </summary>
    public float GetCellAngleFromCenter(Vector2Int cell)
    {
        return GetCellAngleFromCenter(cell, GetPlatformCenter());
    }

    /// <summary>
    /// 计算格子相对于指定中心的角度（0~360度，顺时针，12点钟方向为0度）
    /// </summary>
    public static float GetCellAngleFromCenter(Vector2Int cell, Vector2Int center)
    {
        float dx = cell.x - center.x;
        float dy = cell.y - center.y;
        // Atan2(dx, dy) 使得 y+ 方向为 0 度, 顺时针增加
        float angle = Mathf.Atan2(dx, dy) * Mathf.Rad2Deg;
        if (angle < 0f) angle += 360f;
        return angle;
    }

    /// <summary>
    /// 为圆环扫掠计算对特定矿石的攻击强度（考虑矿石类型触发的特性）。
    /// 半径单位：格子数/六边形圈数；累加所有与 mapPosition 同圈数的钻头格攻击强度。
    /// </summary>
    public int CalculateCircularSweepStrengthForOre(Vector2Int mapPosition, string oreType, DrillData drillData = null, int mapWidth = 9, int mapHeight = 11)
    {
        EnsureManagers();

        Vector2Int center = GetPlatformCenter();
        int mapRing = GetGridRingDistance(center, mapPosition);

        List<PlacedDrillShape> placedShapes = _platformManager.GetPlacedShapes();
        int totalStrength = 0;

        foreach (var placedShape in placedShapes)
        {
            DrillShapeConfig config = _configManager.GetDrillShapeConfig(placedShape.shapeId);
            if (config == null) continue;

            List<Vector2Int> occupiedCells = placedShape.GetOccupiedCells(config);
            foreach (var cell in occupiedCells)
            {
                int drillRing = GetGridRingDistance(center, cell);
                if (mapRing == drillRing)
                    totalStrength += CalculateCellAttackStrength(cell, config, placedShape, drillData, oreType);
            }
        }

        return totalStrength;
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

/// <summary>
/// 圆环扫掠攻击结果
/// </summary>
[Serializable]
public class CircularSweepResult
{
    /// <summary>格子坐标 -> 累加后的攻击信息</summary>
    public Dictionary<Vector2Int, CellAttackInfo> attackMap = new Dictionary<Vector2Int, CellAttackInfo>();

    /// <summary>角度(0~360) -> 该角度触发攻击的目标格列表（用于动画按角度触发）</summary>
    public Dictionary<float, List<Vector2Int>> angleToTargets = new Dictionary<float, List<Vector2Int>>();

    /// <summary>所有钻头格的扫掠半径集合（Obsolete 方法中为格子数/六边形圈数）</summary>
    public HashSet<float> attackRadii = new HashSet<float>();

    /// <summary>扫掠范围内所有被覆盖的格子坐标</summary>
    public HashSet<Vector2Int> sweepRange = new HashSet<Vector2Int>();
}

/// <summary>
/// 每 60° 扫掠攻击结果（0/60/120/180/240/300 各一张攻击图 + 角度→目标格，用于动画）
/// </summary>
[Serializable]
public class CircularSweepResultPer60
{
    /// <summary>角度(0/60/120/180/240/300) -> 该角度下的攻击图</summary>
    public Dictionary<int, Dictionary<Vector2Int, CellAttackInfo>> attackMapsByAngle = new Dictionary<int, Dictionary<Vector2Int, CellAttackInfo>>();

    /// <summary>角度(0/60/120/180/240/300，float) -> 该角度触发攻击的目标格列表（用于动画按角度触发）</summary>
    public Dictionary<float, List<Vector2Int>> angleToTargets = new Dictionary<float, List<Vector2Int>>();

    /// <summary>扫掠范围内所有被覆盖的格子坐标（6 角度并集）</summary>
    public HashSet<Vector2Int> sweepRange = new HashSet<Vector2Int>();
}
