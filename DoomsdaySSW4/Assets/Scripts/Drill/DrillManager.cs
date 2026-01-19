using UnityEngine;

/// <summary>
/// 钻头管理器：负责钻头管理、属性计算、升级应用
/// </summary>
public class DrillManager : MonoBehaviour
{
    private static DrillManager _instance;
    public static DrillManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("DrillManager");
                _instance = go.AddComponent<DrillManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private DrillData _currentDrill;
    private ConfigManager _configManager;
    private DrillPlatformManager _platformManager;

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
    }

    /// <summary>
    /// 初始化钻头（使用造型系统）
    /// </summary>
    /// <param name="shipId">船只ID，用于获取初始造型配置</param>
    public void InitializeDrill(string shipId = "default_ship")
    {
        // 确保 ConfigManager 已初始化（防止在 Start() 之前调用）
        if (_configManager == null)
        {
            _configManager = ConfigManager.Instance;
        }
        
        if (_platformManager == null)
        {
            _platformManager = DrillPlatformManager.Instance;
        }

        // 获取船只配置
        ShipConfig shipConfig = _configManager.GetShipConfig(shipId);
        string drillName = shipConfig != null ? $"{shipConfig.shipName}的钻头" : "默认钻头";

        _currentDrill = new DrillData
        {
            drillId = $"drill_{shipId}",
            drillName = drillName,
            drillType = DrillType.Default,
            platformData = new DrillPlatformData(), // 使用造型系统
            currentLevel = 1,
            upgrades = new System.Collections.Generic.List<DrillUpgrade>(),
            permanentStrengthBonus = 0,
            permanentAttackMultiplier = 1f
        };

        // 初始化钻机平台（会根据船只配置放置初始造型）
        _platformManager.InitializePlatform(shipId);
        
        // 关联平台数据
        _currentDrill.platformData = _platformManager.GetPlatformData();

        Debug.Log($"钻头初始化完成（造型系统），船只: {shipId}, 钻头: {_currentDrill.drillName}");
    }
    
    /// <summary>
    /// 初始化默认钻头（向后兼容）
    /// </summary>
    public void InitializeDefaultDrill()
    {
        InitializeDrill("default_ship");
    }

    /// <summary>
    /// 获取当前装备的钻头
    /// </summary>
    public DrillData GetCurrentDrill()
    {
        return _currentDrill;
    }

    /// <summary>
    /// 应用升级（来自能源升级系统）
    /// </summary>
    public void ApplyUpgrade(DrillUpgrade upgrade)
    {
        if (_currentDrill == null)
        {
            Debug.LogError("当前没有装备钻头");
            return;
        }

        switch (upgrade.type)
        {
            case UpgradeType.StrengthBoost:
                // 增加永久攻击加成
                _currentDrill.permanentStrengthBonus += upgrade.value;
                Debug.Log($"钻头永久强度加成提升 {upgrade.value}，当前加成: {_currentDrill.permanentStrengthBonus}");
                break;

            case UpgradeType.RangeBoost:
                // 旧模式：范围提升（已弃用，保留向后兼容）
                #pragma warning disable 612, 618
                _currentDrill.miningRange += new Vector2Int(upgrade.value, upgrade.value);
                Debug.Log($"钻头范围提升 {upgrade.value}（旧模式）");
                #pragma warning restore 612, 618
                break;
                
            case UpgradeType.NewShape:
                // 新模式：获得新造型
                if (_platformManager != null && !string.IsNullOrEmpty(upgrade.description))
                {
                    // upgrade.description 存储新造型的 shapeId
                    _platformManager.AddShapeToInventory(upgrade.description);
                    Debug.Log($"获得新造型: {upgrade.description}");
                }
                break;

            default:
                Debug.LogWarning($"未处理的升级类型: {upgrade.type}");
                break;
        }

        _currentDrill.upgrades.Add(upgrade);
        _currentDrill.currentLevel++;
    }
    
    /// <summary>
    /// 添加新造型到库存
    /// </summary>
    public void AddNewShape(string shapeId)
    {
        if (_platformManager == null)
        {
            _platformManager = DrillPlatformManager.Instance;
        }
        
        _platformManager.AddShapeToInventory(shapeId);
    }
    
    /// <summary>
    /// 获取钻机平台管理器
    /// </summary>
    public DrillPlatformManager GetPlatformManager()
    {
        if (_platformManager == null)
        {
            _platformManager = DrillPlatformManager.Instance;
        }
        return _platformManager;
    }

    /// <summary>
    /// 重置本关升级（任务完成后）
    /// </summary>
    public void ResetLevelUpgrades()
    {
        if (_currentDrill == null) return;

        // 对于造型系统，重新初始化平台（保留永久加成和已解锁的造型）
        if (_currentDrill.UsesShapeSystem() && _platformManager != null)
        {
            // 注意：永久解锁的造型不应该被移除
            // 这里只清空平台布局，造型库存保留
            _platformManager.ClearPlatform();
        }

        // 清除本关升级（但保留永久加成）
        _currentDrill.upgrades.Clear();
        _currentDrill.currentLevel = 1;

        Debug.Log("钻头本关升级已重置");
    }
}
