using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>
/// 可拖拽的造型组件：支持从库存拖拽到平台放置
/// </summary>
public class DraggableShape : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("设置")]
    [SerializeField] private float cellSize = 20f;
    [SerializeField] private Color normalColor = new Color(0.4f, 0.7f, 0.9f, 0.9f);
    [SerializeField] private Color validColor = new Color(0.3f, 0.9f, 0.3f, 0.9f);
    [SerializeField] private Color invalidColor = new Color(0.9f, 0.3f, 0.3f, 0.9f);
    
    [Header("引用")]
    [SerializeField] private DrillPlatformView platformView;
    [SerializeField] private DrillEditorScreen editorScreen;
    
    private string _shapeId;
    private string _instanceId; // 如果是已放置的造型
    private DrillShapeConfig _config;
    private int _currentRotation = 0;
    
    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private Canvas _canvas;
    
    private List<GameObject> _cellVisuals = new List<GameObject>();
    private Vector3 _originalPosition;
    private Transform _originalParent;
    private bool _isDragging;
    private bool _isValidPlacement;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        
        if (_canvasGroup == null)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        _canvas = GetComponentInParent<Canvas>();
    }

    /// <summary>
    /// 设置为库存中的造型（准备放置）
    /// </summary>
    public void SetupAsInventoryShape(string shapeId)
    {
        _shapeId = shapeId;
        _instanceId = null;
        _config = ConfigManager.Instance.GetDrillShapeConfig(shapeId);
        _currentRotation = 0;
        
        CreateVisuals();
    }

    /// <summary>
    /// 设置为已放置的造型（准备移动）
    /// </summary>
    public void SetupAsPlacedShape(PlacedDrillShape placedShape)
    {
        _shapeId = placedShape.shapeId;
        _instanceId = placedShape.instanceId;
        _config = ConfigManager.Instance.GetDrillShapeConfig(_shapeId);
        _currentRotation = placedShape.rotation;
        
        CreateVisuals();
    }

    /// <summary>
    /// 创建造型视觉表示
    /// </summary>
    private void CreateVisuals()
    {
        // 清除现有视觉
        foreach (var cell in _cellVisuals)
        {
            if (cell != null) Destroy(cell);
        }
        _cellVisuals.Clear();

        if (_config == null || _config.cells == null) return;

        List<Vector2Int> rotatedCells = _config.GetRotatedCells(_currentRotation);
        
        foreach (var cell in rotatedCells)
        {
            GameObject cellObj = new GameObject($"Cell_{cell.x}_{cell.y}");
            cellObj.transform.SetParent(transform);
            
            Image image = cellObj.AddComponent<Image>();
            image.color = normalColor;
            
            RectTransform rect = cellObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(cellSize * 0.9f, cellSize * 0.9f);
            rect.anchoredPosition = new Vector2(cell.x * cellSize, cell.y * cellSize);
            
            _cellVisuals.Add(cellObj);
        }
    }

    /// <summary>
    /// 旋转造型
    /// </summary>
    public void Rotate(bool clockwise = true)
    {
        _currentRotation = clockwise ? (_currentRotation + 90) % 360 : (_currentRotation + 270) % 360;
        CreateVisuals();
    }

    /// <summary>
    /// 检查是否可以拖拽（非自动挖矿状态）
    /// </summary>
    private bool CanDrag()
    {
        // 检查编辑器是否允许编辑
        if (editorScreen != null && !editorScreen.CanEdit())
        {
            return false;
        }
        
        // 也直接检查 TurnManager
        TurnManager turnManager = TurnManager.Instance;
        if (turnManager != null)
        {
            return !turnManager.IsAutoMiningEnabled() && !turnManager.IsProcessingTurn();
        }
        
        return true;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 检查是否允许拖拽
        if (!CanDrag())
        {
            Debug.Log("自动挖矿中，无法拖拽钻头");
            return;
        }
        
        _isDragging = true;
        _originalPosition = _rectTransform.position;
        _originalParent = transform.parent;
        
        // 将拖拽对象移到最上层
        if (_canvas != null)
        {
            transform.SetParent(_canvas.transform);
        }
        
        _canvasGroup.blocksRaycasts = false;
        _canvasGroup.alpha = 0.8f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;
        
        _rectTransform.position = eventData.position;
        
        // 检查放置有效性
        UpdatePlacementPreview(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        _isDragging = false;
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.alpha = 1f;

        if (_isValidPlacement && platformView != null)
        {
            Vector2Int? gridPos = platformView.WorldToGridPosition(eventData.position);
            if (gridPos.HasValue)
            {
                TryPlace(gridPos.Value);
                return;
            }
        }

        // 放置失败，返回原位置
        ReturnToOriginalPosition();
    }

    /// <summary>
    /// 更新放置预览
    /// </summary>
    private void UpdatePlacementPreview(PointerEventData eventData)
    {
        _isValidPlacement = false;
        
        if (platformView == null) return;
        
        Vector2Int? gridPos = platformView.WorldToGridPosition(eventData.position);
        if (!gridPos.HasValue)
        {
            UpdateCellColors(normalColor);
            return;
        }
        
        // 验证放置
        PlaceResult result = DrillPlatformManager.Instance.ValidatePlacement(
            _shapeId, 
            gridPos.Value, 
            _currentRotation,
            _instanceId // 排除自身（如果是移动操作）
        );
        
        _isValidPlacement = result.success;
        UpdateCellColors(_isValidPlacement ? validColor : invalidColor);
    }

    /// <summary>
    /// 更新格子颜色
    /// </summary>
    private void UpdateCellColors(Color color)
    {
        foreach (var cell in _cellVisuals)
        {
            Image image = cell.GetComponent<Image>();
            if (image != null)
            {
                image.color = color;
            }
        }
    }

    /// <summary>
    /// 尝试放置造型
    /// </summary>
    private void TryPlace(Vector2Int position)
    {
        DrillPlatformManager manager = DrillPlatformManager.Instance;
        
        if (!string.IsNullOrEmpty(_instanceId))
        {
            // 移动已有造型
            PlaceResult result = manager.TryMoveShape(_instanceId, position);
            if (result.success)
            {
                // 销毁拖拽对象
                Destroy(gameObject);
                
                // 刷新编辑器视图
                if (editorScreen != null)
                {
                    editorScreen.RefreshViews();
                }
                return;
            }
        }
        else
        {
            // 放置新造型
            PlaceResult result = manager.TryPlaceShape(_shapeId, position, _currentRotation);
            if (result.success)
            {
                // 销毁拖拽对象
                Destroy(gameObject);
                
                // 刷新编辑器视图
                if (editorScreen != null)
                {
                    editorScreen.RefreshViews();
                }
                return;
            }
        }
        
        // 放置失败
        ReturnToOriginalPosition();
    }

    /// <summary>
    /// 返回原位置
    /// </summary>
    private void ReturnToOriginalPosition()
    {
        transform.SetParent(_originalParent);
        _rectTransform.position = _originalPosition;
        UpdateCellColors(normalColor);
    }

    /// <summary>
    /// 获取当前造型ID
    /// </summary>
    public string GetShapeId()
    {
        return _shapeId;
    }

    /// <summary>
    /// 获取当前实例ID（如果是已放置的造型）
    /// </summary>
    public string GetInstanceId()
    {
        return _instanceId;
    }

    /// <summary>
    /// 获取当前旋转角度
    /// </summary>
    public int GetRotation()
    {
        return _currentRotation;
    }
}
