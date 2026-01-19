using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 钻机平台管理器：管理9x9平台上钻头造型的放置、移除、旋转等操作
/// </summary>
public class DrillPlatformManager : MonoBehaviour
{
    private static DrillPlatformManager _instance;
    public static DrillPlatformManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("DrillPlatformManager");
                _instance = go.AddComponent<DrillPlatformManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // 当前钻机平台数据
    private DrillPlatformData _platformData;
    
    // 配置管理器引用
    private ConfigManager _configManager;

    // 事件
    public UnityEvent OnPlatformChanged = new UnityEvent();
    public UnityEvent<PlacedDrillShape> OnShapePlaced = new UnityEvent<PlacedDrillShape>();
    public UnityEvent<PlacedDrillShape> OnShapeRemoved = new UnityEvent<PlacedDrillShape>();
    public UnityEvent<PlacedDrillShape> OnShapeRotated = new UnityEvent<PlacedDrillShape>();

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
    /// 初始化平台数据（游戏开始时调用）
    /// </summary>
    /// <param name="shipId">船只ID，用于获取初始造型配置</param>
    public void InitializePlatform(string shipId = "default_ship")
    {
        // #region agent log
        try { System.IO.File.AppendAllText(@"e:\Work\Cursor\DoomsdaySSW4\.cursor\debug.log", $"{{\"timestamp\":\"{System.DateTime.Now:o}\",\"location\":\"DrillPlatformManager:62\",\"hypothesisId\":\"F\",\"message\":\"InitializePlatform called\",\"data\":{{\"shipId\":\"{shipId}\",\"instanceId\":\"{GetInstanceID()}\"}}}}\n"); } catch { }
        // #endregion
        if (_configManager == null)
        {
            _configManager = ConfigManager.Instance;
        }

        _platformData = new DrillPlatformData();
        
        // 从船只配置获取初始可用造型列表
        List<string> initialShapeIds = _configManager.GetInitialShapeIds(shipId);
        foreach (string shapeId in initialShapeIds)
        {
            _platformData.availableShapeIds.Add(shapeId);
        }

        Debug.Log($"钻机平台初始化，船只: {shipId}，初始造型库存: {_platformData.availableShapeIds.Count} 个");
        
        // 从船只初始钻头配置获取已放置的造型
        List<ShipInitialDrillConfig> initialDrills = _configManager.GetShipInitialDrillConfigs(shipId);
        
        if (initialDrills.Count > 0)
        {
            // 按配置放置初始造型
            foreach (var drillConfig in initialDrills)
            {
                PlaceInitialShape(drillConfig);
            }
            Debug.Log($"已根据配置放置 {initialDrills.Count} 个初始钻头");
        }
        else if (_platformData.availableShapeIds.Count > 0)
        {
            // 向后兼容：如果没有配置初始放置，自动放置第一个造型在中心
            string firstShapeId = _platformData.availableShapeIds[0];
            Vector2Int centerPosition = new Vector2Int(4, 4);
            TryPlaceShape(firstShapeId, centerPosition, 0);
            Debug.Log($"未配置初始钻头，自动放置第一个造型在中心");
        }
        
        // #region agent log
        try { System.IO.File.AppendAllText(@"e:\Work\Cursor\DoomsdaySSW4\.cursor\debug.log", $"{{\"timestamp\":\"{System.DateTime.Now:o}\",\"location\":\"DrillPlatformManager:100\",\"hypothesisId\":\"F\",\"message\":\"InitializePlatform finished\",\"data\":{{\"instanceId\":\"{GetInstanceID()}\",\"placedShapes_count\":{_platformData?.placedShapes?.Count ?? -1},\"availableShapeIds_count\":{_platformData?.availableShapeIds?.Count ?? -1}}}}}\n"); } catch { }
        // #endregion
        OnPlatformChanged?.Invoke();
    }
    
    /// <summary>
    /// 放置初始造型（不检查库存，用于初始化）
    /// </summary>
    private void PlaceInitialShape(ShipInitialDrillConfig config)
    {
        DrillShapeConfig shapeConfig = _configManager.GetDrillShapeConfig(config.shapeId);
        if (shapeConfig == null)
        {
            Debug.LogWarning($"找不到造型配置: {config.shapeId}，跳过放置");
            return;
        }

        Vector2Int position = config.GetPosition();
        int rotation = config.rotation;

        // 创建放置实例
        PlacedDrillShape placedShape = new PlacedDrillShape(config.shapeId, position, rotation);
        List<Vector2Int> occupiedCells = placedShape.GetOccupiedCells(shapeConfig);

        // 检查边界
        if (!DrillPlatformData.AreAllWithinBounds(occupiedCells))
        {
            Debug.LogWarning($"初始造型 {config.shapeId} 在位置 {position} 超出边界，跳过放置");
            return;
        }

        // 检查碰撞
        HashSet<Vector2Int> existingCells = _platformData.GetAllOccupiedCells(_configManager.GetShapeConfigDelegate());
        foreach (var cell in occupiedCells)
        {
            if (existingCells.Contains(cell))
            {
                Debug.LogWarning($"初始造型 {config.shapeId} 在位置 {position} 与已有造型重叠，跳过放置");
                return;
            }
        }

        // 放置成功
        _platformData.placedShapes.Add(placedShape);
        
        // 从库存中移除（如果在库存中）
        _platformData.availableShapeIds.Remove(config.shapeId);

        Debug.Log($"初始造型 {config.shapeId} 放置成功，位置: {position}，旋转: {rotation}");
    }

    /// <summary>
    /// 获取当前平台数据
    /// </summary>
    public DrillPlatformData GetPlatformData()
    {
        return _platformData;
    }

    /// <summary>
    /// 设置平台数据（用于加载存档）
    /// </summary>
    public void SetPlatformData(DrillPlatformData data)
    {
        _platformData = data ?? new DrillPlatformData();
        OnPlatformChanged?.Invoke();
    }

    /// <summary>
    /// 尝试放置造型
    /// </summary>
    /// <param name="shapeId">造型ID</param>
    /// <param name="position">放置位置</param>
    /// <param name="rotation">旋转角度</param>
    /// <returns>放置结果</returns>
    public PlaceResult TryPlaceShape(string shapeId, Vector2Int position, int rotation = 0)
    {
        if (_platformData == null)
        {
            return new PlaceResult { success = false, errorMessage = "平台未初始化" };
        }

        // 检查造型是否在库存中
        if (!_platformData.availableShapeIds.Contains(shapeId))
        {
            return new PlaceResult { success = false, errorMessage = "造型不在库存中" };
        }

        DrillShapeConfig config = _configManager.GetDrillShapeConfig(shapeId);
        if (config == null)
        {
            return new PlaceResult { success = false, errorMessage = "找不到造型配置" };
        }

        // 创建临时放置实例来计算占用格子
        PlacedDrillShape tempShape = new PlacedDrillShape(shapeId, position, rotation);
        List<Vector2Int> occupiedCells = tempShape.GetOccupiedCells(config);

        // 检查边界
        if (!DrillPlatformData.AreAllWithinBounds(occupiedCells))
        {
            return new PlaceResult { success = false, errorMessage = "造型超出平台边界" };
        }

        // 检查碰撞
        HashSet<Vector2Int> existingCells = _platformData.GetAllOccupiedCells(_configManager.GetShapeConfigDelegate());
        foreach (var cell in occupiedCells)
        {
            if (existingCells.Contains(cell))
            {
                return new PlaceResult { success = false, errorMessage = "与现有造型重叠" };
            }
        }

        // 放置成功
        _platformData.placedShapes.Add(tempShape);
        _platformData.availableShapeIds.Remove(shapeId);

        OnShapePlaced?.Invoke(tempShape);
        OnPlatformChanged?.Invoke();

        Debug.Log($"造型 {shapeId} 放置成功，位置: {position}，旋转: {rotation}");
        return new PlaceResult { success = true, placedShape = tempShape };
    }

    /// <summary>
    /// 移除已放置的造型
    /// </summary>
    /// <param name="instanceId">实例ID</param>
    /// <returns>是否成功移除</returns>
    public bool RemoveShape(string instanceId)
    {
        if (_platformData == null) return false;

        PlacedDrillShape shape = _platformData.FindPlacedShapeByInstanceId(instanceId);
        if (shape == null) return false;

        _platformData.placedShapes.Remove(shape);
        
        // 将造型放回库存
        if (!_platformData.availableShapeIds.Contains(shape.shapeId))
        {
            _platformData.availableShapeIds.Add(shape.shapeId);
        }

        OnShapeRemoved?.Invoke(shape);
        OnPlatformChanged?.Invoke();

        Debug.Log($"造型 {shape.shapeId} 已移除");
        return true;
    }

    /// <summary>
    /// 旋转已放置的造型
    /// </summary>
    /// <param name="instanceId">实例ID</param>
    /// <param name="clockwise">是否顺时针</param>
    /// <returns>旋转结果</returns>
    public PlaceResult TryRotateShape(string instanceId, bool clockwise = true)
    {
        if (_platformData == null)
        {
            return new PlaceResult { success = false, errorMessage = "平台未初始化" };
        }

        PlacedDrillShape shape = _platformData.FindPlacedShapeByInstanceId(instanceId);
        if (shape == null)
        {
            return new PlaceResult { success = false, errorMessage = "找不到指定造型" };
        }

        DrillShapeConfig config = _configManager.GetDrillShapeConfig(shape.shapeId);
        if (config == null)
        {
            return new PlaceResult { success = false, errorMessage = "找不到造型配置" };
        }

        // 计算新的旋转角度
        int newRotation = clockwise ? (shape.rotation + 90) % 360 : (shape.rotation + 270) % 360;

        // 创建临时实例检查碰撞
        PlacedDrillShape tempShape = shape.Clone();
        tempShape.rotation = newRotation;
        List<Vector2Int> newOccupiedCells = tempShape.GetOccupiedCells(config);

        // 检查边界
        if (!DrillPlatformData.AreAllWithinBounds(newOccupiedCells))
        {
            return new PlaceResult { success = false, errorMessage = "旋转后超出平台边界" };
        }

        // 检查碰撞（排除自身）
        foreach (var placedShape in _platformData.placedShapes)
        {
            if (placedShape.instanceId == instanceId) continue;

            DrillShapeConfig otherConfig = _configManager.GetDrillShapeConfig(placedShape.shapeId);
            if (otherConfig == null) continue;

            List<Vector2Int> otherCells = placedShape.GetOccupiedCells(otherConfig);
            foreach (var cell in newOccupiedCells)
            {
                if (otherCells.Contains(cell))
                {
                    return new PlaceResult { success = false, errorMessage = "旋转后与其他造型重叠" };
                }
            }
        }

        // 旋转成功
        shape.rotation = newRotation;

        OnShapeRotated?.Invoke(shape);
        OnPlatformChanged?.Invoke();

        Debug.Log($"造型 {shape.shapeId} 旋转成功，新角度: {newRotation}");
        return new PlaceResult { success = true, placedShape = shape };
    }

    /// <summary>
    /// 尝试移动已放置的造型
    /// </summary>
    /// <param name="instanceId">实例ID</param>
    /// <param name="newPosition">新位置</param>
    /// <returns>移动结果</returns>
    public PlaceResult TryMoveShape(string instanceId, Vector2Int newPosition)
    {
        if (_platformData == null)
        {
            return new PlaceResult { success = false, errorMessage = "平台未初始化" };
        }

        PlacedDrillShape shape = _platformData.FindPlacedShapeByInstanceId(instanceId);
        if (shape == null)
        {
            return new PlaceResult { success = false, errorMessage = "找不到指定造型" };
        }

        DrillShapeConfig config = _configManager.GetDrillShapeConfig(shape.shapeId);
        if (config == null)
        {
            return new PlaceResult { success = false, errorMessage = "找不到造型配置" };
        }

        // 创建临时实例检查碰撞
        PlacedDrillShape tempShape = shape.Clone();
        tempShape.position = newPosition;
        List<Vector2Int> newOccupiedCells = tempShape.GetOccupiedCells(config);

        // 检查边界
        if (!DrillPlatformData.AreAllWithinBounds(newOccupiedCells))
        {
            return new PlaceResult { success = false, errorMessage = "移动后超出平台边界" };
        }

        // 检查碰撞（排除自身）
        foreach (var placedShape in _platformData.placedShapes)
        {
            if (placedShape.instanceId == instanceId) continue;

            DrillShapeConfig otherConfig = _configManager.GetDrillShapeConfig(placedShape.shapeId);
            if (otherConfig == null) continue;

            List<Vector2Int> otherCells = placedShape.GetOccupiedCells(otherConfig);
            foreach (var cell in newOccupiedCells)
            {
                if (otherCells.Contains(cell))
                {
                    return new PlaceResult { success = false, errorMessage = "移动后与其他造型重叠" };
                }
            }
        }

        // 移动成功
        Vector2Int oldPosition = shape.position;
        shape.position = newPosition;

        OnPlatformChanged?.Invoke();

        Debug.Log($"造型 {shape.shapeId} 移动成功，从 {oldPosition} 到 {newPosition}");
        return new PlaceResult { success = true, placedShape = shape };
    }

    /// <summary>
    /// 添加新造型到库存（通过升级获得）
    /// </summary>
    /// <param name="shapeId">造型ID</param>
    public void AddShapeToInventory(string shapeId)
    {
        if (_platformData == null)
        {
            Debug.LogError("平台未初始化，无法添加造型");
            return;
        }

        if (!_platformData.availableShapeIds.Contains(shapeId))
        {
            _platformData.availableShapeIds.Add(shapeId);
            Debug.Log($"新造型 {shapeId} 已添加到库存");
            OnPlatformChanged?.Invoke();
        }
    }

    /// <summary>
    /// 获取平台上所有被占用的格子坐标
    /// </summary>
    public HashSet<Vector2Int> GetAllOccupiedCells()
    {
        if (_platformData == null) return new HashSet<Vector2Int>();
        return _platformData.GetAllOccupiedCells(_configManager.GetShapeConfigDelegate());
    }

    /// <summary>
    /// 获取指定位置的造型
    /// </summary>
    public PlacedDrillShape GetShapeAtPosition(Vector2Int position)
    {
        if (_platformData == null) return null;
        return _platformData.FindShapeAtPosition(position, _configManager.GetShapeConfigDelegate());
    }

    /// <summary>
    /// 检查指定位置是否被占用
    /// </summary>
    public bool IsCellOccupied(Vector2Int position)
    {
        return GetShapeAtPosition(position) != null;
    }

    /// <summary>
    /// 获取库存中的造型ID列表
    /// </summary>
    public List<string> GetAvailableShapeIds()
    {
        if (_platformData == null) return new List<string>();
        return new List<string>(_platformData.availableShapeIds);
    }

    /// <summary>
    /// 获取已放置的造型列表
    /// </summary>
    public List<PlacedDrillShape> GetPlacedShapes()
    {
        // #region agent log
        try { System.IO.File.AppendAllText(@"e:\Work\Cursor\DoomsdaySSW4\.cursor\debug.log", $"{{\"timestamp\":\"{System.DateTime.Now:o}\",\"location\":\"DrillPlatformManager:442\",\"hypothesisId\":\"F\",\"message\":\"GetPlacedShapes called\",\"data\":{{\"instanceId\":\"{GetInstanceID()}\",\"platformData_is_null\":{(_platformData == null).ToString().ToLower()},\"placedShapes_count\":{_platformData?.placedShapes?.Count ?? -1}}}}}\n"); } catch { }
        // #endregion
        if (_platformData == null) return new List<PlacedDrillShape>();
        return new List<PlacedDrillShape>(_platformData.placedShapes);
    }

    /// <summary>
    /// 清空平台（所有造型移回库存）
    /// </summary>
    public void ClearPlatform()
    {
        if (_platformData == null) return;
        
        _platformData.ClearPlatform();
        OnPlatformChanged?.Invoke();
        
        Debug.Log("钻机平台已清空");
    }

    /// <summary>
    /// 验证放置是否有效（不实际放置，仅检查）
    /// </summary>
    public PlaceResult ValidatePlacement(string shapeId, Vector2Int position, int rotation, string excludeInstanceId = null)
    {
        if (_platformData == null)
        {
            return new PlaceResult { success = false, errorMessage = "平台未初始化" };
        }

        DrillShapeConfig config = _configManager.GetDrillShapeConfig(shapeId);
        if (config == null)
        {
            return new PlaceResult { success = false, errorMessage = "找不到造型配置" };
        }

        PlacedDrillShape tempShape = new PlacedDrillShape(shapeId, position, rotation);
        List<Vector2Int> occupiedCells = tempShape.GetOccupiedCells(config);

        // 检查边界
        if (!DrillPlatformData.AreAllWithinBounds(occupiedCells))
        {
            return new PlaceResult { success = false, errorMessage = "超出平台边界" };
        }

        // 检查碰撞
        foreach (var placedShape in _platformData.placedShapes)
        {
            if (excludeInstanceId != null && placedShape.instanceId == excludeInstanceId) continue;

            DrillShapeConfig otherConfig = _configManager.GetDrillShapeConfig(placedShape.shapeId);
            if (otherConfig == null) continue;

            List<Vector2Int> otherCells = placedShape.GetOccupiedCells(otherConfig);
            foreach (var cell in occupiedCells)
            {
                if (otherCells.Contains(cell))
                {
                    return new PlaceResult { success = false, errorMessage = "与现有造型重叠" };
                }
            }
        }

        return new PlaceResult { success = true };
    }
}

/// <summary>
/// 放置操作结果
/// </summary>
[Serializable]
public class PlaceResult
{
    public bool success;
    public string errorMessage;
    public PlacedDrillShape placedShape;
}
