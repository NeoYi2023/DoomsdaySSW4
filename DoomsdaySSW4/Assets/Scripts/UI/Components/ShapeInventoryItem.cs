using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class ShapeInventoryItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI cellCountText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    private DrillShapeConfig _config;
    private DrillShapeInventory _inventory;
    private DrillEditorScreen _editorScreen;
    private DrillPlatformView _platformView;
    private bool _isDragging;

    public void Setup(DrillShapeConfig config, DrillShapeInventory inventory)
    {
        _config = config;
        _inventory = inventory;

        // ???????????????????
        if (_inventory != null)
        {
            _editorScreen = _inventory.GetEditorScreen();
            if (_editorScreen != null)
            {
                _platformView = _editorScreen.GetPlatformView();
            }
        }

        if (nameText != null)
        {
            nameText.text = config.shapeName;
        }

        if (attackText != null)
        {
            attackText.text = $"ùùùù: {config.baseAttackStrength}";
        }

        if (cellCountText != null)
        {
            cellCountText.text = $"ùùùù: {config.CellCount}";
        }

        if (descriptionText != null)
        {
            descriptionText.text = config.description;
        }
    }

    public DrillShapeConfig GetConfig()
    {
        return _config;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (_config == null)
        {
            return;
        }

        _isDragging = true;

        if (_editorScreen != null)
        {
            // ???????????????????????????
            _editorScreen.SelectInventoryShape(_config.shapeId);
            _editorScreen.BeginInventoryDrag();
        }

        if (_platformView != null)
        {
            _platformView.BeginInventoryDrag(_config.shapeId);
            _platformView.UpdateInventoryDrag(_config.shapeId,
                _editorScreen != null ? _editorScreen.GetPendingShapeRotation() : 0,
                eventData.position);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging)
        {
            return;
        }

        if (eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (_platformView != null && _config != null)
        {
            int rotation = _editorScreen != null ? _editorScreen.GetPendingShapeRotation() : 0;
            _platformView.UpdateInventoryDrag(_config.shapeId, rotation, eventData.position);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isDragging)
        {
            return;
        }

        _isDragging = false;

        if (_editorScreen != null)
        {
            _editorScreen.EndInventoryDrag();
        }

        if (_platformView != null && _config != null)
        {
            int rotation = _editorScreen != null ? _editorScreen.GetPendingShapeRotation() : 0;
            _platformView.EndInventoryDrag(_config.shapeId, rotation, eventData.position);
        }
    }
}
