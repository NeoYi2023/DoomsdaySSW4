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
    [SerializeField] private Button saveButton;          // 新增：保存当前平台布局但不关闭界面
    
    [Header("信息显示")]
    [SerializeField] private TextMeshProUGUI selectedShapeText;
    [SerializeField] private TextMeshProUGUI statusText;
    
    private DrillPlatformManager _platformManager;
    private TurnManager _turnManager;
    private DrillPlatformData _backupData; // 用于取消操作时恢复
    private PlacedDrillShape _selectedShape;
    private string _pendingShapeId; // 从库存拖出准备放置的造型ID
    private int _pendingShapeRotation; // 待放置造型的旋转角度（0/90/180/270）
    private bool _isDraggingFromInventory; // 是否正从库存拖拽造型
    
    private void Awake()
    {
        _platformManager = DrillPlatformManager.Instance;
        _turnManager = TurnManager.Instance;

        // #region agent log
        try
        {
            var log = "{\"sessionId\":\"debug-session\",\"runId\":\"ui-debug-1\",\"hypothesisId\":\"HE2\",\"location\":\"DrillEditorScreen.Awake\",\"message\":\"DrillEditorScreen Awake\",\"data\":{\"panelNull\":" + (panel == null ? "true" : "false") + "},\"timestamp\":" + System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}";
            System.IO.File.AppendAllText(@"e:\Work\Cursor\DoomsdaySSW4\.cursor\debug.log", log + System.Environment.NewLine);
        }
        catch { }
        // #endregion
    }

    private void Start()
    {
        // 初始化时隐藏
        if (panel != null)
        {
            panel.SetActive(false);
        }

        _pendingShapeRotation = 0;
        _isDraggingFromInventory = false;
        
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

        if (saveButton != null)
        {
            saveButton.onClick.AddListener(OnSaveClicked);
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
        // #region agent log
        try
        {
            var log = "{\"sessionId\":\"debug-session\",\"runId\":\"ui-debug-1\",\"hypothesisId\":\"HE3\",\"location\":\"DrillEditorScreen.Show\",\"message\":\"Show called\",\"data\":{\"canEdit\":" + (CanEdit() ? "true" : "false") + ",\"panelNull\":" + (panel == null ? "true" : "false") + "},\"timestamp\":" + System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}";
            System.IO.File.AppendAllText(@"e:\Work\Cursor\DoomsdaySSW4\.cursor\debug.log", log + System.Environment.NewLine);
        }
        catch { }
        // #endregion

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

        // 打开界面时重置待放置旋转状态
        _pendingShapeRotation = 0;
        _isDraggingFromInventory = false;
        
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
        _pendingShapeRotation = 0;
        
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
        _pendingShapeRotation = 0;
        
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
        // #region agent log
        try
        {
            var logEnter = "{\"sessionId\":\"debug-session\",\"runId\":\"drag-debug-1\",\"hypothesisId\":\"H2\",\"location\":\"DrillEditorScreen.TryPlaceAtPosition\",\"message\":\"Enter TryPlaceAtPosition\",\"data\":{\"posX\":" + position.x + ",\"posY\":" + position.y + ",\"pendingShapeId\":\"" + (_pendingShapeId ?? "null") + "\",\"pendingRotation\":" + _pendingShapeRotation + "},\"timestamp\":" + System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}";
            System.IO.File.AppendAllText("e:\\Work\\Cursor\\DoomsdaySSW4\\.cursor\\debug.log", logEnter + System.Environment.NewLine);
        }
        catch { }
        // #endregion

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
        
        // 使用当前记录的旋转角度尝试放置
        PlaceResult result = _platformManager.TryPlaceShape(_pendingShapeId, position, _pendingShapeRotation);

        // #region agent log
        try
        {
            var logResult = "{\"sessionId\":\"debug-session\",\"runId\":\"drag-debug-1\",\"hypothesisId\":\"H3\",\"location\":\"DrillEditorScreen.TryPlaceAtPosition\",\"message\":\"PlaceShape result\",\"data\":{\"posX\":" + position.x + ",\"posY\":" + position.y + ",\"pendingShapeId\":\"" + (_pendingShapeId ?? "null") + "\",\"pendingRotation\":" + _pendingShapeRotation + ",\"success\":" + (result != null && result.success ? "true" : "false") + ",\"error\":\"" + (result != null && result.errorMessage != null ? result.errorMessage : "") + "\"},\"timestamp\":" + System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "}";
            System.IO.File.AppendAllText("e:\\Work\\Cursor\\DoomsdaySSW4\\.cursor\\debug.log", logResult + System.Environment.NewLine);
        }
        catch { }
        // #endregion
        
        if (result.success)
        {
            UpdateStatusText($"造型放置成功");
            _pendingShapeId = null;
            _pendingShapeRotation = 0;
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
        RotateSelectedShape(false);
    }

    private void OnRotateRightClicked()
    {
        RotateSelectedShape(true);
    }

    /// <summary>
    /// 旋转当前选中的造型（供按钮和右键调用）
    /// </summary>
    public void RotateSelectedShape(bool clockwise)
    {
        if (_selectedShape == null) return;

        PlaceResult result = _platformManager.TryRotateShape(_selectedShape.instanceId, clockwise);

        if (result.success)
        {
            RefreshViews();
            UpdateStatusText(clockwise ? "造型顺时针旋转90°" : "造型逆时针旋转90°");
        }
        else
        {
            UpdateStatusText($"旋转失败: {result.errorMessage}");
        }
    }

    private void OnSaveClicked()
    {
        // 将当前平台布局保存回当前钻头数据，但不关闭编辑界面
        DrillManager drillManager = DrillManager.Instance;
        if (drillManager == null)
        {
            UpdateStatusText("保存失败：未找到 DrillManager");
            return;
        }

        DrillData drill = drillManager.GetCurrentDrill();
        if (drill == null)
        {
            UpdateStatusText("保存失败：当前没有已装备的钻头");
            return;
        }

        DrillPlatformData currentData = _platformManager.GetPlatformData();
        if (currentData == null)
        {
            UpdateStatusText("保存失败：平台数据为空");
            return;
        }

        // 深拷贝一份当前平台数据到钻头数据中
        drill.platformData = currentData.Clone();

        UpdateStatusText("当前钻机平台布局已保存");
        Debug.Log("钻机平台布局已保存到当前钻头数据（保持编辑界面打开）");
    }

    private void ClearSelection()
    {
        _selectedShape = null;
        _pendingShapeId = null;
        _pendingShapeRotation = 0;
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

    /// <summary>
    /// 获取待放置造型的当前旋转角度
    /// </summary>
    public int GetPendingShapeRotation()
    {
        return _pendingShapeRotation;
    }

    /// <summary>
    /// 旋转待放置造型（用于拖拽库存造型时右键旋转）
    /// </summary>
    public void RotatePendingShape(bool clockwise)
    {
        if (string.IsNullOrEmpty(_pendingShapeId))
        {
            return;
        }

        _pendingShapeRotation = clockwise
            ? (_pendingShapeRotation + 90) % 360
            : (_pendingShapeRotation + 270) % 360;

        // 旋转后尝试刷新当前鼠标所在格子的放置预览
        if (platformView != null)
        {
            platformView.RefreshPendingPreviewAtMousePosition();
        }

        UpdateStatusText("预览造型顺时针旋转90°");
    }

    /// <summary>
    /// 由库存项开始拖拽时通知编辑界面，标记当前拖拽来源
    /// </summary>
    public void BeginInventoryDrag()
    {
        _isDraggingFromInventory = true;
    }

    /// <summary>
    /// 由库存项结束拖拽时通知编辑界面
    /// </summary>
    public void EndInventoryDrag()
    {
        _isDraggingFromInventory = false;
    }

    /// <summary>
    /// 获取当前平台视图（供库存项访问）
    /// </summary>
    public DrillPlatformView GetPlatformView()
    {
        return platformView;
    }

    /// <summary>
    /// 获取当前是否正在从库存拖拽造型
    /// </summary>
    public bool IsDraggingFromInventory()
    {
        return _isDraggingFromInventory;
    }

    private void Update()
    {
        // 拖拽库存造型过程中，支持右键旋转预览造型
        if (panel != null && panel.activeInHierarchy &&
            _isDraggingFromInventory &&
            !string.IsNullOrEmpty(_pendingShapeId) &&
            Input.GetMouseButtonDown(1))
        {
            RotatePendingShape(true);
        }
    }
}
