using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 钻头管理器：管理钻头道具的解锁和库存
/// </summary>
public class DrillBitManager : MonoBehaviour
{
    private static DrillBitManager _instance;
    public static DrillBitManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("DrillBitManager");
                _instance = go.AddComponent<DrillBitManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private ConfigManager _configManager;
    
    /// <summary>
    /// 已解锁的钻头ID列表（库存）
    /// </summary>
    private List<string> _unlockedBitIds = new List<string>();

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
    /// 获取可用的钻头列表（库存）
    /// </summary>
    public List<string> GetAvailableBits()
    {
        return new List<string>(_unlockedBitIds);
    }

    /// <summary>
    /// 解锁新钻头（加入库存）
    /// </summary>
    public void UnlockBit(string bitId)
    {
        if (string.IsNullOrEmpty(bitId))
        {
            Debug.LogWarning("尝试解锁空的钻头ID");
            return;
        }

        if (!_unlockedBitIds.Contains(bitId))
        {
            _unlockedBitIds.Add(bitId);
            Debug.Log($"钻头已解锁: {bitId}");
        }
    }

    /// <summary>
    /// 检查钻头是否已解锁
    /// </summary>
    public bool IsBitUnlocked(string bitId)
    {
        return _unlockedBitIds.Contains(bitId);
    }

    /// <summary>
    /// 获取钻头配置
    /// </summary>
    public DrillBitConfig GetBitConfig(string bitId)
    {
        if (_configManager == null)
        {
            _configManager = ConfigManager.Instance;
        }
        
        return _configManager?.GetDrillBitConfig(bitId);
    }

    /// <summary>
    /// 获取所有已解锁的钻头配置
    /// </summary>
    public List<DrillBitConfig> GetUnlockedBitConfigs()
    {
        List<DrillBitConfig> configs = new List<DrillBitConfig>();
        
        foreach (var bitId in _unlockedBitIds)
        {
            DrillBitConfig config = GetBitConfig(bitId);
            if (config != null)
            {
                configs.Add(config);
            }
        }
        
        return configs;
    }

    /// <summary>
    /// 初始化钻头库存（从船只配置或存档加载）
    /// </summary>
    public void InitializeBits(List<string> initialBitIds = null)
    {
        _unlockedBitIds.Clear();
        
        if (initialBitIds != null)
        {
            foreach (var bitId in initialBitIds)
            {
                UnlockBit(bitId);
            }
        }
        
        Debug.Log($"钻头管理器初始化完成，已解锁 {_unlockedBitIds.Count} 个钻头");
    }

    /// <summary>
    /// 重置本关解锁（任务完成后）
    /// </summary>
    public void ResetLevelUnlocks()
    {
        // 注意：这里只清空本关解锁的钻头，永久解锁的钻头应该保留
        // 具体实现可以根据需求调整，例如区分永久解锁和本关解锁
        // 当前实现：清空所有，由初始化重新加载
        _unlockedBitIds.Clear();
        Debug.Log("钻头本关解锁已重置");
    }
}
