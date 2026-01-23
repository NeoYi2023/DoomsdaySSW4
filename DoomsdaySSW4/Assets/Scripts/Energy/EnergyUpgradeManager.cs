using System.Collections.Generic;
using System.Linq;
using System.IO;
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
        if (_energyData == null)
        {
            InitializeEnergyData();
        }

        _energyData.currentEnergy += amount;
        _energyData.totalEnergyCollected += amount;

        Debug.Log($"获得能源: +{amount}, 当前能源: {_energyData.currentEnergy}");

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

            // 每次触发升级后，消耗本次升级所需的阈值能源
            // 例如：当前能源为102点，阈值为100点，则升级后变为2点
            _energyData.currentEnergy -= nextThreshold;
            if (_energyData.currentEnergy < 0)
            {
                _energyData.currentEnergy = 0;
            }

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

        // 过滤已解锁的造型解锁项：同一个 shapeId 只需解锁一次
        if (_energyData != null && _energyData.upgrades != null)
        {
            HashSet<string> unlockedShapeIds = new HashSet<string>();
            foreach (var upgrade in _energyData.upgrades)
            {
                if (upgrade.optionType == UpgradeOptionType.DrillShapeUnlock && !string.IsNullOrEmpty(upgrade.upgradeId))
                {
                    unlockedShapeIds.Add(upgrade.upgradeId);
                }
            }

            configs = configs.Where(c =>
            {
                if (string.Equals(c.type, UpgradeOptionType.DrillShapeUnlock.ToString(), System.StringComparison.OrdinalIgnoreCase))
                {
                    // 对于造型解锁，约定 upgradeId 即 shapeId
                    return !unlockedShapeIds.Contains(c.upgradeId);
                }
                return true;
            }).ToList();
        }

        if (configs.Count == 0)
        {
            Debug.LogWarning("过滤已解锁造型后没有可用的升级配置，使用默认选项");
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
                upgradeId = config.upgradeId,
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
                upgradeId = config.upgradeId,
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
                    type = UpgradeOptionType.MiningEfficiency,
                    name = "挖掘效率提升（小）",
                    description = "每次挖掘额外获得 5% 金钱",
                    value = 5,
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

        // 创建升级记录（对于造型解锁，约定 upgradeId 即 shapeId）
        EnergyUpgrade upgrade = new EnergyUpgrade
        {
            upgradeId = option.upgradeId ?? System.Guid.NewGuid().ToString(),
            optionType = option.type,
            value = option.value,
            timestamp = System.DateTime.Now.Ticks
        };

        _energyData.upgrades.Add(upgrade);

        // #region agent log
        try
        {
            var log = "{\"sessionId\":\"debug-session\",\"runId\":\"pre-fix-1\",\"hypothesisId\":\"H1\",\"location\":\"EnergyUpgradeManager.SelectUpgrade\",\"message\":\"SelectUpgrade called\",\"data\":{\"optionType\":\"" + option.type + "\",\"upgradeId\":\"" + (option.upgradeId ?? "") + "\",\"currentEnergy\":" + _energyData.currentEnergy + "},\"timestamp\":" + System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}";
            File.AppendAllText("e:\\Work\\Cursor\\DoomsdaySSW4\\.cursor\\debug.log", log + System.Environment.NewLine);
        }
        catch { }
        // #endregion

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
        DrillPlatformManager platformManager = DrillPlatformManager.Instance;
        DrillData drill = drillManager.GetCurrentDrill();

        // #region agent log
        try
        {
            var log = "{\"sessionId\":\"debug-session\",\"runId\":\"pre-fix-2\",\"hypothesisId\":\"H1\",\"location\":\"EnergyUpgradeManager.ApplyUpgradeEffect\",\"message\":\"ApplyUpgradeEffect enter\",\"data\":{\"optionType\":\"" + option.type + "\",\"upgradeId\":\"" + (option.upgradeId ?? "") + "\",\"platformManagerNull\":" + (platformManager == null ? "true" : "false") + "},\"timestamp\":" + System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}";
            File.AppendAllText("e:\\Work\\Cursor\\DoomsdaySSW4\\.cursor\\debug.log", log + System.Environment.NewLine);
        }
        catch { }
        // #endregion

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

            case UpgradeOptionType.DrillShapeUnlock:
                // 通过升级解锁新的钻头造型：约定 upgradeId 即 shapeId
                if (platformManager != null && !string.IsNullOrEmpty(option.upgradeId))
                {
                    // #region agent log
                    try
                    {
                        var log = "{\"sessionId\":\"debug-session\",\"runId\":\"pre-fix-2\",\"hypothesisId\":\"H1\",\"location\":\"EnergyUpgradeManager.ApplyUpgradeEffect\",\"message\":\"DrillShapeUnlock condition passed\",\"data\":{\"shapeId\":\"" + option.upgradeId + "\"},\"timestamp\":" + System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}";
                        File.AppendAllText("e:\\Work\\Cursor\\DoomsdaySSW4\\.cursor\\debug.log", log + System.Environment.NewLine);
                    }
                    catch { }
                    // #endregion

                    platformManager.AddShapeToInventory(option.upgradeId);
                    
                    // #region agent log
                    try
                    {
                        var log = "{\"sessionId\":\"debug-session\",\"runId\":\"pre-fix-1\",\"hypothesisId\":\"H1\",\"location\":\"EnergyUpgradeManager.ApplyUpgradeEffect\",\"message\":\"Apply DrillShapeUnlock\",\"data\":{\"shapeId\":\"" + option.upgradeId + "\"},\"timestamp\":" + System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}";
                        File.AppendAllText("e:\\Work\\Cursor\\DoomsdaySSW4\\.cursor\\debug.log", log + System.Environment.NewLine);
                    }
                    catch { }
                    // #endregion

                    Debug.Log($"解锁钻头造型: {option.upgradeId}，已加入当前关卡钻机平台库存");
                }
                else
                {
                    // #region agent log
                    try
                    {
                        var log = "{\"sessionId\":\"debug-session\",\"runId\":\"pre-fix-2\",\"hypothesisId\":\"H1\",\"location\":\"EnergyUpgradeManager.ApplyUpgradeEffect\",\"message\":\"DrillShapeUnlock condition failed\",\"data\":{\"platformManagerNull\":" + (platformManager == null ? "true" : "false") + ",\"upgradeIdNullOrEmpty\":" + (string.IsNullOrEmpty(option.upgradeId) ? "true" : "false") + "},\"timestamp\":" + System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}";
                        File.AppendAllText("e:\\Work\\Cursor\\DoomsdaySSW4\\.cursor\\debug.log", log + System.Environment.NewLine);
                    }
                    catch { }
                    // #endregion

                    Debug.LogWarning($"尝试解锁钻头造型失败，upgradeId 无效: {option.upgradeId}");
                }
                break;

            case UpgradeOptionType.DrillPlatformUpgrade:
                // 钻机平台升级：提升基础强度或增加插槽
                // upgradeId 指向造型ID，value 表示强度提升值或插槽数量
                if (platformManager != null && !string.IsNullOrEmpty(option.upgradeId))
                {
                    DrillPlatformData platformData = platformManager.GetPlatformData();
                    if (platformData != null)
                    {
                        // 查找该造型的所有已放置实例
                        List<PlacedDrillShape> shapes = platformData.placedShapes
                            .FindAll(s => s.shapeId == option.upgradeId);
                        
                        if (shapes.Count > 0)
                        {
                            // 提升所有该造型实例的基础强度
                            // 注意：这里通过永久加成来实现，实际实现可能需要更复杂的机制
                            if (drill != null)
                            {
                                drill.permanentStrengthBonus += option.value;
                                Debug.Log($"钻机平台 {option.upgradeId} 基础强度提升: +{option.value}，当前总加成: {drill.permanentStrengthBonus}");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"尝试升级钻机平台失败，造型 {option.upgradeId} 未放置在平台上");
                        }
                    }
                }
                break;

            case UpgradeOptionType.DrillBitUnlock:
                // 解锁新钻头：将钻头加入库存
                DrillBitManager bitManager = DrillBitManager.Instance;
                if (bitManager != null && !string.IsNullOrEmpty(option.upgradeId))
                {
                    bitManager.UnlockBit(option.upgradeId);
                    Debug.Log($"解锁钻头: {option.upgradeId}，已加入钻头库存");
                }
                else
                {
                    Debug.LogWarning($"尝试解锁钻头失败，upgradeId 无效: {option.upgradeId}");
                }
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
    public string upgradeId;          // 升级ID（对于造型解锁，约定为 shapeId）
    public UpgradeOptionType type;
    public string name;
    public string description;
    public int value;
    public string iconPath;          // 图标路径（相对于Resources目录）
}
