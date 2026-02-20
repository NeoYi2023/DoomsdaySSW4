using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 挖矿地图单元格标记组件：
/// - 仅用于标记该格子在挖矿地图逻辑网格中的坐标（网格尺寸由 MiningManager.LAYER_WIDTH/HEIGHT 决定，默认 9x11）
/// - 可选缓存 Image / TextMeshProUGUI 引用，减少运行时 GetComponent 调用
/// - 与 PlatformGrid 的 DrillPlatformCell 对应，用于静态格子方案
/// </summary>
public class MiningMapCell : MonoBehaviour
{
    [Header("地图坐标（0~10）")]
    [Tooltip("Inspector 允许配置 0~10，用于预留扩展；实际有效范围由 MiningManager.LAYER_WIDTH/HEIGHT 决定（当前 9×11 时为 X:0~8, Y:0~10）。")]
    [Range(0, 10)]
    public int x;

    [Range(0, 10)]
    public int y;

    /// <summary>
    /// 该格子在地图中的逻辑坐标（与 MiningManager 层网格一致）
    /// </summary>
    public Vector2Int GridPosition => new Vector2Int(x, y);

    [Header("可选组件缓存")]
    [HideInInspector] public Image image;
    [HideInInspector] public TextMeshProUGUI text;

    private void Awake()
    {
        if (image == null)
            image = GetComponent<Image>();
        if (text == null)
            text = GetComponentInChildren<TextMeshProUGUI>(true);
    }
}
