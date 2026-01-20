using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 回合管理器：负责回合循环、回合限制检查
/// </summary>
public class TurnManager : MonoBehaviour
{
    private static TurnManager _instance;
    public static TurnManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("TurnManager");
                _instance = go.AddComponent<TurnManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private int _currentTurn = 0;
    private int _maxTurns = 0;
    private TaskManager _taskManager;
    private MiningManager _miningManager;
    private DrillManager _drillManager;
    private DebtManager _debtManager;
    private EnergyUpgradeManager _energyManager;
    
    // 自动挖矿状态
    private bool _isAutoMiningEnabled = false;
    private bool _isProcessingTurn = false;  // 防止重复执行

    // 回合事件
    public UnityEvent<int> OnTurnStarted = new UnityEvent<int>(); // currentTurn
    public UnityEvent<int> OnTurnEnded = new UnityEvent<int>(); // currentTurn
    public UnityEvent<bool> OnAutoMiningChanged = new UnityEvent<bool>(); // 自动挖矿状态变化
    public UnityEvent OnLayerSwitched = new UnityEvent(); // 切换到新层时触发（用于UI更新）

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
        _taskManager = TaskManager.Instance;
        _miningManager = MiningManager.Instance;
        _drillManager = DrillManager.Instance;
        _debtManager = DebtManager.Instance;
        _energyManager = EnergyUpgradeManager.Instance;
    }

    /// <summary>
    /// 初始化回合系统
    /// </summary>
    public void Initialize(int maxTurns)
    {
        _currentTurn = 0;
        _maxTurns = maxTurns;
        Debug.Log($"回合系统初始化，最大回合数: {_maxTurns}");
    }

    /// <summary>
    /// 开始新回合
    /// </summary>
    public void StartTurn()
    {
        _currentTurn++;
        OnTurnStarted?.Invoke(_currentTurn);
        Debug.Log($"回合 {_currentTurn} 开始");
    }

    /// <summary>
    /// 结束当前回合（协程版本，支持等待动画完成）
    /// </summary>
    public IEnumerator EndTurnCoroutine()
    {
        // 防止重复执行
        if (_isProcessingTurn)
        {
            yield break;
        }
        _isProcessingTurn = true;
        
        if (_currentTurn == 0)
        {
            // 第一回合，先开始
            StartTurn();
        }

        Debug.Log($"回合 {_currentTurn} 结束，开始执行挖矿逻辑");

        // 先获取要攻击的格子列表（不造成伤害，用于动效）
        DrillData drill = _drillManager.GetCurrentDrill();
        MiningResult result = null;
        bool layerSwitched = false;
        
        if (drill != null)
        {
            MiningData miningData = _miningManager.GetMiningData();
            if (miningData != null)
            {
                int currentLayerDepth = miningData.currentDepth >= 1 ? miningData.currentDepth : 1;
                
                // 先执行实际的挖矿逻辑（造成伤害），获取攻击结果
                result = _miningManager.AttackOresInRange(drill, currentLayerDepth);
                
                // 获取MiningMapView引用
                MiningMapView miningMapView = FindObjectOfType<MiningMapView>();
                
                // 播放晃动动画（使用攻击结果中的格子信息，包含isFullyMined状态）
                if (result != null && result.attackedTiles != null && result.attackedTiles.Count > 0)
                {
                    if (miningMapView != null)
                    {
                        // 晃动动画（带红色高亮反馈）
                        yield return miningMapView.PlayShakeAnimation(result.attackedTiles);
                        
                        // 播放金钱飞行特效（对于被完全挖掉的格子）
                        MiningEffectsManager effectsManager = MiningEffectsManager.Instance;
                        if (effectsManager != null)
                        {
                            yield return effectsManager.PlayMiningEffectSequence(result.attackedTiles, miningMapView);
                        }
                        
                        // 刷新地图显示（更新已挖掉的格子）
                        miningMapView.UpdateMap(currentLayerDepth);
                    }
                }
                
                // 应用矿石发现能力加成（每回合额外发现矿石）
                if (_energyManager != null)
                {
                    int discoveryBonus = _energyManager.GetOreDiscoveryBonus();
                    if (discoveryBonus > 0)
                    {
                        // 额外发现矿石（随机揭示当前层未揭示的矿石）
                        _miningManager.DiscoverAdditionalOres(currentLayerDepth, discoveryBonus);
                    }
                }

                // 处理挖矿结果
                if (result != null && (result.moneyGained > 0 || result.energyGained > 0))
                {
                    Debug.Log($"本回合挖矿结果: 金钱 +{result.moneyGained}, 能源 +{result.energyGained}");

                    // 金钱转化为还债
                    if (result.moneyGained > 0)
                    {
                        _debtManager.AddMoneyAndPayDebt(result.moneyGained);
                    }

                    // 能源累计
                    if (result.energyGained > 0)
                    {
                        _energyManager.AddEnergy(result.energyGained);
                    }
                }

                // 检查当前层是否挖完，如果挖完则切换到下一层
                layerSwitched = _miningManager.TrySwitchToNextLayer();
                if (layerSwitched)
                {
                    // 通知GameManager更新UI（触发UI刷新以显示新层）
                    GameManager gameManager = GameManager.Instance;
                    if (gameManager != null)
                    {
                        gameManager.NotifyGameStateChanged();
                    }
                    
                    // 触发层切换事件
                    OnLayerSwitched?.Invoke();
                }
                else if (_isAutoMiningEnabled)
                {
                    // 当前层未挖完，无法进入下一层，关闭自动挖矿
                    Debug.Log("当前层未挖完，无法进入下一层，自动关闭自动挖矿");
                    _isAutoMiningEnabled = false;
                    OnAutoMiningChanged?.Invoke(false);
                }
            }
        }

        // 更新任务进度
        if (_taskManager != null)
        {
            int totalPaidDebt = _debtManager.GetPaidDebtAmount();
            _taskManager.UpdateDebtProgress(totalPaidDebt);

            // 检查任务完成
            if (_taskManager.CheckTaskCompletion())
            {
                _taskManager.CompleteCurrentTask();
            }
        }

        // 检查任务失败
        if (_taskManager != null)
        {
            _taskManager.CheckTaskFailure(_currentTurn);
        }

        OnTurnEnded?.Invoke(_currentTurn);

        // 开始下一回合
        if (!IsTurnLimitReached())
        {
            StartTurn();
        }
        else
        {
            Debug.LogWarning($"已达到回合限制: {_maxTurns}");
            // 达到回合限制，关闭自动挖矿
            if (_isAutoMiningEnabled)
            {
                _isAutoMiningEnabled = false;
                OnAutoMiningChanged?.Invoke(false);
            }
        }
        
        _isProcessingTurn = false;
    }

    /// <summary>
    /// 结束当前回合（保持向后兼容，内部调用协程版本）
    /// </summary>
    public void EndTurn()
    {
        StartCoroutine(EndTurnCoroutine());
    }

    /// <summary>
    /// 获取当前回合数
    /// </summary>
    public int GetCurrentTurn()
    {
        return _currentTurn;
    }

    /// <summary>
    /// 获取剩余回合数
    /// </summary>
    public int GetRemainingTurns()
    {
        return Mathf.Max(0, _maxTurns - _currentTurn);
    }

    /// <summary>
    /// 检查回合限制
    /// </summary>
    public bool IsTurnLimitReached()
    {
        return _currentTurn >= _maxTurns;
    }

    /// <summary>
    /// 设置最大回合数（由任务系统调用）
    /// </summary>
    public void SetMaxTurns(int maxTurns)
    {
        _maxTurns = maxTurns;
    }
    
    /// <summary>
    /// 获取自动挖矿状态
    /// </summary>
    public bool IsAutoMiningEnabled()
    {
        return _isAutoMiningEnabled;
    }
    
    /// <summary>
    /// 设置自动挖矿状态
    /// </summary>
    public void SetAutoMining(bool enabled)
    {
        if (_isAutoMiningEnabled != enabled)
        {
            _isAutoMiningEnabled = enabled;
            OnAutoMiningChanged?.Invoke(_isAutoMiningEnabled);
            Debug.Log($"自动挖矿状态: {(_isAutoMiningEnabled ? "开启" : "关闭")}");
            
            // 如果刚开启自动挖矿且当前没有在处理回合，立即开始挖矿
            if (_isAutoMiningEnabled && !_isProcessingTurn)
            {
                StartCoroutine(AutoMiningLoop());
            }
        }
    }
    
    /// <summary>
    /// 切换自动挖矿状态
    /// </summary>
    public void ToggleAutoMining()
    {
        SetAutoMining(!_isAutoMiningEnabled);
    }
    
    /// <summary>
    /// 检查当前是否正在处理回合
    /// </summary>
    public bool IsProcessingTurn()
    {
        return _isProcessingTurn;
    }
    
    /// <summary>
    /// 自动挖矿循环
    /// </summary>
    private IEnumerator AutoMiningLoop()
    {
        while (_isAutoMiningEnabled && !IsTurnLimitReached())
        {
            yield return StartCoroutine(EndTurnCoroutine());
            
            // 短暂延迟，让UI有时间更新
            yield return new WaitForSeconds(0.1f);
        }
    }
}
