using System;
using System.Collections.Generic;

/// <summary>
/// 当前任务运行时数据（对应 SPEC 3.16 TaskData）
/// </summary>
[Serializable]
public class TaskData
{
    // 当前任务信息
    public string currentTaskId;          // 当前任务ID
    public TaskType taskType;            // 任务类型（基础 / 进阶）
    public int currentTaskIndex;         // 当前任务索引（在任务列表中的位置）

    // 任务目标（债务偿还）
    public int targetDebtAmount;         // 目标债务金额
    public int currentDebtPaid;          // 当前已还金额
    public bool isTaskCompleted;         // 是否完成
    public bool isTaskFailed;            // 是否失败

    // 回合限制
    public int maxTurns;                 // 最大回合数
    public int currentTurn;              // 当前回合数

    // 任务列表
    public List<string> completedTasks = new List<string>(); // 已完成任务ID
    public List<string> basicTasks = new List<string>();      // 基础任务ID列表
    public List<string> advancedTasks = new List<string>();   // 进阶任务ID列表
}

public enum TaskType
{
    Basic,
    Advanced
}

/// <summary>
/// 任务静态配置（对应 SPEC 中 TaskConfig）
/// </summary>
[Serializable]
public class TaskConfig
{
    public string taskId;               // 任务ID
    public string taskName;             // 名称
    public TaskType taskType;           // 类型
    public int maxTurns;                // 最大回合数
    public int targetDebtAmount;        // 目标债务金额
    public string nextTaskId;           // 下一个任务ID
    public string description;          // 描述
}

/// <summary>
/// 任务配置列表的JSON包装类型，便于反序列化
/// </summary>
[Serializable]
public class TaskConfigCollection
{
    public List<TaskConfig> tasks = new List<TaskConfig>();
}

