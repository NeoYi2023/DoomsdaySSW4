using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 债务管理器：负责债务管理、还债逻辑
/// </summary>
public class DebtManager : MonoBehaviour
{
    private static DebtManager _instance;
    public static DebtManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("DebtManager");
                _instance = go.AddComponent<DebtManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private DebtData _debtData;
    private ConfigManager _configManager;
    private int _currentMoney; // 当前拥有的金钱

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
    /// 初始化默认船只（产生初始债务）
    /// </summary>
    public void InitializeDefaultShip()
    {
        // 确保 ConfigManager 已初始化（防止在 Start() 之前调用）
        if (_configManager == null)
        {
            _configManager = ConfigManager.Instance;
        }

        ShipConfig shipConfig = _configManager.GetShipConfig("default_ship");
        if (shipConfig == null)
        {
            Debug.LogError("无法加载默认船只配置");
            return;
        }

        _debtData = new DebtData
        {
            currentShipId = shipConfig.shipId,
            totalDebt = shipConfig.initialDebt,
            paidDebt = 0,
            isDebtCleared = false,
            paymentHistory = new List<DebtPayment>()
        };

        _currentMoney = 0;

        Debug.Log($"默认船只初始化完成: {shipConfig.shipName}, 初始债务: {_debtData.totalDebt}");
    }

    /// <summary>
    /// 添加金钱并自动还债
    /// </summary>
    public void AddMoneyAndPayDebt(int money)
    {
        if (_debtData == null)
        {
            Debug.LogError("债务数据未初始化");
            return;
        }

        _currentMoney += money;

        // 自动还债（所有金钱都用于还债）
        if (_currentMoney > 0 && _debtData.RemainingDebt > 0)
        {
            int paymentAmount = Mathf.Min(_currentMoney, _debtData.RemainingDebt);
            _debtData.paidDebt += paymentAmount;
            _currentMoney -= paymentAmount;

            // 记录还债历史
            DebtPayment payment = new DebtPayment
            {
                timestamp = System.DateTime.Now.Ticks,
                amount = paymentAmount,
                remainingAfterPayment = _debtData.RemainingDebt
            };
            _debtData.paymentHistory.Add(payment);

            // 检查是否还清所有债务
            if (_debtData.RemainingDebt <= 0)
            {
                _debtData.isDebtCleared = true;
                Debug.Log("所有债务已还清！");
            }

            Debug.Log($"还债 {paymentAmount}，剩余债务: {_debtData.RemainingDebt}");
        }
    }

    /// <summary>
    /// 获取债务数据
    /// </summary>
    public DebtData GetDebtData()
    {
        return _debtData;
    }

    /// <summary>
    /// 检查是否还清债务
    /// </summary>
    public bool IsDebtCleared()
    {
        return _debtData != null && _debtData.isDebtCleared;
    }

    /// <summary>
    /// 获取当前金钱
    /// </summary>
    public int GetCurrentMoney()
    {
        return _currentMoney;
    }

    /// <summary>
    /// 获取已还债务金额（用于任务系统）
    /// </summary>
    public int GetPaidDebtAmount()
    {
        return _debtData != null ? _debtData.paidDebt : 0;
    }

    /// <summary>
    /// 获取当前船只ID
    /// </summary>
    public string GetCurrentShipId()
    {
        return _debtData != null ? _debtData.currentShipId : "default_ship";
    }

    /// <summary>
    /// 增加债务（用于任务失败时扣除targetDebtAmount）
    /// </summary>
    public void AddDebt(int amount)
    {
        if (_debtData == null)
        {
            Debug.LogError("债务数据未初始化");
            return;
        }

        // #region agent log
        System.IO.File.AppendAllText(@"f:\CursorGame_Git\DoomsdaySSW4\.cursor\debug.log", "{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"E\",\"location\":\"DebtManager.cs:162\",\"message\":\"AddDebt called\",\"data\":{\"amount\":" + amount + ",\"oldTotalDebt\":" + _debtData.totalDebt + ",\"oldPaidDebt\":" + _debtData.paidDebt + "},\"timestamp\":" + System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n");
        // #endregion

        _debtData.totalDebt += amount;

        // #region agent log
        System.IO.File.AppendAllText(@"f:\CursorGame_Git\DoomsdaySSW4\.cursor\debug.log", "{\"sessionId\":\"debug-session\",\"runId\":\"run1\",\"hypothesisId\":\"E\",\"location\":\"DebtManager.cs:168\",\"message\":\"After AddDebt\",\"data\":{\"newTotalDebt\":" + _debtData.totalDebt + ",\"remainingDebt\":" + _debtData.RemainingDebt + "},\"timestamp\":" + System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}\n");
        // #endregion

        Debug.Log($"债务增加 {amount}，总债务: {_debtData.totalDebt}，剩余债务: {_debtData.RemainingDebt}");
    }
}
