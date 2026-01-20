using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 钻机平台视图：显示9x9的钻机平台网格和已放置的造型
/// </summary>
public class DrillPlatformView : MonoBehaviour
{
    [Header("网格设置")]
    [SerializeField] private RectTransform gridContainer;
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private float cellSize = 40f;
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

    private void Awake()
    {
        _platformManager = DrillPlatformManager.Instance;
        _configManager = ConfigManager.Instance;
    }

    private void Start()
    {
        CreateGrid();
    }

    /// <summary>
    /// 创建9x9网格
    /// </summary>
    private void CreateGrid()
    {
        if (gridContainer == null)
        {
            Debug.LogError("DrillPlatformView: gridContainer未设置");
            return;
        }

        // 清除现有格子
        foreach (var cell in _cellObjects.Values)
        {
            if (cell != null) Destroy(cell);
        }
        _cellObjects.Clear();

        float totalSize = DrillPlatformData.PLATFORM_SIZE * (cellSize + cellSpacing) - cellSpacing;
        float startX = -totalSize / 2f + cellSize / 2f;
        float startY = -totalSize / 2f + cellSize / 2f;

        for (int y = 0; y < DrillPlatformData.PLATFORM_SIZE; y++)
        {
            for (int x = 0; x < DrillPlatformData.PLATFORM_SIZE; x++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                GameObject cellObj = CreateCell(pos, startX, startY);
                _cellObjects[pos] = cellObj;
            }
        }

        Refresh();
    }

    /// <summary>
    /// 创建单个格子
    /// </summary>
    private GameObject CreateCell(Vector2Int position, float startX, float startY)
    {
        GameObject cellObj;
        
        if (cellPrefab != null)
        {
            cellObj = Instantiate(cellPrefab, gridContainer);
        }
        else
        {
            cellObj = new GameObject($"Cell_{position.x}_{position.y}");
            cellObj.transform.SetParent(gridContainer);
            
            Image image = cellObj.AddComponent<Image>();
            image.color = emptyColor;
            
            // 添加按钮组件用于点击
            Button button = cellObj.AddComponent<Button>();
            button.targetGraphic = image;
        }

        RectTransform rect = cellObj.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(cellSize, cellSize);
            rect.anchoredPosition = new Vector2(
                startX + position.x * (cellSize + cellSpacing),
                startY + position.y * (cellSize + cellSpacing)
            );
        }

        // 设置点击事件
        Button btn = cellObj.GetComponent<Button>();
        if (btn != null)
        {
            Vector2Int capturedPos = position;
            btn.onClick.AddListener(() => OnCellClicked(capturedPos));
        }

        // 添加悬停检测
        EventTrigger trigger = cellObj.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = cellObj.AddComponent<EventTrigger>();
        }
        
        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        Vector2Int capturedPosEnter = position;
        enterEntry.callback.AddListener((data) => OnCellHoverEnter(capturedPosEnter));
        trigger.triggers.Add(enterEntry);
        
        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => OnCellHoverExit());
        trigger.triggers.Add(exitEntry);

        return cellObj;
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
        
        // 验证放置
        PlaceResult result = _platformManager.ValidatePlacement(pendingShapeId, _hoveredCell.Value, 0);
        
        DrillShapeConfig config = _configManager.GetDrillShapeConfig(pendingShapeId);
        if (config == null) return;
        
        PlacedDrillShape tempShape = new PlacedDrillShape(pendingShapeId, _hoveredCell.Value, 0);
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
    /// 清除悬停预览
    /// </summary>
    private void ClearHoverPreview()
    {
        Refresh();
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
    /// 从世界坐标获取网格坐标
    /// </summary>
    public Vector2Int? WorldToGridPosition(Vector3 worldPosition)
    {
        foreach (var kvp in _cellObjects)
        {
            RectTransform rect = kvp.Value.GetComponent<RectTransform>();
            if (rect != null)
            {
                Vector3[] corners = new Vector3[4];
                rect.GetWorldCorners(corners);
                
                if (worldPosition.x >= corners[0].x && worldPosition.x <= corners[2].x &&
                    worldPosition.y >= corners[0].y && worldPosition.y <= corners[2].y)
                {
                    return kvp.Key;
                }
            }
        }
        return null;
    }
}
