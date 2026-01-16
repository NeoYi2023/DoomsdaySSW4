using System;
using System.Collections.Generic;

/// <summary>
/// 债务系统数据（对应 SPEC 3.15 DebtData / DebtPayment）
/// </summary>
[Serializable]
public class DebtData
{
    // 船只信息
    public string currentShipId;           // 当前使用的船只ID

    // 债务信息
    public int totalDebt;                  // 总债务
    public int paidDebt;                   // 已还债务

    /// <summary>
    /// 当前剩余债务
    /// </summary>
    public int RemainingDebt
    {
        get { return totalDebt - paidDebt; }
    }

    // 还债进度（0-1）
    public float RepaymentProgress
    {
        get
        {
            if (totalDebt <= 0) return 1f;
            return (float)paidDebt / totalDebt;
        }
    }

    public bool isDebtCleared;             // 是否已还清所有债务

    // 债务历史
    public List<DebtPayment> paymentHistory = new List<DebtPayment>();
}

[Serializable]
public class DebtPayment
{
    public long timestamp;                 // 还债时间戳
    public int amount;                     // 还债金额
    public int remainingAfterPayment;      // 还债后剩余债务
}

