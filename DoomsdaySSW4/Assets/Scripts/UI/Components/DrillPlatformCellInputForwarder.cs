using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 挂在平台格子上，将 PointerDown/Drag/PointerUp 转发给 DrillPlatformView，
/// 使点击格子时也能触发 View 的拖拽逻辑（否则事件只会命中格子，View 收不到）。
/// </summary>
public class DrillPlatformCellInputForwarder : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [SerializeField] private DrillPlatformView platformView;

    public void SetPlatformView(DrillPlatformView view)
    {
        platformView = view;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (platformView != null)
            platformView.HandleCellPointerDown(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (platformView != null)
            platformView.HandleCellDrag(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (platformView != null)
            platformView.HandleCellPointerUp(eventData);
    }
}
