using UnityEngine;
using TMPro;

/// <summary>
/// 资源显示组件：显示游戏资源信息
/// </summary>
public class ResourceDisplay : MonoBehaviour
{
    [Header("显示组件")]
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI energyText;
    [SerializeField] private TextMeshProUGUI debtText;

    private GameManager _gameManager;

    private void Awake()
    {
        _gameManager = GameManager.Instance;
    }

    private void Update()
    {
        UpdateDisplay();
    }

    /// <summary>
    /// 更新显示
    /// </summary>
    private void UpdateDisplay()
    {
        if (_gameManager == null || !_gameManager.IsGameInitialized())
            return;

        GameStateInfo state = _gameManager.GetGameStateInfo();
        if (state == null) return;

        if (moneyText != null)
        {
            moneyText.text = $"金钱: {state.currentMoney}";
        }

        if (energyText != null)
        {
            energyText.text = $"能源: {state.currentEnergy}";
        }

        if (debtText != null && state.debtData != null)
        {
            debtText.text = $"债务: {state.debtData.RemainingDebt} / {state.debtData.totalDebt}";
        }
    }
}
