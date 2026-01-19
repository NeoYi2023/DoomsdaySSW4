using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 能源升级管理器：负责能源累计、阈值检查、三选一升级
/// </summary>
public class EnergyUpgradeManager : MonoBehaviour
{
    private static EnergyUpgradeManager _instance;
    public static EnergyUpgradeManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("EnergyUpgradeManager");
                _instance = go.AddComponent<EnergyUpgradeManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private EnergyData _energyData;
    private ConfigManager _configManager;
    private DebtManager _debtManager;
    
    // 全局升级效果状态
    private int _miningEfficiencyBonus = 0;      // 挖掘效率加成（百分比）
    private int _oreValueBoostBonus = 0;         // 矿石价值加成（百分比）
    private int _oreDiscoveryBonus = 0;          // 矿石发现能力加成（每回合额外发现数量）

    // 升级触发事件
    public UnityEvent<List<EnergyUpgradeOption>> OnUpgradeAvailable = new UnityEvent<List<EnergyUpgradeOption>>();

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
        _debtManager = DebtManager.Instance;
        InitializeEnergyData();
    }

    /// <summary>
    /// 初始化能源数据
    /// </summary>
    private void InitializeEnergyData()
    {
        // 获取当前船只ID
        string shipId = "default_ship";
        if (_debtManager != null)
        {
            shipId = _debtManager.GetCurrentShipId();
        }

        // 根据船只ID获取对应的能源阈值
        List<int> thresholds = null;
        if (_configManager != null)
        {
            thresholds = _configManager.GetEnergyThresholds(shipId);
        }

        // 确保 thresholds 不为 null
        if (thresholds == null)
        {
            thresholds = new List<int> { 50, 100, 150 }; // 使用默认值
            Debug.LogWarning("能源阈值配置为 null，使用默认值 [50, 100, 150]");
        }

        _energyData = new EnergyData
        {
            currentEnergy = 0,
            totalEnergyCollected = 0,
            energyThresholds = thresholds,
            nextThresholdIndex = 0,
            upgrades = new List<EnergyUpgrade>()
        };

        Debug.Log($"能源系统初始化完成，阈值: {string.Join(", ", _energyData.energyThresholds ?? new List<int>())}");
    }

    /// <summary>
    /// 重新初始化能源数据（在船只初始化后调用，确保使用正确的阈值）
    /// </summary>
    public void ReinitializeEnergyData()
    {
        // 如果已经有能源数据，只更新阈值（保留当前能源值）
        if (_energyData != null)
        {
            string shipId = "default_ship";
            if (_debtManager != null)
            {
                shipId = _debtManager.GetCurrentShipId();
            }

            List<int> thresholds = null;
            if (_configManager != null)
            {
                thresholds = _configManager.GetEnergyThresholds(shipId);
            }

            if (thresholds != null)
            {
                _energyData.energyThresholds = thresholds;
                _energyData.nextThresholdIndex = 0; // 重置阈值索引
                Debug.Log($"能源阈值已更新（船只: {shipId}），阈值: {string.Join(", ", thresholds ?? new List<int>())}");
            }
            else
            {
                Debug.LogWarning($"未获取到船只 {shipId} 的能源阈值配置，保持当前阈值");
            }
        }
        else
        {
            InitializeEnergyData();
        }
    }

    /// <summary>
    /// 添加能源
    /// </summary>
    public void AddEnergy(int amount)
    {
        // #region agent log
        try { System.IO.File.AppendAllText(@"e:\Work\Cursor\DoomsdaySSW4\.cursor\debug.log", $"{{\"timestamp\":\"{System.DateTime.Now:o}\",\"location\":\"EnergyUpgradeManager:136\",\"hypothesisId\":\"C\",\"message\":\"AddEnergy called\",\"data\":{{\"amount\":{amount},\"energyData_is_null\":{(_energyData == null).ToString().ToLower()},\"currentEnergy_before\":{_energyData?.currentEnergy ?? 0}}}}}\n"); } catch { }
        // #endregion
        if (_energyData == null)
        {
            InitializeEnergyData();
        }

        _energyData.currentEnergy += amount;
        _energyData.totalEnergyCollected += amount;

        Debug.Log($"获得能源: +{amount}, 当前能源: {_energyData.currentEnergy}");
        // #region agent log
        try { System.IO.File.AppendAllText(@"e:\Work\Cursor\DoomsdaySSW4\.cursor\debug.log", $"{{\"timestamp\":\"{System.DateTime.Now:o}\",\"location\":\"EnergyUpgradeManager:148\",\"hypothesisId\":\"C\",\"message\":\"Energy added\",\"data\":{{\"amount\":{amount},\"currentEnergy_after\":{_energyData.currentEnergy}}}}}\n"); } catch { }
        // #endregion

        // 检查是否达到阈值
        CheckEnergyThreshold();
    }

    /// <summary>
    /// 检查能源阈值
    /// </summary>
    private void CheckEnergyThreshold()
    {
        if (_energyData == null || _energyData.energyThresholds == null)
        {
            return;
        }

        if (_energyData.nextThresholdIndex >= _energyData.energyThresholds.Count)
        {
            // 所有阈值都已触发
            return;
        }

        int nextThreshold = _energyData.energyThresholds[_energyData.nextThresholdIndex];

        if (_energyData.currentEnergy >= nextThreshold)
        {
            Debug.Log($"达到能源阈值: {nextThreshold}，触发升级选择");
            TriggerUpgradeSelection();
            _energyData.nextThresholdIndex++;
        }
    }

    /// <summary>
    /// 触发三选一升级
    /// </summary>
    private void TriggerUpgradeSelection()
    {
        List<EnergyUpgradeOption> options = GenerateUpgradeOptions(3);
        OnUpgradeAvailable?.Invoke(options);
    }

    /// <summary>
    /// 生成升级选项（从配置文件加载，使用权重系统随机选择）
    /// </summary>
    private List<EnergyUpgradeOption> GenerateUpgradeOptions(int count)
    {
        if (_configManager == null)
        {
            _configManager = ConfigManager.Instance;
        }

        List<EnergyUpgradeConfig> configs = _configManager.GetEnergyUpgradeConfigs();
        if (configs == null || configs.Count == 0)
        {
            Debug.LogWarning("没有可用的升级配置，使用默认选项");
            return GenerateDefaultOptions(count);
        }

        // 使用权重系统选择（类似矿石生成逻辑）
        System.Random random = new System.Random();
        List<EnergyUpgradeOption> selected = new List<EnergyUpgradeOption>();
        List<EnergyUpgradeConfig> available = new List<EnergyUpgradeConfig>(configs);

        for (int i = 0; i < count && available.Count > 0; i++)
        {
            // 计算总权重
            int totalWeight = available.Sum(c => c.weight);
            if (totalWeight == 0)
            {
                // 如果所有权重为0，使用均匀随机
                int index = random.Next(0, available.Count);
                EnergyUpgradeConfig selectedConfig = available[index];
                selected.Add(ConvertConfigToOption(selectedConfig));
                available.RemoveAt(index);
            }
            else
            {
                // 基于权重随机选择
                int randomValue = random.Next(0, totalWeight);
                int currentWeight = 0;
                EnergyUpgradeConfig selectedConfig = null;
                int selectedIndex = -1;

                for (int j = 0; j < available.Count; j++)
                {
                    currentWeight += available[j].weight;
                    if (randomValue < currentWeight)
                    {
                        selectedConfig = available[j];
                        selectedIndex = j;
                        break;
                    }
                }

                if (selectedConfig != null)
                {
                    selected.Add(ConvertConfigToOption(selectedConfig));
                    available.RemoveAt(selectedIndex);
                }
            }
        }

        return selected;
    }

    /// <summary>
    /// 将配置转换为选项
    /// </summary>
    private EnergyUpgradeOption ConvertConfigToOption(EnergyUpgradeConfig config)
    {
        // 将字符串类型转换为枚举
        UpgradeOptionType type;
        if (System.Enum.TryParse<UpgradeOptionType>(config.type, out type))
        {
            return new EnergyUpgradeOption
            {
                type = type,
                name = config.name,
                description = config.description,
                value = config.value,
                iconPath = config.iconPath
            };
        }
        else
        {
            Debug.LogWarning($"未知的升级类型: {config.type}");
            return new EnergyUpgradeOption
            {
                type = UpgradeOptionType.DrillStrength,
                name = config.name,
                description = config.description,
                value = config.value,
                iconPath = config.iconPath
            };
        }
    }

    /// <summary>
    /// 生成默认选项（当配置文件加载失败时使用）
    /// </summary>
    private List<EnergyUpgradeOption> GenerateDefaultOptions(int count)
    {
        List<EnergyUpgradeOption> allOptions = new List<EnergyUpgradeOption>
        {
            new EnergyUpgradeOption
            {
                type = UpgradeOptionType.DrillStrength,
                name = "挖掘强度提升",
                description = "钻头攻击值 +5",
                value = 5,
                iconPath = ""
            },
            new EnergyUpgradeOption
            {
                type = UpgradeOptionType.DrillRange,
                name = "挖掘范围提升",
                description = "钻头范围 +1x1",
                value = 1,
                iconPath = ""
            },
            new EnergyUpgradeOption
            {
                type = UpgradeOptionType.MiningEfficiency,
                name = "挖掘效率提升",
                description = "每次挖掘额外获得 10% 金钱",
                value = 10,
                iconPath = ""
            }
        };

        System.Random random = new System.Random();
        List<EnergyUpgradeOption> selected = new List<EnergyUpgradeOption>();
        List<EnergyUpgradeOption> available = new List<EnergyUpgradeOption>(allOptions);

        for (int i = 0; i < count && available.Count > 0; i++)
        {
            int index = random.Next(0, available.Count);
            selected.Add(available[index]);
            available.RemoveAt(index);
        }

        return selected;
    }

    /// <summary>
    /// 选择升级选项
    /// </summary>
    public void SelectUpgrade(EnergyUpgradeOption option)
    {
        if (_energyData == null)
        {
            Debug.LogError("能源数据未初始化");
            return;
        }

        // 创建升级记录
        EnergyUpgrade upgrade = new EnergyUpgrade
        {
            upgradeId = System.Guid.NewGuid().ToString(),
            optionType = option.type,
            value = option.value,
            timestamp = System.DateTime.Now.Ticks
        };

        _energyData.upgrades.Add(upgrade);

        // 应用升级效果
        ApplyUpgradeEffect(option);

        Debug.Log($"选择升级: {option.name}");
    }

    /// <summary>
    /// 应用升级效果
    /// </summary>
    private void ApplyUpgradeEffect(EnergyUpgradeOption option)
    {
        DrillManager drillManager = DrillManager.Instance;
        DrillData drill = drillManager.GetCurrentDrill();

        switch (option.type)
        {
            case UpgradeOptionType.DrillStrength:
                if (drill != null)
                {
                    DrillUpgrade strengthUpgrade = new DrillUpgrade
                    {
                        upgradeId = System.Guid.NewGuid().ToString(),
                        type = UpgradeType.StrengthBoost,
                        value = option.value,
                        description = option.description,
                        isPermanent = false
                    };
                    drillManager.ApplyUpgrade(strengthUpgrade);
                }
                break;

            case UpgradeOptionType.DrillRange:
                if (drill != null)
                {
                    DrillUpgrade rangeUpgrade = new DrillUpgrade
                    {
                        upgradeId = System.Guid.NewGuid().ToString(),
                        type = UpgradeType.RangeBoost,
                        value = option.value,
                        description = option.description,
                        isPermanent = false
                    };
                    drillManager.ApplyUpgrade(rangeUpgrade);
                }
                break;

            case UpgradeOptionType.MiningEfficiency:
                // 挖掘效率加成（累加）
                _miningEfficiencyBonus += option.value;
                Debug.Log($"挖掘效率提升: +{option.value}%, 当前总加成: {_miningEfficiencyBonus}%");
                break;

            case UpgradeOptionType.OreValueBoost:
                // 矿石价值加成（累加）
                _oreValueBoostBonus += option.value;
                Debug.Log($"矿石价值提升: +{option.value}%, 当前总加成: {_oreValueBoostBonus}%");
                break;

            case UpgradeOptionType.OreDiscovery:
                // 矿石发现能力加成（累加）
                _oreDiscoveryBonus += option.value;
                Debug.Log($"矿石发现能力提升: +{option.value}, 当前总加成: {_oreDiscoveryBonus}");
                break;

            default:
                Debug.LogWarning($"未知的升级类型: {option.type}");
                break;
        }
    }

    /// <summary>
    /// 获取当前能源值
    /// </summary>
    public int GetCurrentEnergy()
    {
        return _energyData != null ? _energyData.currentEnergy : 0;
    }

    /// <summary>
    /// 获取能源数据
    /// </summary>
    public EnergyData GetEnergyData()
    {
        return _energyData;
    }

    /// <summary>
    /// 获取挖掘效率加成（百分比）
    /// </summary>
    public int GetMiningEfficiencyBonus()
    {
        return _miningEfficiencyBonus;
    }

    /// <summary>
    /// 获取矿石价值加成（百分比）
    /// </summary>
    public int GetOreValueBoostBonus()
    {
        return _oreValueBoostBonus;
    }

    /// <summary>
    /// 获取矿石发现能力加成（每回合额外发现数量）
    /// </summary>
    public int GetOreDiscoveryBonus()
    {
        return _oreDiscoveryBonus;
    }

    /// <summary>
    /// 重置本关升级（任务完成后）
    /// </summary>
    public void ResetLevelUpgrades()
    {
        if (_energyData != null)
        {
            _energyData.currentEnergy = 0;
            _energyData.nextThresholdIndex = 0;
            _energyData.upgrades.Clear();
        }

        // 重置全局升级效果
        _miningEfficiencyBonus = 0;
        _oreValueBoostBonus = 0;
        _oreDiscoveryBonus = 0;

        Debug.Log("能源系统本关升级已重置");
    }
}

/// <summary>
/// 能源升级选项（用于UI显示）
/// </summary>
[System.Serializable]
public class EnergyUpgradeOption
{
    public UpgradeOptionType type;
    public string name;
    public string description;
    public int value;
    public string iconPath;          // 图标路径（相对于Resources目录）
}
