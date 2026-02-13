using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// 钻机平台视图：显示9x9的钻机平台网格和已放置的造型，
/// 支持鼠标左键拖动放置/移动造型，以及右键旋转选中造型。
/// </summary>
public class DrillPlatformView : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IPointerClickHandler
{
    [Header("网格设置")]
    [SerializeField] private RectTransform gridContainer;
    [SerializeField] private GameObject cellPrefab;
    [Tooltip("与 GridRoot 的 HexLayoutGroup.CellSize 保持一致时六边形布局更一致。")]
    [SerializeField] private float cellSize = 40f;
    [Tooltip("与 GridRoot 的 HexLayoutGroup.Spacing 保持一致。")]
    [SerializeField] private float cellSpacing = 2f;
    
    [Header("颜色设置")]
    [SerializeField] private Color emptyColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
    [SerializeField] private Color occupiedColor = new Color(0.3f, 0.6f, 0.9f, 0.8f);
    [SerializeField] private Color highlightColor = new Color(0.9f, 0.9f, 0.3f, 0.8f);
    [SerializeField] private Color hoverColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
    [SerializeField] private Color invalidColor = new Color(0.9f, 0.3f, 0.3f, 0.8f);
    
    [Header("引用")]
    [SerializeField] private DrillEditorScreen editorScreen;
    
    private DrillPlatformManager _platformManager;
    private ConfigManager _configManager;
    
    private Dictionary<Vector2Int, GameObject> _cellObjects = new Dictionary<Vector2Int, GameObject>();
    private PlacedDrillShape _highlightedShape;
    private Vector2Int? _hoveredCell;

    // 拖拽相关状态
    private bool _isDragging;
    private bool _draggingExistingShape;
    private Vector2Int? _lastDragGridPos;

    private void Awake()
    {
        _platformManager = DrillPlatformManager.Instance;
        _configManager = ConfigManager.Instance;

        // 如果未在 Inspector 绑定 editorScreen，则尝试从父节点自动获取
        if (editorScreen == null)
        {
            editorScreen = GetComponentInParent<DrillEditorScreen>(true);
        }
    }

    private void Start()
    {
        InitGridFromChildren();
    }

    /// <summary>
    /// 从 gridContainer 子节点初始化9x9静态网格
    /// </summary>
    private void InitGridFromChildren()
    {
        // 如果未在 Inspector 绑定 gridContainer，但自身挂有 RectTransform，则做一次防御性自动绑定
        if (gridContainer == null)
        {
            RectTransform selfRect = GetComponent<RectTransform>();
            if (selfRect != null)
            {
                gridContainer = selfRect;
            }
        }

        if (gridContainer == null)
        {
            Debug.LogError("DrillPlatformView: gridContainer未设置");
            return;
        }

        _cellObjects.Clear();

        // ====================================================================
        // 自动从 sibling 顺序计算 row-major 坐标（不依赖 Inspector 序列化的 x/y），
        // 只处理前 PLATFORM_SIZE×PLATFORM_SIZE 个格子，多余的外圈装饰格子自动跳过。
        // ====================================================================
        int size = DrillPlatformData.PLATFORM_SIZE;
        int maxLogicCells = size * size; // 9×9 = 81

        // 定位实际的格子容器：如果 gridContainer 只有 1 个子节点（GridRoot），则进入它
        Transform cellContainer = gridContainer;
        if (gridContainer.childCount == 1)
        {
            Transform child = gridContainer.GetChild(0);
            if (child.childCount > 0)
            {
                cellContainer = child;
            }
        }

        int totalChildren = cellContainer.childCount;
        if (totalChildren == 0)
        {
            Debug.LogError("DrillPlatformView: 在 gridContainer 下未找到任何子节点，请在 PlatformGrid 下布置 9x9 格子并挂载 DrillPlatformCell 组件。");
            return;
        }

        int logicIdx = 0; // 逻辑格子计数（用于计算 row-major 坐标）
        for (int i = 0; i < totalChildren; i++)
        {
            Transform childTransform = cellContainer.GetChild(i);
            if (childTransform == null) continue;

            // 跳过FogMaskContainer及其子节点
            if (childTransform.name == "FogMaskContainer" || childTransform.name.Contains("FogTile_"))
            {
                continue;
            }

            DrillPlatformCell cell = childTransform.GetComponent<DrillPlatformCell>();
            if (cell == null) continue;

            // 超过 81 个逻辑格子的为外圈装饰格子，跳过
            if (logicIdx >= maxLogicCells)
            {
                logicIdx++;
                continue;
            }

            // 从 sibling 顺序自动计算 row-major 坐标：row 0 在下，row 8 在上
            int col = logicIdx % size;
            int row = logicIdx / size;
            Vector2Int pos = new Vector2Int(col, row);

            // 将正确坐标写回组件，修正 Inspector 中的错误值
            cell.x = col;
            cell.y = row;

            GameObject cellObj = cell.gameObject;

            // 确保存在 Image 组件
            Image image = cell.image != null ? cell.image : cellObj.GetComponent<Image>();
            if (image == null)
            {
                image = cellObj.AddComponent<Image>();
                image.color = emptyColor;
            }
            cell.image = image;

            // 确保存在 Button 组件并绑定点击事件
            Button button = cell.button != null ? cell.button : cellObj.GetComponent<Button>();
            if (button == null)
            {
                button = cellObj.AddComponent<Button>();
            }
            button.targetGraphic = image;
            button.onClick.RemoveAllListeners();
            Vector2Int capturedPos = pos;
            button.onClick.AddListener(() => OnCellClicked(capturedPos));
            cell.button = button;

            // 确保存在 EventTrigger 并绑定悬停事件
            EventTrigger trigger = cell.eventTrigger != null ? cell.eventTrigger : cellObj.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = cellObj.AddComponent<EventTrigger>();
            }
            if (trigger.triggers == null)
            {
                trigger.triggers = new List<EventTrigger.Entry>();
            }
            else
            {
                trigger.triggers.Clear();
            }

            EventTrigger.Entry enterEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerEnter
            };
            Vector2Int capturedPosEnter = pos;
            enterEntry.callback.AddListener((data) => OnCellHoverEnter(capturedPosEnter));
            trigger.triggers.Add(enterEntry);

            EventTrigger.Entry exitEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerExit
            };
            exitEntry.callback.AddListener((data) => OnCellHoverExit());
            trigger.triggers.Add(exitEntry);

            _cellObjects[pos] = cellObj;
            logicIdx++;
        }

        if (_cellObjects.Count < maxLogicCells)
        {
            Debug.LogWarning($"DrillPlatformView: 有效逻辑格子数量不足 ({_cellObjects.Count}/{maxLogicCells})，请检查 PlatformGrid 下是否有足够的 DrillPlatformCell 子节点。");
        }

        Refresh();
    }

    /// <summary>
    /// 刷新显示
    /// </summary>
    public void Refresh()
    {
        if (_platformManager == null)
        {
            _platformManager = DrillPlatformManager.Instance;
        }
        
        if (_configManager == null)
        {
            _configManager = ConfigManager.Instance;
        }

        HashSet<Vector2Int> occupiedCells = _platformManager.GetAllOccupiedCells();
        HashSet<Vector2Int> highlightedCells = GetHighlightedCells();

        foreach (var kvp in _cellObjects)
        {
            Vector2Int pos = kvp.Key;
            GameObject cellObj = kvp.Value;
            
            if (cellObj == null) continue;
            
            Image image = cellObj.GetComponent<Image>();
            if (image == null) continue;

            if (highlightedCells.Contains(pos))
            {
                image.color = highlightColor;
            }
            else if (occupiedCells.Contains(pos))
            {
                image.color = occupiedColor;
            }
            else
            {
                image.color = emptyColor;
            }
        }
    }

    /// <summary>
    /// 高亮显示指定造型
    /// </summary>
    public void HighlightShape(PlacedDrillShape shape)
    {
        _highlightedShape = shape;
        Refresh();
    }

    /// <summary>
    /// 获取高亮的格子坐标
    /// </summary>
    private HashSet<Vector2Int> GetHighlightedCells()
    {
        HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
        
        if (_highlightedShape != null)
        {
            DrillShapeConfig config = _configManager.GetDrillShapeConfig(_highlightedShape.shapeId);
            if (config != null)
            {
                List<Vector2Int> shapeCells = _highlightedShape.GetOccupiedCells(config);
                foreach (var cell in shapeCells)
                {
                    cells.Add(cell);
                }
            }
        }
        
        return cells;
    }

    /// <summary>
    /// 格子点击事件
    /// </summary>
    private void OnCellClicked(Vector2Int position)
    {
        if (editorScreen != null)
        {
            editorScreen.TryPlaceAtPosition(position);
        }
    }

    /// <summary>
    /// 格子悬停进入事件
    /// </summary>
    private void OnCellHoverEnter(Vector2Int position)
    {
        _hoveredCell = position;
        UpdateHoverPreview();
    }

    /// <summary>
    /// 格子悬停退出事件
    /// </summary>
    private void OnCellHoverExit()
    {
        ClearHoverPreview();
        _hoveredCell = null;
    }

    /// <summary>
    /// 更新悬停预览
    /// </summary>
    private void UpdateHoverPreview()
    {
        if (!_hoveredCell.HasValue) return;
        
        string pendingShapeId = editorScreen?.GetPendingShapeId();
        if (string.IsNullOrEmpty(pendingShapeId)) return;
        int rotation = editorScreen != null ? editorScreen.GetPendingShapeRotation() : 0;

        ShowPlacementPreview(pendingShapeId, rotation, _hoveredCell.Value);
    }

    /// <summary>
    /// 清除悬停预览
    /// </summary>
    private void ClearHoverPreview()
    {
        Refresh();
    }

    #region 指针事件（拖拽与右键旋转）

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        Vector2Int? gridPos = WorldToGridPosition(eventData.position);
        if (!gridPos.HasValue) return;

        _isDragging = true;
        _draggingExistingShape = false;
        _lastDragGridPos = gridPos;

        if (editorScreen == null)
        {
            return;
        }

        string pendingShapeId = editorScreen.GetPendingShapeId();
        if (!string.IsNullOrEmpty(pendingShapeId))
        {
            // 从库存拖出新造型：仅标记拖拽开始，实际放置在 PointerUp 中完成
            _draggingExistingShape = false;
            _hoveredCell = gridPos;
            UpdateHoverPreview();
        }
        else
        {
            // 尝试拖动已有造型
            if (_platformManager == null)
            {
                _platformManager = DrillPlatformManager.Instance;
            }

            PlacedDrillShape shape = _platformManager.GetShapeAtPosition(gridPos.Value);
            if (shape != null)
            {
                _draggingExistingShape = true;
                editorScreen.SelectPlacedShape(shape);
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;

        Vector2Int? gridPos = WorldToGridPosition(eventData.position);
        if (!gridPos.HasValue || (_lastDragGridPos.HasValue && _lastDragGridPos.Value == gridPos.Value))
        {
            return;
        }

        _lastDragGridPos = gridPos;

        if (editorScreen == null)
        {
            return;
        }

        if (_draggingExistingShape)
        {
            // 拖动已放置造型：实时尝试移动
            editorScreen.TryMoveSelectedShape(gridPos.Value);
        }
        else
        {
            // 拖动待放置的造型：更新悬停预览
            _hoveredCell = gridPos;
            ClearHoverPreview();
            UpdateHoverPreview();
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!_isDragging) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;

        Vector2Int? gridPos = WorldToGridPosition(eventData.position);
        if (gridPos.HasValue && editorScreen != null && !_draggingExistingShape)
        {
            // 拖拽新造型松手时尝试放置
            editorScreen.TryPlaceAtPosition(gridPos.Value);
        }

        _isDragging = false;
        _draggingExistingShape = false;
        _lastDragGridPos = null;
        _hoveredCell = null;
        ClearHoverPreview();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right) return;
        if (editorScreen == null)
        {
            return;
        }

        Vector2Int? gridPos = WorldToGridPosition(eventData.position);
        if (!gridPos.HasValue)
        {
            return;
        }

        if (_platformManager == null)
        {
            _platformManager = DrillPlatformManager.Instance;
        }

        PlacedDrillShape shape = _platformManager.GetShapeAtPosition(gridPos.Value);
        if (shape != null)
        {
            // 右键点击某个格子：选中并顺时针旋转该造型
            editorScreen.SelectPlacedShape(shape);
            editorScreen.RotateSelectedShape(true);
        }
    }

    #endregion

    /// <summary>
    /// 使用给定的造型与旋转角度，在指定平台坐标上显示放置预览（供悬停与库存拖拽共用）
    /// </summary>
    private void ShowPlacementPreview(string shapeId, int rotation, Vector2Int position)
    {
        if (string.IsNullOrEmpty(shapeId))
        {
            return;
        }

        // 先验证放置合法性
        PlaceResult result = _platformManager.ValidatePlacement(shapeId, position, rotation);

        DrillShapeConfig config = _configManager.GetDrillShapeConfig(shapeId);
        if (config == null) return;

        PlacedDrillShape tempShape = new PlacedDrillShape(shapeId, position, rotation);
        List<Vector2Int> cells = tempShape.GetOccupiedCells(config);

        Color previewColor = result.success ? hoverColor : invalidColor;

        foreach (var cell in cells)
        {
            if (_cellObjects.TryGetValue(cell, out GameObject cellObj))
            {
                Image image = cellObj.GetComponent<Image>();
                if (image != null)
                {
                    image.color = previewColor;
                }
            }
        }
    }

    /// <summary>
    /// 库存拖拽开始（目前主要用于语义标记，必要时可扩展）
    /// </summary>
    public void BeginInventoryDrag(string shapeId)
    {
        // 当前实现不需要额外状态，这里保留接口以便后续扩展
    }

    /// <summary>
    /// 库存拖拽过程中，根据屏幕坐标更新放置预览
    /// </summary>
    public void UpdateInventoryDrag(string shapeId, int rotation, Vector2 screenPosition)
    {
        if (string.IsNullOrEmpty(shapeId))
        {
            return;
        }

        Vector2Int? gridPos = WorldToGridPosition(screenPosition);
        if (!gridPos.HasValue)
        {
            _hoveredCell = null;
            ClearHoverPreview();
            return;
        }

        _hoveredCell = gridPos;
        ClearHoverPreview();
        ShowPlacementPreview(shapeId, rotation, gridPos.Value);
    }

    /// <summary>
    /// 库存拖拽结束，在当前位置尝试正式放置并清理预览
    /// </summary>
    public void EndInventoryDrag(string shapeId, int rotation, Vector2 screenPosition)
    {
        // 优先使用拖拽过程中记录的最后一个悬停格子位置（_hoveredCell），
        // 避免松手时鼠标轻微移出格子导致 WorldToGridPosition 返回 null。
        Vector2Int? gridPos = _hoveredCell.HasValue ? _hoveredCell : WorldToGridPosition(screenPosition);

        if (gridPos.HasValue && editorScreen != null)
        {
            editorScreen.TryPlaceAtPosition(gridPos.Value);
        }

        _hoveredCell = null;
        ClearHoverPreview();
    }

    /// <summary>
    /// 当待放置造型在编辑界面中被右键旋转时，基于当前鼠标位置刷新一次预览
    /// </summary>
    public void RefreshPendingPreviewAtMousePosition()
    {
        if (editorScreen == null)
        {
            return;
        }

        string pendingShapeId = editorScreen.GetPendingShapeId();
        if (string.IsNullOrEmpty(pendingShapeId))
        {
            return;
        }

        Vector2 mousePos = Input.mousePosition;
        Vector2Int? gridPos = WorldToGridPosition(mousePos);
        if (!gridPos.HasValue)
        {
            _hoveredCell = null;
            ClearHoverPreview();
            return;
        }

        _hoveredCell = gridPos;
        ClearHoverPreview();
        ShowPlacementPreview(pendingShapeId, editorScreen.GetPendingShapeRotation(), gridPos.Value);
    }

    /// <summary>
    /// 获取格子的世界坐标
    /// </summary>
    public Vector3 GetCellWorldPosition(Vector2Int gridPosition)
    {
        if (_cellObjects.TryGetValue(gridPosition, out GameObject cellObj))
        {
            return cellObj.transform.position;
        }
        return Vector3.zero;
    }

    /// <summary>
    /// 从世界坐标获取网格坐标。
    /// 六边形布局下相邻格子矩形会重叠，若有多个格子包含该点则返回中心距离最近的一格。
    /// </summary>
    public Vector2Int? WorldToGridPosition(Vector3 worldPosition)
    {
        List<KeyValuePair<Vector2Int, GameObject>> containing = new List<KeyValuePair<Vector2Int, GameObject>>();

        foreach (var kvp in _cellObjects)
        {
            RectTransform rect = kvp.Value.GetComponent<RectTransform>();
            if (rect == null) continue;

            Vector3[] corners = new Vector3[4];
            rect.GetWorldCorners(corners);

            if (worldPosition.x >= corners[0].x && worldPosition.x <= corners[2].x &&
                worldPosition.y >= corners[0].y && worldPosition.y <= corners[2].y)
            {
                containing.Add(kvp);
            }
        }

        if (containing.Count == 0)
            return null;
        if (containing.Count == 1)
            return containing[0].Key;

        // 多格重叠（六边形边缘）：取格子中心与 worldPosition 距离最小的一格
        float minDistSq = float.MaxValue;
        Vector2Int best = containing[0].Key;
        Vector2 pos2 = new Vector2(worldPosition.x, worldPosition.y);

        for (int i = 0; i < containing.Count; i++)
        {
            RectTransform rect = containing[i].Value.GetComponent<RectTransform>();
            if (rect == null) continue;
            Vector2 center = rect.position;
            float dSq = (pos2 - center).sqrMagnitude;
            if (dSq < minDistSq)
            {
                minDistSq = dSq;
                best = containing[i].Key;
            }
        }

        return best;
    }
}
