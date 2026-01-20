using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

/// <summary>
/// 能源升级进度条组件：显示当前能源相对于下一次升级阈值的进度
/// 
/// Unity编辑器设置说明：
/// ====================
/// 1. 在场景的 Canvas 下创建一个空物体，命名为 "EnergyProgressBarRoot"
/// 
/// 2. 在 EnergyProgressBarRoot 下创建三个子对象：
///    - BarBackgroundBottom (Image组件)
///      * 使用 Sprite: Experience_bar_3 (路径: Assets/UI/Backgrounds/Experience_bar_3)
///      * RectTransform: 左对齐，设置合适的宽度和高度
///      * Image: Type = Simple, Preserve Aspect = false
///    
///    - BarFill (Image组件)
///      * 使用 Sprite: Experience_bar_2 (路径: Assets/UI/Backgrounds/Experience_bar_2)
///      * RectTransform: 左对齐，与底层相同的宽度和高度
///      * Image: Type = Simple, Preserve Aspect = false
///      * 注意：此对象的宽度会根据进度自动调整
///    
///    - BarForegroundTop (Image组件)
///      * 使用 Sprite: Experience_bar_1 (路径: Assets/UI/Backgrounds/Experience_bar_1)
///      * RectTransform: 左对齐，与底层相同的宽度和高度
///      * Image: Type = Simple, Preserve Aspect = false
/// 
/// 3. 可选：添加百分比文字显示
///    - 在 EnergyProgressBarRoot 下创建 TextMeshProUGUI 对象
///    - 设置合适的字体大小和位置
/// 
/// 4. 将 EnergyProgressBar 组件添加到 EnergyProgressBarRoot 上
///    - 在 Inspector 中拖拽三个 Image 组件到对应字段
///    - 可选：拖拽 TextMeshProUGUI 到 percentText 字段
/// 
/// 5. 在 GameScreen 组件中
///    - 将 EnergyProgressBarRoot 拖拽到 energyProgressBar 字段
/// 
/// 层级顺序（从下到上）：
/// - BarBackgroundBottom (底层，永久显示)
/// - BarFill (中间层，根据进度调整宽度)
/// - BarForegroundTop (顶层，永久显示)
/// </summary>
public class EnergyProgressBar : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private Image bottomImage;      // 底层图片 (Experience_bar_3)
    [SerializeField] private Image fillImage;        // 填充层图片 (Experience_bar_2)
    [SerializeField] private Image topImage;        // 顶层图片 (Experience_bar_1)
    [SerializeField] private TextMeshProUGUI percentText; // 可选：百分比文字显示
    
    [Header("设置")]
    [SerializeField] private bool showPercentText = true; // 是否显示百分比文字
    
    // 数据来源委托
    private Func<int> _getCurrentEnergy;
    private Func<List<int>> _getThresholds;
    private Func<int> _getNextThresholdIndex;
    
    // 缓存的最大宽度（用于计算进度宽度）
    private float _maxFillWidth;
    private RectTransform _fillRectTransform;
    
    private void Awake()
    {
        // 获取填充层的 RectTransform
        if (fillImage != null)
        {
            _fillRectTransform = fillImage.GetComponent<RectTransform>();
            if (_fillRectTransform != null)
            {
                // 记录初始宽度作为最大宽度
                _maxFillWidth = _fillRectTransform.sizeDelta.x;
            }
        }
    }
    
    /// <summary>
    /// 初始化进度条（注入数据来源）
    /// </summary>
    /// <param name="getCurrentEnergy">获取当前能源值的委托</param>
    /// <param name="getThresholds">获取阈值列表的委托</param>
    /// <param name="getNextThresholdIndex">获取下一个阈值索引的委托</param>
    public void Initialize(
        Func<int> getCurrentEnergy,
        Func<List<int>> getThresholds,
        Func<int> getNextThresholdIndex)
    {
        _getCurrentEnergy = getCurrentEnergy;
        _getThresholds = getThresholds;
        _getNextThresholdIndex = getNextThresholdIndex;
        
        // 初始化时更新一次
        UpdateProgress();
    }
    
    /// <summary>
    /// 更新进度条显示
    /// </summary>
    public void UpdateProgress()
    {
        if (_getCurrentEnergy == null || _getThresholds == null || _getNextThresholdIndex == null)
        {
            // 如果未初始化，尝试从 EnergyUpgradeManager 获取
            TryAutoInitialize();
        }
        
        if (_getCurrentEnergy == null || _getThresholds == null || _getNextThresholdIndex == null)
        {
            return; // 仍然无法获取数据，跳过更新
        }
        
        int currentEnergy = _getCurrentEnergy();
        List<int> thresholds = _getThresholds();
        int nextThresholdIndex = _getNextThresholdIndex();
        
        if (thresholds == null || thresholds.Count == 0)
        {
            // 没有阈值配置，显示0进度
            SetProgress(0f);
            UpdatePercentText(0f, currentEnergy, 0);
            return;
        }
        
        // 计算进度
        float progress = CalculateProgress(currentEnergy, thresholds, nextThresholdIndex);
        
        // 更新UI
        SetProgress(progress);
        
        // 更新百分比文字
        int nextThreshold = GetNextThreshold(thresholds, nextThresholdIndex);
        UpdatePercentText(progress, currentEnergy, nextThreshold);
    }
    
    /// <summary>
    /// 尝试自动初始化（从 EnergyUpgradeManager 获取数据）
    /// </summary>
    private void TryAutoInitialize()
    {
        EnergyUpgradeManager energyManager = EnergyUpgradeManager.Instance;
        if (energyManager == null)
        {
            return;
        }
        
        _getCurrentEnergy = () => energyManager.GetCurrentEnergy();
        _getThresholds = () =>
        {
            EnergyData data = energyManager.GetEnergyData();
            return data != null ? data.energyThresholds : null;
        };
        _getNextThresholdIndex = () =>
        {
            EnergyData data = energyManager.GetEnergyData();
            return data != null ? data.nextThresholdIndex : 0;
        };
    }
    
    /// <summary>
    /// 计算进度值（0-1）
    /// </summary>
    private float CalculateProgress(int currentEnergy, List<int> thresholds, int nextThresholdIndex)
    {
        if (thresholds == null || thresholds.Count == 0)
        {
            return 0f;
        }
        
        // 如果所有阈值都已触发
        if (nextThresholdIndex >= thresholds.Count)
        {
            return 1f; // 显示100%
        }
        
        int nextThreshold = thresholds[nextThresholdIndex];
        int prevThreshold = nextThresholdIndex > 0 ? thresholds[nextThresholdIndex - 1] : 0;
        
        // 如果当前能源已经达到或超过下一个阈值
        if (currentEnergy >= nextThreshold)
        {
            return 1f;
        }
        
        // 如果当前能源小于上一个阈值
        if (currentEnergy < prevThreshold)
        {
            return 0f;
        }
        
        // 计算在当前区间内的进度
        int range = nextThreshold - prevThreshold;
        if (range <= 0)
        {
            return 0f;
        }
        
        float progress = (float)(currentEnergy - prevThreshold) / range;
        return Mathf.Clamp01(progress);
    }
    
    /// <summary>
    /// 获取下一个阈值
    /// </summary>
    private int GetNextThreshold(List<int> thresholds, int nextThresholdIndex)
    {
        if (thresholds == null || thresholds.Count == 0)
        {
            return 0;
        }
        
        if (nextThresholdIndex >= thresholds.Count)
        {
            // 所有阈值都已触发，返回最后一个阈值
            return thresholds[thresholds.Count - 1];
        }
        
        return thresholds[nextThresholdIndex];
    }
    
    /// <summary>
    /// 设置进度值（0-1）
    /// </summary>
    private void SetProgress(float progress)
    {
        if (_fillRectTransform == null || _maxFillWidth <= 0)
        {
            return;
        }
        
        // 通过修改宽度来控制进度
        float newWidth = _maxFillWidth * progress;
        _fillRectTransform.sizeDelta = new Vector2(newWidth, _fillRectTransform.sizeDelta.y);
    }
    
    /// <summary>
    /// 更新百分比文字显示
    /// </summary>
    private void UpdatePercentText(float progress, int currentEnergy, int nextThreshold)
    {
        if (!showPercentText || percentText == null)
        {
            return;
        }
        
        int percent = Mathf.RoundToInt(progress * 100f);
        percentText.text = $"{currentEnergy} / {nextThreshold} ({percent}%)";
    }
}
