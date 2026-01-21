using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 钻机平台单元格标记组件：
/// - 仅用于标记该格子在 9x9 平台中的坐标
/// - 可选缓存 Image / Button / EventTrigger 引用，减少运行时 GetComponent 调用
/// - 不参与存档与钻头逻辑，仅为 UI 层服务
/// </summary>
public class DrillPlatformCell : MonoBehaviour
{
    [Header("平台坐标（0~8）")]
    [Range(0, DrillPlatformData.PLATFORM_SIZE - 1)]
    public int x;

    [Range(0, DrillPlatformData.PLATFORM_SIZE - 1)]
    public int y;

    /// <summary>
    /// 该格子在平台中的坐标（左下角为 (0,0)，右上角为 (8,8)）
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

