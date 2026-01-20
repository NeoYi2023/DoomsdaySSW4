using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 钻头造型库存视图：显示可用的造型列表
/// </summary>
public class DrillShapeInventory : MonoBehaviour
{
    [Header("UI组件")]
    [SerializeField] private RectTransform contentContainer;
    [SerializeField] private GameObject shapeItemPrefab;
    [SerializeField] private ScrollRect scrollRect;
    
    [Header("布局设置")]
    [SerializeField] private float itemHeight = 80f;
    [SerializeField] private float itemSpacing = 5f;
    
    [Header("颜色设置")]
    [SerializeField] private Color normalColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
    [SerializeField] private Color selectedColor = new Color(0.4f, 0.7f, 0.4f, 0.9f);
    [SerializeField] private Color hoverColor = new Color(0.4f, 0.4f, 0.4f, 0.9f);
    
    [Header("引用")]
    [SerializeField] private DrillEditorScreen editorScreen;
    
    private DrillPlatformManager _platformManager;
    private ConfigManager _configManager;
    
    private List<GameObject> _itemObjects = new List<GameObject>();
    private string _selectedShapeId;

    private void Awake()
    {
        _platformManager = DrillPlatformManager.Instance;
        _configManager = ConfigManager.Instance;
    }

    private void Start()
    {
        Refresh();
    }

    /// <summary>
    /// 刷新库存显示
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

        // 清除现有项
        foreach (var item in _itemObjects)
        {
            if (item != null) Destroy(item);
        }
        _itemObjects.Clear();

        if (contentContainer == null)
        {
            Debug.LogError("DrillShapeInventory: contentContainer未设置");
            return;
        }

        // 获取可用造型
        List<string> availableShapeIds = _platformManager.GetAvailableShapeIds();
        
        // 计算内容高度
        float totalHeight = availableShapeIds.Count * (itemHeight + itemSpacing) - itemSpacing;
        contentContainer.sizeDelta = new Vector2(contentContainer.sizeDelta.x, Mathf.Max(totalHeight, 0));

        // 创建造型项
        float yOffset = -itemHeight / 2f;
        foreach (string shapeId in availableShapeIds)
        {
            DrillShapeConfig config = _configManager.GetDrillShapeConfig(shapeId);
            if (config == null) continue;

            GameObject itemObj = CreateShapeItem(config, yOffset);
            _itemObjects.Add(itemObj);
            
            yOffset -= (itemHeight + itemSpacing);
        }
    }

    /// <summary>
    /// 创建造型项
    /// </summary>
    private GameObject CreateShapeItem(DrillShapeConfig config, float yOffset)
    {
        GameObject itemObj;
        
        if (shapeItemPrefab != null)
        {
            itemObj = Instantiate(shapeItemPrefab, contentContainer);
        }
        else
        {
            itemObj = CreateDefaultShapeItem(config);
        }

        RectTransform rect = itemObj.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0, yOffset);
            rect.sizeDelta = new Vector2(-10, itemHeight);
        }

        // 设置数据
        ShapeInventoryItem itemComponent = itemObj.GetComponent<ShapeInventoryItem>();
        if (itemComponent != null)
        {
            itemComponent.Setup(config, this);
        }

        // 设置点击事件
        Button btn = itemObj.GetComponent<Button>();
        if (btn != null)
        {
            string capturedId = config.shapeId;
            btn.onClick.AddListener(() => OnShapeItemClicked(capturedId));
        }

        // 应用动态字体
        FontHelper.ApplyFontToGameObject(itemObj);

        return itemObj;
    }

    /// <summary>
    /// 创建默认造型项（无预制体时）
    /// </summary>
    private GameObject CreateDefaultShapeItem(DrillShapeConfig config)
    {
        GameObject itemObj = new GameObject($"ShapeItem_{config.shapeId}");
        itemObj.transform.SetParent(contentContainer);

        // 背景
        Image bgImage = itemObj.AddComponent<Image>();
        bgImage.color = normalColor;

        // 按钮
        Button button = itemObj.AddComponent<Button>();
        button.targetGraphic = bgImage;
        
        ColorBlock colors = button.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = hoverColor;
        colors.pressedColor = selectedColor;
        button.colors = colors;

        // 创建名称文本
        GameObject nameObj = new GameObject("NameText");
        nameObj.transform.SetParent(itemObj.transform);
        
        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.text = config.shapeName;
        nameText.fontSize = 16;
        nameText.color = Color.white;
        nameText.alignment = TextAlignmentOptions.Left;
        
        RectTransform nameRect = nameObj.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0.5f);
        nameRect.anchorMax = new Vector2(0.6f, 1);
        nameRect.offsetMin = new Vector2(10, 5);
        nameRect.offsetMax = new Vector2(-5, -5);

        // 创建攻击力文本
        GameObject atkObj = new GameObject("AtkText");
        atkObj.transform.SetParent(itemObj.transform);
        
        TextMeshProUGUI atkText = atkObj.AddComponent<TextMeshProUGUI>();
        atkText.text = $"攻击: {config.baseAttackStrength}";
        atkText.fontSize = 14;
        atkText.color = new Color(0.8f, 0.8f, 0.5f);
        atkText.alignment = TextAlignmentOptions.Left;
        
        RectTransform atkRect = atkObj.GetComponent<RectTransform>();
        atkRect.anchorMin = new Vector2(0, 0);
        atkRect.anchorMax = new Vector2(0.5f, 0.5f);
        atkRect.offsetMin = new Vector2(10, 5);
        atkRect.offsetMax = new Vector2(-5, -5);

        // 创建格子数文本
        GameObject cellObj = new GameObject("CellText");
        cellObj.transform.SetParent(itemObj.transform);
        
        TextMeshProUGUI cellText = cellObj.AddComponent<TextMeshProUGUI>();
        cellText.text = $"格子: {config.CellCount}";
        cellText.fontSize = 14;
        cellText.color = new Color(0.5f, 0.8f, 0.8f);
        cellText.alignment = TextAlignmentOptions.Right;
        
        RectTransform cellRect = cellObj.GetComponent<RectTransform>();
        cellRect.anchorMin = new Vector2(0.5f, 0);
        cellRect.anchorMax = new Vector2(1, 0.5f);
        cellRect.offsetMin = new Vector2(5, 5);
        cellRect.offsetMax = new Vector2(-10, -5);

        // 创建造型预览（简单的格子表示）
        GameObject previewObj = new GameObject("Preview");
        previewObj.transform.SetParent(itemObj.transform);
        
        RectTransform previewRect = previewObj.AddComponent<RectTransform>();
        previewRect.anchorMin = new Vector2(0.65f, 0.1f);
        previewRect.anchorMax = new Vector2(0.95f, 0.9f);
        previewRect.offsetMin = Vector2.zero;
        previewRect.offsetMax = Vector2.zero;

        CreateShapePreview(previewObj, config);

        return itemObj;
    }

    /// <summary>
    /// 创建造型预览
    /// </summary>
    private void CreateShapePreview(GameObject parent, DrillShapeConfig config)
    {
        if (config.cells == null || config.cells.Count == 0) return;

        // 计算边界
        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;
        
        foreach (var cell in config.cells)
        {
            minX = Mathf.Min(minX, cell.x);
            minY = Mathf.Min(minY, cell.y);
            maxX = Mathf.Max(maxX, cell.x);
            maxY = Mathf.Max(maxY, cell.y);
        }

        int width = maxX - minX + 1;
        int height = maxY - minY + 1;
        int maxDim = Mathf.Max(width, height);

        RectTransform parentRect = parent.GetComponent<RectTransform>();
        float previewSize = Mathf.Min(parentRect.rect.width, parentRect.rect.height);
        float cellSize = previewSize / maxDim * 0.8f;

        foreach (var cell in config.cells)
        {
            GameObject cellObj = new GameObject($"PreviewCell_{cell.x}_{cell.y}");
            cellObj.transform.SetParent(parent.transform);

            Image cellImage = cellObj.AddComponent<Image>();
            cellImage.color = new Color(0.4f, 0.7f, 0.9f, 0.9f);

            RectTransform cellRect = cellObj.GetComponent<RectTransform>();
            cellRect.sizeDelta = new Vector2(cellSize * 0.9f, cellSize * 0.9f);
            cellRect.anchorMin = new Vector2(0.5f, 0.5f);
            cellRect.anchorMax = new Vector2(0.5f, 0.5f);
            
            float offsetX = (cell.x - minX - (width - 1) / 2f) * cellSize;
            float offsetY = (cell.y - minY - (height - 1) / 2f) * cellSize;
            cellRect.anchoredPosition = new Vector2(offsetX, offsetY);
        }
    }

    /// <summary>
    /// 造型项点击事件
    /// </summary>
    private void OnShapeItemClicked(string shapeId)
    {
        _selectedShapeId = shapeId;
        
        if (editorScreen != null)
        {
            editorScreen.SelectInventoryShape(shapeId);
        }
        
        UpdateSelection();
    }

    /// <summary>
    /// 更新选中状态显示
    /// </summary>
    private void UpdateSelection()
    {
        List<string> availableShapeIds = _platformManager.GetAvailableShapeIds();
        
        for (int i = 0; i < _itemObjects.Count && i < availableShapeIds.Count; i++)
        {
            GameObject itemObj = _itemObjects[i];
            string shapeId = availableShapeIds[i];
            
            Image bgImage = itemObj.GetComponent<Image>();
            if (bgImage != null)
            {
                bgImage.color = (shapeId == _selectedShapeId) ? selectedColor : normalColor;
            }
        }
    }

    /// <summary>
    /// 清除选中状态
    /// </summary>
    public void ClearSelection()
    {
        _selectedShapeId = null;
        UpdateSelection();
    }

    /// <summary>
    /// 获取当前选中的造型ID
    /// </summary>
    public string GetSelectedShapeId()
    {
        return _selectedShapeId;
    }
}

/// <summary>
/// 造型库存项组件（可选，用于预制体）
/// </summary>
public class ShapeInventoryItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI attackText;
    [SerializeField] private TextMeshProUGUI cellCountText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    
    private DrillShapeConfig _config;
    private DrillShapeInventory _inventory;

    public void Setup(DrillShapeConfig config, DrillShapeInventory inventory)
    {
        _config = config;
        _inventory = inventory;

        if (nameText != null)
        {
            nameText.text = config.shapeName;
        }

        if (attackText != null)
        {
            attackText.text = $"攻击: {config.baseAttackStrength}";
        }

        if (cellCountText != null)
        {
            cellCountText.text = $"格子: {config.CellCount}";
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
}
