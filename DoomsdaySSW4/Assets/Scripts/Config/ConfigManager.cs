using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 配置管理器：负责加载和管理所有游戏配置数据
/// </summary>
public class ConfigManager : MonoBehaviour
{
    private static ConfigManager _instance;
    public static ConfigManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("ConfigManager");
                _instance = go.AddComponent<ConfigManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // 配置数据存储
    private Dictionary<string, TaskConfig> _taskConfigs = new Dictionary<string, TaskConfig>();
    private Dictionary<string, OreConfig> _oreConfigs = new Dictionary<string, OreConfig>();
    private Dictionary<string, DrillConfig> _drillConfigs = new Dictionary<string, DrillConfig>();
    private Dictionary<string, ShipConfig> _shipConfigs = new Dictionary<string, ShipConfig>();
    private Dictionary<int, OreSpawnConfig> _oreSpawnConfigs = new Dictionary<int, OreSpawnConfig>();
    private List<EnergyUpgradeConfig> _energyUpgradeConfigs = new List<EnergyUpgradeConfig>();
    private Dictionary<string, EnergyThresholdConfig> _energyThresholdConfigs = new Dictionary<string, EnergyThresholdConfig>();
    private List<int> _energyThresholds = new List<int> { 50, 100, 150 }; // 默认能源阈值（向后兼容）
    private List<TileHardnessColorThreshold> _tileHardnessThresholds = new List<TileHardnessColorThreshold>();
    private readonly Color _defaultLowHardnessColor = new Color32(0xE3, 0xC1, 0x76, 0xFF);

    private bool _isLoaded = false;

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

    /// <summary>
    /// 加载所有配置
    /// </summary>
    public void LoadAllConfigs()
    {
        if (_isLoaded)
        {
            Debug.LogWarning("配置已经加载过了");
            return;
        }

        LoadTaskConfigs();
        LoadOreConfigs();
        LoadDrillConfigs();
        LoadShipConfigs();
        LoadOreSpawnConfigs();
        LoadEnergyUpgradeConfigs();
        LoadEnergyThresholdConfigs();
        LoadTileHardnessColorConfigs();

        _isLoaded = true;
        Debug.Log("所有配置加载完成");
    }

    /// <summary>
    /// 加载任务配置
    /// </summary>
    public void LoadTaskConfigs()
    {
        TextAsset textAsset = Resources.Load<TextAsset>("Configs/TaskConfigs");
        if (textAsset == null)
        {
            Debug.LogError("无法加载任务配置: Configs/TaskConfigs");
            return;
        }

        try
        {
            TaskConfigCollection collection = JsonUtility.FromJson<TaskConfigCollection>(textAsset.text);
            _taskConfigs.Clear();

            foreach (var task in collection.tasks)
            {
                _taskConfigs[task.taskId] = task;
            }

            Debug.Log($"成功加载 {_taskConfigs.Count} 个任务配置");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"解析任务配置失败: {e.Message}");
        }
    }

    /// <summary>
    /// 加载矿石配置
    /// </summary>
    public void LoadOreConfigs()
    {
        TextAsset textAsset = Resources.Load<TextAsset>("Configs/OreConfigs");
        if (textAsset == null)
        {
            Debug.LogError("无法加载矿石配置: Configs/OreConfigs");
            return;
        }

        try
        {
            OreConfigCollection collection = JsonUtility.FromJson<OreConfigCollection>(textAsset.text);
            _oreConfigs.Clear();

            foreach (var ore in collection.ores)
            {
                _oreConfigs[ore.oreId] = ore;
            }

            Debug.Log($"成功加载 {_oreConfigs.Count} 个矿石配置");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"解析矿石配置失败: {e.Message}");
        }
    }

    /// <summary>
    /// 加载钻头配置
    /// </summary>
    public void LoadDrillConfigs()
    {
        TextAsset textAsset = Resources.Load<TextAsset>("Configs/DrillConfigs");
        if (textAsset == null)
        {
            Debug.LogError("无法加载钻头配置: Configs/DrillConfigs");
            return;
        }

        try
        {
            DrillConfigCollection collection = JsonUtility.FromJson<DrillConfigCollection>(textAsset.text);
            _drillConfigs.Clear();

            foreach (var drill in collection.drills)
            {
                _drillConfigs[drill.drillId] = drill;
            }

            Debug.Log($"成功加载 {_drillConfigs.Count} 个钻头配置");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"解析钻头配置失败: {e.Message}");
        }
    }

    /// <summary>
    /// 加载船只配置
    /// </summary>
    public void LoadShipConfigs()
    {
        TextAsset textAsset = Resources.Load<TextAsset>("Configs/ShipConfigs");
        if (textAsset == null)
        {
            Debug.LogError("无法加载船只配置: Configs/ShipConfigs");
            return;
        }

        try
        {
            ShipConfigCollection collection = JsonUtility.FromJson<ShipConfigCollection>(textAsset.text);
            _shipConfigs.Clear();

            foreach (var ship in collection.ships)
            {
                _shipConfigs[ship.shipId] = ship;
            }

            Debug.Log($"成功加载 {_shipConfigs.Count} 个船只配置");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"解析船只配置失败: {e.Message}");
        }
    }

    /// <summary>
    /// 加载矿石生成规则配置
    /// </summary>
    public void LoadOreSpawnConfigs()
    {
        TextAsset textAsset = Resources.Load<TextAsset>("Configs/OreSpawnConfigs");
        if (textAsset == null)
        {
            Debug.LogError("无法加载矿石生成规则配置: Configs/OreSpawnConfigs");
            return;
        }

        try
        {
            OreSpawnConfigCollection collection = JsonUtility.FromJson<OreSpawnConfigCollection>(textAsset.text);
            _oreSpawnConfigs.Clear();

            foreach (var layer in collection.layers)
            {
                _oreSpawnConfigs[layer.layerDepth] = layer;
            }

            Debug.Log($"成功加载 {_oreSpawnConfigs.Count} 层矿石生成规则");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"解析矿石生成规则配置失败: {e.Message}");
        }
    }

    // 查询接口

    /// <summary>
    /// 获取任务配置
    /// </summary>
    public TaskConfig GetTaskConfig(string taskId)
    {
        if (_taskConfigs.TryGetValue(taskId, out TaskConfig config))
        {
            return config;
        }
        Debug.LogWarning($"未找到任务配置: {taskId}");
        return null;
    }

    /// <summary>
    /// 获取所有基础任务
    /// </summary>
    public List<TaskConfig> GetAllBasicTasks()
    {
        return _taskConfigs.Values.Where(t => t.taskType == TaskType.Basic).ToList();
    }

    /// <summary>
    /// 获取矿石配置
    /// </summary>
    public OreConfig GetOreConfig(string oreId)
    {
        if (_oreConfigs.TryGetValue(oreId, out OreConfig config))
        {
            return config;
        }
        Debug.LogWarning($"未找到矿石配置: {oreId}");
        return null;
    }

    /// <summary>
    /// 获取所有矿石配置
    /// </summary>
    public List<OreConfig> GetAllOreConfigs()
    {
        return _oreConfigs.Values.ToList();
    }

    /// <summary>
    /// 获取指定层的矿石生成规则
    /// </summary>
    public OreSpawnConfig GetOreSpawnConfig(int layerDepth)
    {
        if (_oreSpawnConfigs.TryGetValue(layerDepth, out OreSpawnConfig config))
        {
            return config;
        }
        // 如果没有该层的配置，返回空配置
        return new OreSpawnConfig { layerDepth = layerDepth, spawnRules = new List<OreSpawnRule>() };
    }

    /// <summary>
    /// 获取钻头配置
    /// </summary>
    public DrillConfig GetDrillConfig(string drillId)
    {
        if (_drillConfigs.TryGetValue(drillId, out DrillConfig config))
        {
            return config;
        }
        Debug.LogWarning($"未找到钻头配置: {drillId}");
        return null;
    }

    /// <summary>
    /// 获取船只配置
    /// </summary>
    public ShipConfig GetShipConfig(string shipId)
    {
        if (_shipConfigs.TryGetValue(shipId, out ShipConfig config))
        {
            return config;
        }
        Debug.LogWarning($"未找到船只配置: {shipId}");
        return null;
    }

    /// <summary>
    /// 加载能源升级配置
    /// </summary>
    public void LoadEnergyUpgradeConfigs()
    {
        TextAsset textAsset = Resources.Load<TextAsset>("Configs/EnergyUpgradeConfigs");
        if (textAsset == null)
        {
            Debug.LogError("无法加载能源升级配置: Configs/EnergyUpgradeConfigs");
            return;
        }

        try
        {
            EnergyUpgradeConfigCollection collection = JsonUtility.FromJson<EnergyUpgradeConfigCollection>(textAsset.text);
            _energyUpgradeConfigs.Clear();

            foreach (var upgrade in collection.upgrades)
            {
                _energyUpgradeConfigs.Add(upgrade);
            }

            Debug.Log($"成功加载 {_energyUpgradeConfigs.Count} 个能源升级配置");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"解析能源升级配置失败: {e.Message}");
        }
    }

    /// <summary>
    /// 获取所有能源升级配置
    /// </summary>
    public List<EnergyUpgradeConfig> GetEnergyUpgradeConfigs()
    {
        return new List<EnergyUpgradeConfig>(_energyUpgradeConfigs);
    }

    /// <summary>
    /// 根据类型获取能源升级配置
    /// </summary>
    public List<EnergyUpgradeConfig> GetEnergyUpgradeConfigsByType(UpgradeOptionType type)
    {
        string typeString = type.ToString();
        return _energyUpgradeConfigs.FindAll(c => c.type == typeString);
    }

    /// <summary>
    /// 加载能源阈值配置
    /// </summary>
    public void LoadEnergyThresholdConfigs()
    {
        TextAsset textAsset = Resources.Load<TextAsset>("Configs/EnergyThresholds");
        if (textAsset == null)
        {
            Debug.LogWarning("无法加载能源阈值配置: Configs/EnergyThresholds，使用默认值");
            return;
        }

        try
        {
            EnergyThresholdConfigCollection collection = JsonUtility.FromJson<EnergyThresholdConfigCollection>(textAsset.text);
            _energyThresholdConfigs.Clear();

            foreach (var threshold in collection.thresholds)
            {
                _energyThresholdConfigs[threshold.shipId] = threshold;
            }

            Debug.Log($"成功加载 {_energyThresholdConfigs.Count} 个能源阈值配置");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"解析能源阈值配置失败: {e.Message}");
        }
    }

    /// <summary>
    /// 加载硬度颜色配置
    /// </summary>
    public void LoadTileHardnessColorConfigs()
    {
        TextAsset textAsset = Resources.Load<TextAsset>("Configs/TileHardnessColorConfigs");
        if (textAsset == null)
        {
            Debug.LogWarning("无法加载硬度颜色配置: Configs/TileHardnessColorConfigs，使用默认颜色");
            return;
        }

        try
        {
            TileHardnessColorConfigCollection collection = JsonUtility.FromJson<TileHardnessColorConfigCollection>(textAsset.text);
            _tileHardnessThresholds.Clear();

            if (collection != null && collection.thresholds != null)
            {
                _tileHardnessThresholds.AddRange(collection.thresholds);
                _tileHardnessThresholds = _tileHardnessThresholds
                    .OrderBy(t => t.maxHardness)
                    .ToList();
            }

            Debug.Log($"成功加载 {_tileHardnessThresholds.Count} 条硬度颜色配置");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"解析硬度颜色配置失败: {e.Message}");
        }
    }

    /// <summary>
    /// 根据硬度获取颜色（矿石格子）
    /// </summary>
    public Color GetHardnessColor(int hardness)
    {
        if (_tileHardnessThresholds == null || _tileHardnessThresholds.Count == 0)
        {
            return _defaultLowHardnessColor;
        }

        foreach (var threshold in _tileHardnessThresholds)
        {
            if (hardness <= threshold.maxHardness)
            {
                if (TryParseHexColor(threshold.colorHex, out Color color))
                {
                    return color;
                }
                return _defaultLowHardnessColor;
            }
        }

        TileHardnessColorThreshold last = _tileHardnessThresholds[_tileHardnessThresholds.Count - 1];
        if (TryParseHexColor(last.colorHex, out Color lastColor))
        {
            return lastColor;
        }
        return _defaultLowHardnessColor;
    }

    private bool TryParseHexColor(string hex, out Color color)
    {
        if (!string.IsNullOrEmpty(hex))
        {
            if (ColorUtility.TryParseHtmlString("#" + hex, out color))
            {
                return true;
            }
        }
        color = _defaultLowHardnessColor;
        return false;
    }

    /// <summary>
    /// 获取能源阈值列表（根据船只ID）
    /// </summary>
    public List<int> GetEnergyThresholds(string shipId)
    {
        if (string.IsNullOrEmpty(shipId))
        {
            shipId = "default_ship";
        }

        if (_energyThresholdConfigs.TryGetValue(shipId, out EnergyThresholdConfig config))
        {
            // 确保 energyThresholds 不为 null
            if (config.energyThresholds != null && config.energyThresholds.Length > 0)
            {
                return new List<int>(config.energyThresholds);
            }
            else
            {
                Debug.LogWarning($"船只 {shipId} 的能源阈值配置为空，使用默认值");
            }
        }

        // 如果找不到指定船只的配置，使用默认阈值
        Debug.LogWarning($"未找到船只 {shipId} 的能源阈值配置，使用默认值");
        return new List<int>(_energyThresholds);
    }

    /// <summary>
    /// 获取能源阈值列表（向后兼容，使用默认船只）
    /// </summary>
    public List<int> GetEnergyThresholds()
    {
        return GetEnergyThresholds("default_ship");
    }

    /// <summary>
    /// 检查配置是否已加载
    /// </summary>
    public bool IsLoaded()
    {
        return _isLoaded;
    }
}
