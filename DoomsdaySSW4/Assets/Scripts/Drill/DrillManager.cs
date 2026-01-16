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
    /// 初始化默认钻头
    /// </summary>
    public void InitializeDefaultDrill()
    {
        // 确保 ConfigManager 已初始化（防止在 Start() 之前调用）
        if (_configManager == null)
        {
            _configManager = ConfigManager.Instance;
        }

        DrillConfig config = _configManager.GetDrillConfig("default_drill");
        if (config == null)
        {
            Debug.LogError("无法加载默认钻头配置");
            return;
        }

        _currentDrill = new DrillData
        {
            drillId = config.drillId,
            drillName = config.drillName,
            drillType = DrillType.Default,
            miningStrength = config.miningStrength,
            miningRange = new Vector2Int(config.miningRangeX, config.miningRangeY),
            drillCenter = new Vector2Int(4, 4), // 默认在9x9中心
            currentLevel = 1,
            upgrades = new System.Collections.Generic.List<DrillUpgrade>(),
            permanentStrengthBonus = 0,
            permanentRangeBonus = Vector2Int.zero
        };

        Debug.Log($"默认钻头初始化完成: {_currentDrill.drillName}, 强度={_currentDrill.miningStrength}, 范围={_currentDrill.miningRange.x}x{_currentDrill.miningRange.y}");
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
                _currentDrill.miningStrength += upgrade.value;
                Debug.Log($"钻头强度提升 {upgrade.value}，当前强度: {_currentDrill.miningStrength}");
                break;

            case UpgradeType.RangeBoost:
                _currentDrill.miningRange += new Vector2Int(upgrade.value, upgrade.value);
                Debug.Log($"钻头范围提升 {upgrade.value}，当前范围: {_currentDrill.miningRange.x}x{_currentDrill.miningRange.y}");
                break;

            default:
                Debug.LogWarning($"未处理的升级类型: {upgrade.type}");
                break;
        }

        _currentDrill.upgrades.Add(upgrade);
        _currentDrill.currentLevel++;
    }

    /// <summary>
    /// 重置本关升级（任务完成后）
    /// </summary>
    public void ResetLevelUpgrades()
    {
        if (_currentDrill == null) return;

        // 重新从配置加载基础属性
        DrillConfig config = _configManager.GetDrillConfig(_currentDrill.drillId);
        if (config != null)
        {
            _currentDrill.miningStrength = config.miningStrength;
            _currentDrill.miningRange = new Vector2Int(config.miningRangeX, config.miningRangeY);
        }

        // 清除本关升级（但保留永久加成）
        _currentDrill.upgrades.Clear();
        _currentDrill.currentLevel = 1;

        Debug.Log("钻头本关升级已重置");
    }
}
