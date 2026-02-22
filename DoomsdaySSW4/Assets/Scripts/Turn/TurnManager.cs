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
    /// 结束当前回合（协程版本）。钻机以60度/秒做360度旋转扫掠攻击。
    /// 若一圈后扫掠范围内所有矿石都被挖空，则自动刷出下一层继续旋转，
    /// 直到旋转一圈后仍有矿石残留才算本回合结束。
    /// </summary>
    public IEnumerator EndTurnCoroutine()
    {
        if (_isProcessingTurn)
        {
            yield break;
        }
        _isProcessingTurn = true;
        
        if (_currentTurn == 0)
        {
            StartTurn();
        }

        Debug.Log($"回合 {_currentTurn} 结束，开始执行旋转挖掘");

        DrillData drill = _drillManager.GetCurrentDrill();
        MiningResult accumulatedResult = new MiningResult();
        
        if (drill != null)
        {
            MiningData miningData = _miningManager.GetMiningData();
            if (miningData != null)
            {
                MiningMapView miningMapView = FindObjectOfType<MiningMapView>();
                bool continueRotation = true;

                while (continueRotation)
                {
                    int currentLayerDepth = miningData.currentDepth >= 1 ? miningData.currentDepth : 1;

                    // 计算每 60° 扫掠攻击结果（用于动画按角度触发）
                    DrillAttackCalculator calculator = DrillAttackCalculator.Instance;
                    CircularSweepResultPer60 sweepResult = calculator.CalculateCircularSweepAttackMapPer60(
                        drill, MiningManager.LAYER_WIDTH, MiningManager.LAYER_HEIGHT);

                    // 执行挖矿逻辑（按 0°/60°/120°/180°/240°/300° 顺序施加伤害）
                    MiningResult result = _miningManager.AttackOresInRange(drill, currentLayerDepth);

                    // 播放360度旋转动画，过程中按角度触发攻击特效
                    if (miningMapView != null)
                    {
                        yield return miningMapView.PlayRotationMiningAnimation(
                            60f, sweepResult.angleToTargets, result.attackedTiles);

                        // 播放金钱飞行特效
                        MiningEffectsManager effectsManager = MiningEffectsManager.Instance;
                        if (effectsManager != null && result.attackedTiles != null && result.attackedTiles.Count > 0)
                        {
                            yield return effectsManager.PlayMiningEffectSequence(result.attackedTiles, miningMapView);
                        }

                        miningMapView.UpdateMap(currentLayerDepth);
                    }

                    // 累加结果
                    if (result != null)
                    {
                        accumulatedResult.moneyGained += result.moneyGained;
                        accumulatedResult.energyGained += result.energyGained;
                        accumulatedResult.minedOres.AddRange(result.minedOres);
                        accumulatedResult.partiallyDamagedOres.AddRange(result.partiallyDamagedOres);
                        accumulatedResult.attackedTiles.AddRange(result.attackedTiles);
                    }

                    // 检查扫掠范围内是否还有未挖掉的矿石
                    bool allSweptOresMined = _miningManager.IsLayerFullyMined(currentLayerDepth);

                    if (allSweptOresMined)
                    {
                        // 所有矿石挖空 -> 切换到下一层，继续旋转
                        bool switched = _miningManager.TrySwitchToNextLayer();
                        if (switched)
                        {
                            Debug.Log($"圆环扫掠一圈后所有矿石已挖空，自动切换到下一层");
                            GameManager gameManager = GameManager.Instance;
                            if (gameManager != null)
                            {
                                gameManager.NotifyGameStateChanged();
                            }
                            OnLayerSwitched?.Invoke();

                            if (miningMapView != null)
                            {
                                int newDepth = miningData.currentDepth;
                                miningMapView.UpdateMap(newDepth);
                            }
                            // continueRotation = true, 继续 while 循环
                        }
                        else
                        {
                            // 已到最大层数，无法继续
                            continueRotation = false;
                        }
                    }
                    else
                    {
                        // 有残留矿石，本回合结束
                        continueRotation = false;
                    }
                }

                // 应用矿石发现能力加成
                if (_energyManager != null)
                {
                    int currentLayerDepth = miningData.currentDepth >= 1 ? miningData.currentDepth : 1;
                    int discoveryBonus = _energyManager.GetOreDiscoveryBonus();
                    if (discoveryBonus > 0)
                    {
                        _miningManager.DiscoverAdditionalOres(currentLayerDepth, discoveryBonus);
                    }
                }

                // 处理累计挖矿结果
                if (accumulatedResult.moneyGained > 0 || accumulatedResult.energyGained > 0)
                {
                    Debug.Log($"本回合挖矿结果: 金钱 +{accumulatedResult.moneyGained}, 能源 +{accumulatedResult.energyGained}");

                    if (accumulatedResult.moneyGained > 0)
                    {
                        _debtManager.AddMoneyAndPayDebt(accumulatedResult.moneyGained);
                    }

                    if (accumulatedResult.energyGained > 0)
                    {
                        _energyManager.AddEnergy(accumulatedResult.energyGained);
                    }
                }
            }
        }

        // 更新任务进度
        if (_taskManager != null)
        {
            int totalPaidDebt = _debtManager.GetPaidDebtAmount();
            _taskManager.UpdateDebtProgress(totalPaidDebt);

            if (_taskManager.CheckTaskCompletion())
            {
                _taskManager.CompleteCurrentTask();
            }
        }

        if (_taskManager != null)
        {
            _taskManager.CheckTaskFailure(_currentTurn);
        }

        OnTurnEnded?.Invoke(_currentTurn);

        if (!IsTurnLimitReached())
        {
            StartTurn();
        }
        else
        {
            Debug.LogWarning($"已达到回合限制: {_maxTurns}");
            
            if (_taskManager != null)
            {
                TaskData taskData = _taskManager.GetTaskData();
                if (taskData != null && !taskData.isTaskCompleted && taskData.targetDebtAmount > 0)
                {
                    _debtManager.AddDebt(taskData.targetDebtAmount);
                }
            }

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
    /// 重置当前回合数（由任务系统调用，当切换任务时）
    /// </summary>
    public void ResetCurrentTurn()
    {
        _currentTurn = 0;
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
