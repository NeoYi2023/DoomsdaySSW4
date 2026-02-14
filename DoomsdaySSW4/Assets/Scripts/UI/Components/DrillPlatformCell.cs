using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 钻机平台单元格标记组件：
/// - 仅用于标记该格子在钻机平台逻辑网格中的坐标（逻辑尺寸由 DrillPlatformData.PLATFORM_SIZE×PLATFORM_SIZE 决定，默认 9x9）
/// - 可选缓存 Image / Button / EventTrigger 引用，减少运行时 GetComponent 调用
/// - 不参与存档与钻头逻辑，仅为 UI 层服务
/// </summary>
public class DrillPlatformCell : MonoBehaviour
{
    [Header("平台坐标（0~10）")]
    // Inspector 允许配置 0~10，用于预留未来更大平台（10x10、11x11 等）扩展；
    // 实际逻辑有效范围仍由 DrillPlatformData.PLATFORM_SIZE 决定（当前为 9，对应 0~8）。
    [Range(0, 10)]
    public int x;

    [Range(0, 10)]
    public int y;

    /// <summary>
    /// 该格子在平台中的坐标（左下角为 (0,0)，右上角逻辑上由 DrillPlatformData.PLATFORM_SIZE - 1 决定；当前 PLATFORM_SIZE = 9，对应 (8,8)，未来若扩展为更大平台，则为 (PLATFORM_SIZE - 1, PLATFORM_SIZE - 1)）
    /// </summary>
    public Vector2Int GridPosition => new Vector2Int(x, y);

    [Header("可选组件缓存")]
    [HideInInspector] public Image image;
    [HideInInspector] public Button button;
    [HideInInspector] public EventTrigger eventTrigger;

    private void Awake()
    {
        // 尝试自动缓存常用组件，避免后续重复 GetComponent
        if (image == null)
        {
            image = GetComponent<Image>();
        }

        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (eventTrigger == null)
        {
            eventTrigger = GetComponent<EventTrigger>();
        }

    }
}

