using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.IO;
using System;

/// <summary>
/// 游戏主界面：管理游戏UI显示和交互
/// </summary>
public class GameScreen : MonoBehaviour
{
    [Header("挖矿地图")]
    [SerializeField] private Transform miningMapContainer;
    [SerializeField] private GameObject tilePrefab; // 矿石瓦片预制体（如果使用预制体）

    [Header("信息面板")]
    [SerializeField] private TextMeshProUGUI taskNameText;
    [SerializeField] private TextMeshProUGUI taskProgressText;
    [SerializeField] private TextMeshProUGUI remainingTurnsText;
    [SerializeField] private TextMeshProUGUI debtInfoText;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI energyText;
    [SerializeField] private TextMeshProUGUI drillInfoText;

    [Header("操作按钮")]
    [SerializeField] private Button endTurnButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button autoMiningButton;          // 自动挖矿按钮
    [SerializeField] private TextMeshProUGUI autoMiningText;   // 自动挖矿按钮文字
    [SerializeField] private Button editDrillButton;           // 编辑钻头按钮
    
    [Header("状态显示")]
    [SerializeField] private TextMeshProUGUI miningStatusText; // 挖矿状态文字
    
    [Header("能源进度条")]
    [SerializeField] private EnergyProgressBar energyProgressBar; // 能源升级进度条

    private GameManager _gameManager;
    private MiningMapView _miningMapView;
    private UpgradeSelectionScreen _upgradeScreen;
    private SettingsScreen _settingsScreen;
    private DrillEditorScreen _drillEditorScreen;
    private TurnManager _turnManager;
    private EnergyUpgradeManager _energyManager;

    private void Awake()
    {
        _gameManager = GameManager.Instance;
    }

    private void OnEnable()
    {
        // 确保Canvas在OnEnable时是激活的
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && !canvas.gameObject.activeInHierarchy)
        {
            canvas.gameObject.SetActive(true);
        }
    }

    private void Start()
    {
        // 初始化挖矿地图视图
        if (miningMapContainer != null)
        {
            _miningMapView = miningMapContainer.GetComponent<MiningMapView>();
            if (_miningMapView == null)
            {
                _miningMapView = miningMapContainer.gameObject.AddComponent<MiningMapView>();
            }
        }

        // 查找升级选择界面
        _upgradeScreen = FindObjectOfType<UpgradeSelectionScreen>();
        if (_upgradeScreen != null)
        {
            // 确保不会禁用Canvas（UpgradeSelectionScreen应该在Canvas下）
            if (_upgradeScreen.transform.root != GetComponentInParent<Canvas>()?.transform)
            {
                _upgradeScreen.gameObject.SetActive(false);
            }
        }

        // 查找设置界面（包含未激活对象）
        SettingsScreen[] allSettingsScreens = Resources.FindObjectsOfTypeAll<SettingsScreen>();
        _settingsScreen = null;
        for (int i = 0; i < allSettingsScreens.Length; i++)
        {
            SettingsScreen candidate = allSettingsScreens[i];
            if (candidate != null && candidate.gameObject.scene.IsValid())
            {
                _settingsScreen = candidate;
                break;
            }
        }
        if (_settingsScreen == null)
        {
            Debug.LogWarning("GameScreen: 未找到SettingsScreen，设置按钮可能无法正常工作。请确保场景中存在SettingsScreen GameObject。");
        }

        // 设置按钮事件
        if (endTurnButton != null)
        {
            // 确保按钮是激活的
            if (!endTurnButton.gameObject.activeInHierarchy)
            {
                endTurnButton.gameObject.SetActive(true);
            }
            endTurnButton.onClick.AddListener(OnEndTurnButtonClicked);
        }

        if (settingsButton != null)
        {
            // 确保按钮是激活的
            if (!settingsButton.gameObject.activeInHierarchy)
            {
                settingsButton.gameObject.SetActive(true);
            }
            settingsButton.onClick.AddListener(OnSettingsButtonClicked);
        }
        
        // 设置自动挖矿按钮
        if (autoMiningButton != null)
        {
            if (!autoMiningButton.gameObject.activeInHierarchy)
            {
                autoMiningButton.gameObject.SetActive(true);
            }
            autoMiningButton.onClick.AddListener(OnAutoMiningButtonClicked);
        }
        
        // 设置钻头编辑按钮
        if (editDrillButton != null)
        {
            if (!editDrillButton.gameObject.activeInHierarchy)
            {
                editDrillButton.gameObject.SetActive(true);
            }
            editDrillButton.onClick.AddListener(OnEditDrillButtonClicked);
        }
        
        // 查找钻头编辑界面
        _drillEditorScreen = FindObjectOfType<DrillEditorScreen>(true);
        
        // 获取回合管理器并订阅事件
        _turnManager = TurnManager.Instance;
        if (_turnManager != null)
        {
            _turnManager.OnAutoMiningChanged.AddListener(OnAutoMiningChanged);
            _turnManager.OnLayerSwitched.AddListener(OnLayerSwitched);
        }
        
        // 获取能源管理器
        _energyManager = EnergyUpgradeManager.Instance;
        
        // 初始化能源进度条
        if (energyProgressBar != null && _energyManager != null)
        {
            energyProgressBar.Initialize(
                () => _energyManager.GetCurrentEnergy(),
                () =>
                {
                    EnergyData data = _energyManager.GetEnergyData();
                    return data != null ? data.energyThresholds : null;
                },
                () =>
                {
                    EnergyData data = _energyManager.GetEnergyData();
                    return data != null ? data.nextThresholdIndex : 0;
                }
            );
        }

        // 订阅游戏事件
        if (_gameManager != null)
        {
            _gameManager.OnGameInitialized.AddListener(OnGameInitialized);
            _gameManager.OnVictory.AddListener(OnVictory);
            _gameManager.OnGameOver.AddListener(OnGameOver);
            _gameManager.OnGameStateChanged.AddListener(OnGameStateChanged);
            _gameManager.OnTurnProcessingStarted.AddListener(OnTurnProcessingStarted);
            _gameManager.OnTurnProcessingCompleted.AddListener(OnTurnProcessingCompleted);
        }

        // 订阅能源升级事件
        EnergyUpgradeManager energyManager = EnergyUpgradeManager.Instance;
        if (energyManager != null)
        {
            energyManager.OnUpgradeAvailable.AddListener(OnEnergyUpgradeAvailable);
        }

        // 强制激活所有UI元素
        ForceActivateAllUIElements();

        // 应用动态字体到所有文本组件
        ApplyDynamicFont();

        // 初始化UI显示
        UpdateUI();
    }

    /// <summary>
    /// 尝试设置中文字体
    /// </summary>
    private void TrySetChineseFont()
    {
        // 尝试从Resources加载中文字体（可能的名称）
        string[] possibleFontNames = {
            "Fonts & Materials/微软雅黑 SDF",
            "Fonts & Materials/Microsoft YaHei SDF",
            "Fonts & Materials/ChineseFont SDF",
            "Fonts & Materials/YaHei SDF"
        };

        TMP_FontAsset chineseFont = null;
        foreach (string fontName in possibleFontNames)
        {
            chineseFont = Resources.Load<TMP_FontAsset>(fontName);
            if (chineseFont != null)
            {
                break;
            }
        }

        if (chineseFont == null)
        {
            Debug.LogWarning("未找到中文字体资源，请确保字体资源已创建并放在 Resources/Fonts & Materials/ 目录下");
            return;
        }

        // 检查字符查找表
        if (chineseFont.characterLookupTable == null || chineseFont.characterLookupTable.Count == 0)
        {
            Debug.LogWarning($"字体资源 {chineseFont.name} 的字符查找表为空，请重新生成字体资源并确保包含中文字符");
            return;
        }

        // 测试字符是否在字体中
        string testChar = "任";
        bool hasTestChar = chineseFont.characterLookupTable.ContainsKey(testChar[0]);

        if (!hasTestChar)
        {
            Debug.LogWarning($"字体资源 {chineseFont.name} 不包含测试字符 '{testChar}'，请重新生成字体资源并确保包含所有需要的中文字符");
        }

        // 应用到所有文本组件
        int appliedCount = 0;
        TextMeshProUGUI[] allTexts = GetComponentsInChildren<TextMeshProUGUI>(true);
        
        foreach (TextMeshProUGUI text in allTexts)
        {
            if (text != null)
            {
                text.font = chineseFont;
                appliedCount++;
            }
        }

        Debug.Log($"已为 {appliedCount} 个 TextMeshProUGUI 组件设置中文字体: {chineseFont.name}");
    }

    /// <summary>
    /// 强制激活所有UI元素
    /// </summary>
    private void ForceActivateAllUIElements()
    {
        Canvas canvas = GetComponentInParent<Canvas>();

        // 激活所有文本元素
        if (taskNameText != null)
        {
            if (!taskNameText.gameObject.activeInHierarchy)
            {
                taskNameText.gameObject.SetActive(true);
            }
            // 确保文本有内容且可见
            if (string.IsNullOrEmpty(taskNameText.text))
            {
                taskNameText.text = "任务: 测试文本";
            }
            // 确保文本颜色可见
            if (taskNameText.color.a < 0.1f)
            {
                taskNameText.color = new Color(1f, 1f, 1f, 1f); // 白色，完全不透明
            }
        }
        if (taskProgressText != null && !taskProgressText.gameObject.activeInHierarchy)
        {
            taskProgressText.gameObject.SetActive(true);
        }
        if (remainingTurnsText != null && !remainingTurnsText.gameObject.activeInHierarchy)
        {
            remainingTurnsText.gameObject.SetActive(true);
        }
        if (debtInfoText != null && !debtInfoText.gameObject.activeInHierarchy)
        {
            debtInfoText.gameObject.SetActive(true);
        }
        if (moneyText != null && !moneyText.gameObject.activeInHierarchy)
        {
            moneyText.gameObject.SetActive(true);
        }
        if (energyText != null && !energyText.gameObject.activeInHierarchy)
        {
            energyText.gameObject.SetActive(true);
        }
        if (drillInfoText != null && !drillInfoText.gameObject.activeInHierarchy)
        {
            drillInfoText.gameObject.SetActive(true);
        }

        // 激活所有按钮
        if (endTurnButton != null)
        {
            if (!endTurnButton.gameObject.activeInHierarchy)
            {
                endTurnButton.gameObject.SetActive(true);
            }
            // 确保按钮的Image组件可见
            UnityEngine.UI.Image buttonImage = endTurnButton.GetComponent<UnityEngine.UI.Image>();
            if (buttonImage != null && buttonImage.color.a < 0.1f)
            {
                buttonImage.color = new Color(1f, 1f, 1f, 1f);
            }
        }
        if (settingsButton != null)
        {
            if (!settingsButton.gameObject.activeInHierarchy)
            {
                settingsButton.gameObject.SetActive(true);
            }
            // 确保按钮的Image组件可见
            UnityEngine.UI.Image buttonImage = settingsButton.GetComponent<UnityEngine.UI.Image>();
            if (buttonImage != null && buttonImage.color.a < 0.1f)
            {
                buttonImage.color = new Color(1f, 1f, 1f, 1f);
            }
        }

        // 激活挖矿地图容器
        if (miningMapContainer != null && !miningMapContainer.gameObject.activeInHierarchy)
        {
            miningMapContainer.gameObject.SetActive(true);
        }

        // 确保Canvas本身也是激活的
        if (canvas != null)
        {
            if (!canvas.gameObject.activeInHierarchy)
            {
                canvas.gameObject.SetActive(true);
            }
            // 确保Canvas的Render Mode正确
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay && canvas.renderMode != RenderMode.ScreenSpaceCamera)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            // 确保Canvas组件本身是启用的
            canvas.enabled = true;
        }
    }

    private void Update()
    {
        // 每帧更新UI（可以优化为事件驱动）
        UpdateUI();
    }

    /// <summary>
    /// 更新UI显示
    /// </summary>
    private void UpdateUI()
    {
        if (_gameManager == null || !_gameManager.IsGameInitialized())
            return;

        GameStateInfo state = _gameManager.GetGameStateInfo();
        if (state == null) return;

        // 更新任务信息
        if (state.taskData != null)
        {
            TaskConfig currentTask = TaskManager.Instance.GetCurrentTask();
            if (currentTask != null && taskNameText != null)
            {
                // 确保文本组件是激活的
                if (!taskNameText.gameObject.activeInHierarchy)
                {
                    taskNameText.gameObject.SetActive(true);
                }
                taskNameText.text = $"任务: {currentTask.taskName}";
            }

            if (taskProgressText != null)
            {
                float progress = TaskManager.Instance.GetTaskProgress();
                taskProgressText.text = $"进度: {state.taskData.currentDebtPaid} / {state.taskData.targetDebtAmount} ({progress * 100:F1}%)";
            }

            if (remainingTurnsText != null)
            {
                remainingTurnsText.text = $"剩余回合: {state.remainingTurns} / {state.taskData.maxTurns}";
            }
        }

        // 更新债务信息
        if (state.debtData != null && debtInfoText != null)
        {
            debtInfoText.text = $"债务: {state.debtData.paidDebt} / {state.debtData.totalDebt} (剩余: {state.debtData.RemainingDebt})";
        }

        // 更新资源信息
        if (moneyText != null)
        {
            if (!moneyText.gameObject.activeInHierarchy)
            {
                moneyText.gameObject.SetActive(true);
            }
            moneyText.text = $"金钱: {state.currentMoney}";
        }

        if (energyText != null)
        {
            if (!energyText.gameObject.activeInHierarchy)
            {
                energyText.gameObject.SetActive(true);
            }
            energyText.text = $"能源: {state.currentEnergy}";
        }

        // 更新钻头信息
        if (state.drillData != null && drillInfoText != null)
        {
            drillInfoText.text = $"钻头: {state.drillData.drillName}\n强度: {state.drillData.GetEffectiveStrength()}\n范围: {state.drillData.GetEffectiveRange().x}x{state.drillData.GetEffectiveRange().y}";
        }

        // 更新挖矿地图
        if (_miningMapView != null && state.miningData != null)
        {
            int currentLayer = state.miningData.currentDepth > 0 ? state.miningData.currentDepth : 1;
            
            
            _miningMapView.UpdateMap(currentLayer);
            // 刷新高亮状态（确保钻头范围变化时高亮也会更新）
            _miningMapView.RefreshHighlight();
        }
        
        // 更新能源进度条
        if (energyProgressBar != null)
        {
            energyProgressBar.UpdateProgress();
        }
    }

    /// <summary>
    /// 结束回合按钮点击
    /// </summary>
    private void OnEndTurnButtonClicked()
    {
        if (_gameManager != null)
        {
            _gameManager.OnEndTurnButtonClicked();
        }
    }

    /// <summary>
    /// 设置按钮点击
    /// </summary>
    private void OnSettingsButtonClicked()
    {
        if (_settingsScreen != null)
        {
            _settingsScreen.Show();
        }
        else
        {
            Debug.LogWarning("GameScreen: SettingsScreen未找到，无法打开设置界面。");
        }
    }

    /// <summary>
    /// 游戏初始化完成
    /// </summary>
    private void OnGameInitialized()
    {
        UpdateUI();
        Debug.Log("游戏界面已初始化");
    }

    /// <summary>
    /// 游戏胜利
    /// </summary>
    private void OnVictory()
    {
        Debug.Log("游戏胜利！");
        // 可以显示胜利界面
    }

    /// <summary>
    /// 游戏结束
    /// </summary>
    private void OnGameOver()
    {
        Debug.Log("游戏结束");
        // 可以显示失败界面
    }

    /// <summary>
    /// 能源升级可用
    /// </summary>
    private void OnEnergyUpgradeAvailable(List<EnergyUpgradeOption> options)
    {
        if (_upgradeScreen != null)
        {
            _upgradeScreen.ShowUpgradeOptions(options);
        }
    }

    /// <summary>
    /// 游戏状态变化回调（用于更新UI）
    /// </summary>
    private void OnGameStateChanged()
    {
        UpdateUI();
    }

    /// <summary>
    /// 应用动态字体到所有文本组件
    /// </summary>
    private void ApplyDynamicFont()
    {
        FontHelper.ApplyFontToGameObject(gameObject);
    }

    /// <summary>
    /// 回合处理开始（动画开始）
    /// </summary>
    private void OnTurnProcessingStarted()
    {
        // 禁用结束回合按钮，防止重复点击
        if (endTurnButton != null)
        {
            endTurnButton.interactable = false;
        }
    }

    /// <summary>
    /// 回合处理完成（动画结束）
    /// </summary>
    private void OnTurnProcessingCompleted()
    {
        // 重新启用结束回合按钮（仅在非自动挖矿模式下）
        if (endTurnButton != null)
        {
            bool isAutoMining = _turnManager != null && _turnManager.IsAutoMiningEnabled();
            endTurnButton.interactable = !isAutoMining;
        }
    }
    
    /// <summary>
    /// 自动挖矿按钮点击
    /// </summary>
    private void OnAutoMiningButtonClicked()
    {
        if (_turnManager != null)
        {
            _turnManager.ToggleAutoMining();
        }
    }
    
    /// <summary>
    /// 编辑钻头按钮点击
    /// </summary>
    private void OnEditDrillButtonClicked()
    {
        // 检查是否允许编辑（非自动挖矿模式）
        if (_turnManager != null && _turnManager.IsAutoMiningEnabled())
        {
            Debug.Log("自动挖矿中，无法编辑钻头");
            if (miningStatusText != null)
            {
                miningStatusText.text = "自动挖矿中，无法编辑钻头";
                miningStatusText.color = new Color(1f, 0.5f, 0.5f, 1f);
            }
            return;
        }
        
        if (_drillEditorScreen != null)
        {
            _drillEditorScreen.Show();
        }
        else
        {
            Debug.LogWarning("GameScreen: DrillEditorScreen未找到，无法打开钻头编辑界面。");
        }
    }
    
    /// <summary>
    /// 自动挖矿状态变化回调
    /// </summary>
    private void OnAutoMiningChanged(bool isEnabled)
    {
        UpdateAutoMiningUI(isEnabled);
        
        // 更新结束回合按钮状态
        if (endTurnButton != null)
        {
            endTurnButton.interactable = !isEnabled;
        }
        
        // 更新钻头编辑按钮状态
        if (editDrillButton != null)
        {
            editDrillButton.interactable = !isEnabled;
        }
    }
    
    /// <summary>
    /// 切换到新层回调
    /// </summary>
    private void OnLayerSwitched()
    {
        // 切换到新层时更新挖矿状态文字
        UpdateMiningStatusText();
    }
    
    /// <summary>
    /// 更新自动挖矿UI
    /// </summary>
    private void UpdateAutoMiningUI(bool isAutoMiningEnabled)
    {
        // 更新按钮文字
        if (autoMiningText != null)
        {
            autoMiningText.text = isAutoMiningEnabled ? "停止自动挖矿" : "自动向下挖矿";
        }
        else if (autoMiningButton != null)
        {
            // 如果没有单独的文字组件，尝试获取按钮子对象的文字
            TextMeshProUGUI buttonText = autoMiningButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = isAutoMiningEnabled ? "停止自动挖矿" : "自动向下挖矿";
            }
        }
        
        // 更新按钮颜色表示状态
        if (autoMiningButton != null)
        {
            UnityEngine.UI.Image buttonImage = autoMiningButton.GetComponent<UnityEngine.UI.Image>();
            if (buttonImage != null)
            {
                buttonImage.color = isAutoMiningEnabled ? new Color(0.8f, 0.4f, 0.4f, 1f) : new Color(0.4f, 0.8f, 0.4f, 1f);
            }
        }
        
        // 更新状态文字
        UpdateMiningStatusText();
    }
    
    /// <summary>
    /// 更新挖矿状态文字
    /// </summary>
    private void UpdateMiningStatusText()
    {
        if (miningStatusText == null) return;
        
        bool isAutoMining = _turnManager != null && _turnManager.IsAutoMiningEnabled();
        bool isProcessing = _turnManager != null && _turnManager.IsProcessingTurn();
        
        if (isAutoMining)
        {
            miningStatusText.text = isProcessing ? "自动挖矿中..." : "自动挖矿已开启";
            miningStatusText.color = new Color(0.4f, 0.8f, 0.4f, 1f); // 绿色
        }
        else
        {
            miningStatusText.text = "可以调整钻头位置";
            miningStatusText.color = new Color(1f, 1f, 1f, 1f); // 白色
        }
    }
}
