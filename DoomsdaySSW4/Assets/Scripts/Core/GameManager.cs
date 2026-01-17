using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// 游戏主管理器：整合所有系统、管理游戏流程
/// </summary>
public class GameManager : MonoBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("GameManager");
                _instance = go.AddComponent<GameManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // 各个管理器引用
    private ConfigManager _configManager;
    private MiningManager _miningManager;
    private DrillManager _drillManager;
    private DebtManager _debtManager;
    private TaskManager _taskManager;
    private TurnManager _turnManager;
    private EnergyUpgradeManager _energyManager;

    // 游戏状态
    private bool _isGameInitialized = false;
    private bool _isGamePaused = false;

    // 游戏事件
    public UnityEvent OnGameInitialized = new UnityEvent();
    public UnityEvent OnGameOver = new UnityEvent();
    public UnityEvent OnVictory = new UnityEvent();
    public UnityEvent OnGameStateChanged = new UnityEvent(); // 游戏状态变化事件（用于UI更新）
    public UnityEvent OnTurnProcessingStarted = new UnityEvent(); // 回合处理开始（动画开始）
    public UnityEvent OnTurnProcessingCompleted = new UnityEvent(); // 回合处理完成（动画结束）

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
        // 获取各个管理器实例
        _configManager = ConfigManager.Instance;
        _miningManager = MiningManager.Instance;
        _drillManager = DrillManager.Instance;
        _debtManager = DebtManager.Instance;
        _taskManager = TaskManager.Instance;
        _turnManager = TurnManager.Instance;
        _energyManager = EnergyUpgradeManager.Instance;

    }

    /// <summary>
    /// 开始新游戏
    /// </summary>
    public void StartNewGame()
    {
        if (_isGameInitialized)
        {
            Debug.LogWarning("游戏已经初始化，请先结束当前游戏");
            return;
        }

        Debug.Log("开始新游戏...");

        // 确保所有管理器引用已初始化（防止在 Start() 之前调用）
        if (_configManager == null)
        {
            _configManager = ConfigManager.Instance;
            _miningManager = MiningManager.Instance;
            _drillManager = DrillManager.Instance;
            _debtManager = DebtManager.Instance;
            _taskManager = TaskManager.Instance;
            _turnManager = TurnManager.Instance;
            _energyManager = EnergyUpgradeManager.Instance;
        }

        // 1. 加载所有配置
        _configManager.LoadAllConfigs();

        // 2. 初始化船只（产生债务）
        _debtManager.InitializeDefaultShip();

        // 2.5. 重新初始化能源系统（确保使用正确的船只阈值）
        _energyManager.ReinitializeEnergyData();

        // 3. 初始化钻头
        _drillManager.InitializeDefaultDrill();

        // 4. 生成挖矿地图（5层，使用随机种子）
        int seed = Random.Range(0, int.MaxValue);
        _miningManager.InitializeMiningMap(5, seed);

        // 5. 加载任务配置并开始第一个任务
        _taskManager.LoadTaskConfigs();
        _taskManager.StartFirstTask();

        // 6. 初始化回合系统
        TaskConfig firstTask = _taskManager.GetCurrentTask();
        if (firstTask != null)
        {
            _turnManager.Initialize(firstTask.maxTurns);
        }

        // 7. 订阅任务事件
        _taskManager.OnTaskCompleted.AddListener(OnTaskCompleted);
        _taskManager.OnTaskFailed.AddListener(OnTaskFailed);

        // 8. 订阅能源升级事件
        _energyManager.OnUpgradeAvailable.AddListener(OnEnergyUpgradeAvailable);

        _isGameInitialized = true;
        OnGameInitialized?.Invoke();

        Debug.Log("游戏初始化完成");
    }

    /// <summary>
    /// 结束回合（由UI调用）
    /// </summary>
    public void OnEndTurnButtonClicked()
    {
        if (!_isGameInitialized || _isGamePaused)
            return;

        // 通知UI禁用按钮（动画期间）
        OnTurnProcessingStarted?.Invoke();
        
        // 启动协程处理回合结束逻辑
        StartCoroutine(ProcessEndTurnCoroutine());
    }

    /// <summary>
    /// 处理回合结束的协程
    /// </summary>
    private IEnumerator ProcessEndTurnCoroutine()
    {
        yield return _turnManager.EndTurnCoroutine();
        
        // 通知UI重新启用按钮（动画完成）
        OnTurnProcessingCompleted?.Invoke();
    }

    /// <summary>
    /// 任务完成回调
    /// </summary>
    private void OnTaskCompleted(string taskId)
    {
        Debug.Log($"任务完成: {taskId}");

        // 检查是否完成所有基础任务
        if (_taskManager.AreAllBasicTasksCompleted())
        {
            OnVictory?.Invoke();
            Debug.Log("所有任务完成，游戏胜利！");
        }
    }

    /// <summary>
    /// 任务失败回调
    /// </summary>
    private void OnTaskFailed(string taskId)
    {
        Debug.Log($"任务失败: {taskId}");
        OnGameOver?.Invoke();
    }

    /// <summary>
    /// 能源升级可用回调
    /// </summary>
    private void OnEnergyUpgradeAvailable(System.Collections.Generic.List<EnergyUpgradeOption> options)
    {
        // 这个事件会被UI系统监听，显示升级选择界面
        Debug.Log($"能源升级可用，共 {options.Count} 个选项");
    }

    /// <summary>
    /// 应用升级选择（由UI调用）
    /// </summary>
    public void ApplyUpgradeSelection(EnergyUpgradeOption option)
    {
        _energyManager.SelectUpgrade(option);
    }

    /// <summary>
    /// 暂停游戏
    /// </summary>
    public void PauseGame()
    {
        _isGamePaused = true;
        Time.timeScale = 0f;
    }

    /// <summary>
    /// 恢复游戏
    /// </summary>
    public void ResumeGame()
    {
        _isGamePaused = false;
        Time.timeScale = 1f;
    }

    /// <summary>
    /// 通知游戏状态变化（用于触发UI更新）
    /// </summary>
    public void NotifyGameStateChanged()
    {
        // 触发UI更新事件
        OnGameStateChanged?.Invoke();
    }

    /// <summary>
    /// 获取当前游戏状态（供UI查询）
    /// </summary>
    public GameStateInfo GetGameStateInfo()
    {
        return new GameStateInfo
        {
            currentTurn = _turnManager.GetCurrentTurn(),
            remainingTurns = _turnManager.GetRemainingTurns(),
            currentMoney = _debtManager.GetCurrentMoney(),
            currentEnergy = _energyManager.GetCurrentEnergy(),
            debtData = _debtManager.GetDebtData(),
            taskData = _taskManager.GetTaskData(),
            drillData = _drillManager.GetCurrentDrill(),
            miningData = _miningManager.GetMiningData()
        };
    }

    /// <summary>
    /// 检查游戏是否已初始化
    /// </summary>
    public bool IsGameInitialized()
    {
        return _isGameInitialized;
    }
}

/// <summary>
/// 游戏状态信息（供UI查询）
/// </summary>
[System.Serializable]
public class GameStateInfo
{
    public int currentTurn;
    public int remainingTurns;
    public int currentMoney;
    public int currentEnergy;
    public DebtData debtData;
    public TaskData taskData;
    public DrillData drillData;
    public MiningData miningData;
}
