using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 任务管理器：负责任务管理、完成检查
/// </summary>
public class TaskManager : MonoBehaviour
{
    private static TaskManager _instance;
    public static TaskManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("TaskManager");
                _instance = go.AddComponent<TaskManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private TaskData _taskData;
    private ConfigManager _configManager;
    private DebtManager _debtManager;

    // 任务完成/失败事件
    public UnityEvent<string> OnTaskCompleted = new UnityEvent<string>(); // taskId
    public UnityEvent<string> OnTaskFailed = new UnityEvent<string>(); // taskId

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
    }

    /// <summary>
    /// 确保管理器已初始化（延迟初始化）
    /// </summary>
    private void EnsureManagers()
    {
        if (_configManager == null)
        {
            _configManager = ConfigManager.Instance;
        }
        if (_debtManager == null)
        {
            _debtManager = DebtManager.Instance;
        }
    }

    /// <summary>
    /// 加载任务配置并初始化
    /// </summary>
    public void LoadTaskConfigs()
    {
        // 确保 ConfigManager 已初始化（防止在 Start() 之前调用）
        EnsureManagers();

        List<TaskConfig> basicTasks = _configManager.GetAllBasicTasks();
        
        _taskData = new TaskData
        {
            currentTaskId = "",
            taskType = TaskType.Basic,
            currentTaskIndex = -1,
            targetDebtAmount = 0,
            currentDebtPaid = 0,
            isTaskCompleted = false,
            isTaskFailed = false,
            maxTurns = 0,
            currentTurn = 0,
            completedTasks = new List<string>(),
            basicTasks = basicTasks.Select(t => t.taskId).ToList(),
            advancedTasks = new List<string>()
        };

        Debug.Log($"任务配置加载完成，共 {basicTasks.Count} 个基础任务");
    }

    /// <summary>
    /// 开始第一个任务
    /// </summary>
    public void StartFirstTask()
    {
        if (_taskData == null || _taskData.basicTasks == null || _taskData.basicTasks.Count == 0)
        {
            Debug.LogError("没有可用的任务");
            return;
        }

        string firstTaskId = _taskData.basicTasks[0];
        StartTask(firstTaskId);
    }

    /// <summary>
    /// 开始指定任务
    /// </summary>
    public void StartTask(string taskId)
    {
        // 确保 ConfigManager 已初始化（防止在 Start() 之前调用）
        EnsureManagers();

        TaskConfig config = _configManager.GetTaskConfig(taskId);
        if (config == null)
        {
            Debug.LogError($"无法加载任务配置: {taskId}");
            return;
        }

        // 重置任务状态
        _taskData.currentTaskId = taskId;
        _taskData.taskType = config.taskType;
        _taskData.targetDebtAmount = config.targetDebtAmount;
        _taskData.currentDebtPaid = 0; // 任务开始时，已还金额从0开始计算
        _taskData.isTaskCompleted = false;
        _taskData.isTaskFailed = false;
        _taskData.maxTurns = config.maxTurns;
        _taskData.currentTurn = 0;

        // 更新任务索引
        if (_taskData.basicTasks.Contains(taskId))
        {
            _taskData.currentTaskIndex = _taskData.basicTasks.IndexOf(taskId);
        }

        Debug.Log($"任务开始: {config.taskName}, 目标: 偿还 {config.targetDebtAmount} 债务, 回合限制: {config.maxTurns}");
    }

    /// <summary>
    /// 更新债务偿还进度（由债务系统调用）
    /// </summary>
    public void UpdateDebtProgress(int totalPaidDebt)
    {
        // 确保管理器已初始化
        EnsureManagers();

        if (_taskData == null || string.IsNullOrEmpty(_taskData.currentTaskId))
            return;

        // 计算本任务已还的债务金额
        // 简化实现：使用总已还债务减去之前任务已还的债务
        int previousTaskPaid = _taskData.completedTasks.Sum(id =>
        {
            TaskConfig prevTask = _configManager.GetTaskConfig(id);
            return prevTask != null ? prevTask.targetDebtAmount : 0;
        });

        _taskData.currentDebtPaid = Mathf.Max(0, totalPaidDebt - previousTaskPaid);
    }

    /// <summary>
    /// 检查任务完成条件
    /// </summary>
    public bool CheckTaskCompletion()
    {
        EnsureManagers();

        if (_taskData == null || string.IsNullOrEmpty(_taskData.currentTaskId))
            return false;

        if (_taskData.currentDebtPaid >= _taskData.targetDebtAmount)
        {
            _taskData.isTaskCompleted = true;
            Debug.Log($"任务完成: {_taskData.currentTaskId}");
            OnTaskCompleted?.Invoke(_taskData.currentTaskId);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 检查任务失败条件
    /// </summary>
    public bool CheckTaskFailure(int currentTurn)
    {
        if (_taskData == null || string.IsNullOrEmpty(_taskData.currentTaskId))
            return false;

        if (currentTurn >= _taskData.maxTurns && !_taskData.isTaskCompleted)
        {
            _taskData.isTaskFailed = true;
            Debug.Log($"任务失败: {_taskData.currentTaskId}, 回合数已用完");
            OnTaskFailed?.Invoke(_taskData.currentTaskId);
            return true;
        }

        return false;
    }

    /// <summary>
    /// 完成当前任务并领取下一个任务
    /// </summary>
    public void CompleteCurrentTask()
    {
        EnsureManagers();

        if (_taskData == null || string.IsNullOrEmpty(_taskData.currentTaskId))
            return;

        // 标记任务为已完成
        _taskData.isTaskCompleted = true;
        _taskData.completedTasks.Add(_taskData.currentTaskId);

        // 获取下一个任务ID
        TaskConfig currentConfig = _configManager.GetTaskConfig(_taskData.currentTaskId);
        if (currentConfig != null && !string.IsNullOrEmpty(currentConfig.nextTaskId))
        {
            // 自动领取下一个任务
            StartTask(currentConfig.nextTaskId);
        }
        else
        {
            // 没有下一个任务，所有任务完成
            Debug.Log("所有任务已完成！");
        }
    }

    /// <summary>
    /// 获取当前任务
    /// </summary>
    public TaskConfig GetCurrentTask()
    {
        EnsureManagers();

        if (_taskData == null || string.IsNullOrEmpty(_taskData.currentTaskId))
            return null;

        return _configManager.GetTaskConfig(_taskData.currentTaskId);
    }

    /// <summary>
    /// 获取当前任务数据
    /// </summary>
    public TaskData GetTaskData()
    {
        return _taskData;
    }

    /// <summary>
    /// 获取任务进度（0-1）
    /// </summary>
    public float GetTaskProgress()
    {
        EnsureManagers();

        if (_taskData == null || _taskData.targetDebtAmount <= 0)
            return 0f;

        return Mathf.Clamp01((float)_taskData.currentDebtPaid / _taskData.targetDebtAmount);
    }

    /// <summary>
    /// 检查是否完成所有基础任务
    /// </summary>
    public bool AreAllBasicTasksCompleted()
    {
        if (_taskData == null || _taskData.basicTasks == null)
            return false;

        return _taskData.completedTasks.Count >= _taskData.basicTasks.Count;
    }
}
