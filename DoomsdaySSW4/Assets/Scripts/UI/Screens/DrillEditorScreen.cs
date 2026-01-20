using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 钻机编辑界面：允许玩家在回合之间编辑钻机平台上的造型布局
/// </summary>
public class DrillEditorScreen : MonoBehaviour
{
    [Header("UI组件")]
    [SerializeField] private GameObject panel;
    [SerializeField] private DrillPlatformView platformView;
    [SerializeField] private DrillShapeInventory inventoryView;
    
    [Header("按钮")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button clearButton;
    [SerializeField] private Button rotateLeftButton;
    [SerializeField] private Button rotateRightButton;
    
    [Header("信息显示")]
    [SerializeField] private TextMeshProUGUI selectedShapeText;
    [SerializeField] private TextMeshProUGUI statusText;
    
    private DrillPlatformManager _platformManager;
    private TurnManager _turnManager;
    private DrillPlatformData _backupData; // 用于取消操作时恢复
    private PlacedDrillShape _selectedShape;
    private string _pendingShapeId; // 从库存拖出准备放置的造型ID
    
    private void Awake()
    {
        _platformManager = DrillPlatformManager.Instance;
        _turnManager = TurnManager.Instance;
    }

    private void Start()
    {
        // 初始化时隐藏
        if (panel != null)
        {
            panel.SetActive(false);
        }
        
        SetupButtonEvents();
    }

    private void SetupButtonEvents()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }
        
        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancelClicked);
        }
        
        if (clearButton != null)
        {
            clearButton.onClick.AddListener(OnClearClicked);
        }
        
        if (rotateLeftButton != null)
        {
            rotateLeftButton.onClick.AddListener(OnRotateLeftClicked);
        }
        
        if (rotateRightButton != null)
        {
            rotateRightButton.onClick.AddListener(OnRotateRightClicked);
        }
    }

    /// <summary>
    /// 检查是否可以编辑钻头（非自动挖矿状态且非处理中）
    /// </summary>
    public bool CanEdit()
    {
        if (_turnManager == null)
        {
            _turnManager = TurnManager.Instance;
        }
        
        if (_turnManager == null)
        {
            return true; // 如果找不到 TurnManager，默认允许编辑
        }
        
        return !_turnManager.IsAutoMiningEnabled() && !_turnManager.IsProcessingTurn();
    }
    
    /// <summary>
    /// 显示编辑界面
    /// </summary>
    public void Show()
    {
        // 检查是否允许编辑
        if (!CanEdit())
        {
            Debug.Log("自动挖矿中，无法编辑钻头");
            UpdateStatusText("自动挖矿中，无法编辑钻头布局");
            return;
        }
        
        if (panel != null)
        {
            panel.SetActive(true);
        }
        
        // 备份当前平台数据
        DrillPlatformData currentData = _platformManager.GetPlatformData();
        _backupData = currentData?.Clone();
        
        // 刷新显示
        RefreshViews();
        ClearSelection();
        
        UpdateStatusText("点击平台上的造型可选中，点击库存中的造型可放置");
        
        // 应用动态字体
        ApplyDynamicFont();
    }

    /// <summary>
    /// 隐藏编辑界面
    /// </summary>
    public void Hide()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
        
        ClearSelection();
        _backupData = null;
    }

    /// <summary>
    /// 刷新所有视图
    /// </summary>
    public void RefreshViews()
    {
        if (platformView != null)
        {
            platformView.Refresh();
        }
        
        if (inventoryView != null)
        {
            inventoryView.Refresh();
        }
    }

    /// <summary>
    /// 选中平台上的造型
    /// </summary>
    public void SelectPlacedShape(PlacedDrillShape shape)
    {
        _selectedShape = shape;
        _pendingShapeId = null;
        
        if (shape != null)
        {
            DrillShapeConfig config = ConfigManager.Instance.GetDrillShapeConfig(shape.shapeId);
            string shapeName = config != null ? config.shapeName : shape.shapeId;
            UpdateSelectedShapeText($"选中: {shapeName} (点击旋转按钮或拖动移动)");
        }
        else
        {
            UpdateSelectedShapeText("");
        }
        
        UpdateRotateButtonsState();
        
        if (platformView != null)
        {
            platformView.HighlightShape(shape);
        }
    }

    /// <summary>
    /// 从库存选中造型准备放置
    /// </summary>
    public void SelectInventoryShape(string shapeId)
    {
        _pendingShapeId = shapeId;
        _selectedShape = null;
        
        DrillShapeConfig config = ConfigManager.Instance.GetDrillShapeConfig(shapeId);
        string shapeName = config != null ? config.shapeName : shapeId;
        UpdateSelectedShapeText($"准备放置: {shapeName} (点击平台空位放置)");
        
        UpdateRotateButtonsState();
        
        if (platformView != null)
        {
            platformView.HighlightShape(null);
        }
    }

    /// <summary>
    /// 尝试在指定位置放置造型
    /// </summary>
    public void TryPlaceAtPosition(Vector2Int position)
    {
        if (string.IsNullOrEmpty(_pendingShapeId))
        {
            // 如果没有待放置的造型，检查是否点击了已有造型
            PlacedDrillShape existingShape = _platformManager.GetShapeAtPosition(position);
            if (existingShape != null)
            {
                SelectPlacedShape(existingShape);
            }
            return;
        }
        
        PlaceResult result = _platformManager.TryPlaceShape(_pendingShapeId, position, 0);
        
        if (result.success)
        {
            UpdateStatusText($"造型放置成功");
            _pendingShapeId = null;
            ClearSelection();
            RefreshViews();
        }
        else
        {
            UpdateStatusText($"放置失败: {result.errorMessage}");
        }
    }

    /// <summary>
    /// 尝试移动选中的造型到新位置
    /// </summary>
    public void TryMoveSelectedShape(Vector2Int newPosition)
    {
        if (_selectedShape == null) return;
        
        PlaceResult result = _platformManager.TryMoveShape(_selectedShape.instanceId, newPosition);
        
        if (result.success)
        {
            UpdateStatusText("造型移动成功");
            RefreshViews();
        }
        else
        {
            UpdateStatusText($"移动失败: {result.errorMessage}");
        }
    }

    /// <summary>
    /// 移除选中的造型
    /// </summary>
    public void RemoveSelectedShape()
    {
        if (_selectedShape == null) return;
        
        bool success = _platformManager.RemoveShape(_selectedShape.instanceId);
        
        if (success)
        {
            UpdateStatusText("造型已移除");
            ClearSelection();
            RefreshViews();
        }
    }

    private void OnConfirmClicked()
    {
        // 确认更改，关闭界面
        _backupData = null; // 清除备份，不再需要
        Hide();
        
        Debug.Log("钻机编辑确认");
    }

    private void OnCancelClicked()
    {
        // 恢复备份数据
        if (_backupData != null)
        {
            _platformManager.SetPlatformData(_backupData);
        }
        
        Hide();
        
        Debug.Log("钻机编辑取消");
    }

    private void OnClearClicked()
    {
        _platformManager.ClearPlatform();
        ClearSelection();
        RefreshViews();
        UpdateStatusText("平台已清空，所有造型移回库存");
    }

    private void OnRotateLeftClicked()
    {
        if (_selectedShape == null) return;
        
        PlaceResult result = _platformManager.TryRotateShape(_selectedShape.instanceId, false);
        
        if (result.success)
        {
            RefreshViews();
            UpdateStatusText("造型逆时针旋转90°");
        }
        else
        {
            UpdateStatusText($"旋转失败: {result.errorMessage}");
        }
    }

    private void OnRotateRightClicked()
    {
        if (_selectedShape == null) return;
        
        PlaceResult result = _platformManager.TryRotateShape(_selectedShape.instanceId, true);
        
        if (result.success)
        {
            RefreshViews();
            UpdateStatusText("造型顺时针旋转90°");
        }
        else
        {
            UpdateStatusText($"旋转失败: {result.errorMessage}");
        }
    }

    private void ClearSelection()
    {
        _selectedShape = null;
        _pendingShapeId = null;
        UpdateSelectedShapeText("");
        UpdateRotateButtonsState();
        
        if (platformView != null)
        {
            platformView.HighlightShape(null);
        }
    }

    private void UpdateRotateButtonsState()
    {
        bool canRotate = _selectedShape != null;
        
        if (rotateLeftButton != null)
        {
            rotateLeftButton.interactable = canRotate;
        }
        
        if (rotateRightButton != null)
        {
            rotateRightButton.interactable = canRotate;
        }
    }

    private void UpdateSelectedShapeText(string text)
    {
        if (selectedShapeText != null)
        {
            selectedShapeText.text = text;
        }
    }

    private void UpdateStatusText(string text)
    {
        if (statusText != null)
        {
            statusText.text = text;
        }
    }

    private void ApplyDynamicFont()
    {
        FontHelper.ApplyFontToGameObject(gameObject);
    }

    /// <summary>
    /// 获取当前选中的造型
    /// </summary>
    public PlacedDrillShape GetSelectedShape()
    {
        return _selectedShape;
    }

    /// <summary>
    /// 获取待放置的造型ID
    /// </summary>
    public string GetPendingShapeId()
    {
        return _pendingShapeId;
    }
}
