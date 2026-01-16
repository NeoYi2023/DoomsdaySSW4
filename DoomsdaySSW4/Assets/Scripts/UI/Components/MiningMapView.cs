using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 挖矿地图视图：显示9x9的挖矿地图
/// </summary>
public class MiningMapView : MonoBehaviour
{
    [Header("地图设置")]
    [SerializeField] private GridLayoutGroup gridLayout;
    [SerializeField] private GameObject tilePrefab; // 瓦片预制体（可选，如果为空则动态创建）
    [Header("自适应设置")]
    [SerializeField] private bool autoResize = true; // 是否自动调整大小
    [SerializeField] private Vector2 spacing = new Vector2(5, 5); // 格子间距
    [SerializeField] private bool useParentSize = true; // 是否使用父容器（LeftPanel）的大小

    private MiningManager _miningManager;
    private DrillManager _drillManager;
    private List<GameObject> _tileObjects = new List<GameObject>();
    private Dictionary<Vector2Int, GameObject> _tileMap = new Dictionary<Vector2Int, GameObject>(); // 坐标到GameObject的映射
    private Dictionary<Vector2Int, Color> _baseColors = new Dictionary<Vector2Int, Color>(); // 存储每个格子的基础颜色
    private int _currentLayerDepth = 1;
    private TMP_FontAsset _chineseFont;
    private RectTransform _containerRectTransform;
    private RectTransform _parentRectTransform;
    
    [Header("高亮设置")]
    [SerializeField] private bool enableHighlight = true; // 是否启用高亮
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.5f, 1f); // 高亮颜色（淡黄色）
    [SerializeField] private float dimmedAlpha = 0.3f; // 变暗的透明度

    // 矿石颜色映射
    private readonly Dictionary<MineralType, Color> _oreColors = new Dictionary<MineralType, Color>
    {
        { MineralType.Iron, new Color(0.5f, 0.5f, 0.5f) },      // 灰色
        { MineralType.Gold, new Color(1f, 0.84f, 0f) },          // 金色
        { MineralType.Diamond, new Color(0.2f, 0.6f, 1f) },     // 蓝色
        { MineralType.Crystal, new Color(0.8f, 0.2f, 1f) },     // 紫色
        { MineralType.EnergyCore, new Color(0.2f, 1f, 0.2f) }   // 绿色
    };

    private void Awake()
    {
        _miningManager = MiningManager.Instance;
        _drillManager = DrillManager.Instance;
        _containerRectTransform = GetComponent<RectTransform>();
        
        // 获取父容器（LeftPanel）的RectTransform
        if (useParentSize && transform.parent != null)
        {
            _parentRectTransform = transform.parent.GetComponent<RectTransform>();
        }

        // 如果没有GridLayoutGroup，创建一个
        if (gridLayout == null)
        {
            gridLayout = GetComponent<GridLayoutGroup>();
            if (gridLayout == null)
            {
                gridLayout = gameObject.AddComponent<GridLayoutGroup>();
            }
        }

        // 配置GridLayout基本设置
        if (gridLayout != null)
        {
            gridLayout.spacing = spacing;
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = MiningManager.LAYER_WIDTH; // 9列
            
            // 如果启用自适应，在Start中计算大小（此时RectTransform已正确初始化）
            if (!autoResize)
            {
                gridLayout.cellSize = new Vector2(60, 60);
            }
        }
    }

    private void Start()
    {
        // 加载中文字体
        LoadChineseFont();
        
        // 如果启用自适应，计算格子大小
        if (autoResize)
        {
            CalculateCellSize();
        }
        
        // 初始化地图显示
        UpdateMap(1);
    }

    private void OnRectTransformDimensionsChange()
    {
        // 当RectTransform大小改变时，重新计算格子大小
        if (autoResize && gridLayout != null)
        {
            CalculateCellSize();
        }
    }

    /// <summary>
    /// 计算格子大小，使其自适应容器
    /// </summary>
    private void CalculateCellSize()
    {
        if (gridLayout == null)
        {
            return;
        }

        RectTransform targetRect = null;
        
        // 决定使用哪个RectTransform的大小
        if (useParentSize && _parentRectTransform != null)
        {
            targetRect = _parentRectTransform;
        }
        else if (_containerRectTransform != null)
        {
            targetRect = _containerRectTransform;
        }
        else
        {
            Debug.LogWarning("MiningMapView: 无法找到用于计算大小的RectTransform");
            return;
        }

        // 获取容器的实际大小（考虑RectTransform的rect）
        Rect containerRect = targetRect.rect;
        float containerWidth = containerRect.width;
        float containerHeight = containerRect.height;

        // 如果使用父容器大小，需要考虑当前容器的padding
        RectOffset padding = gridLayout.padding;
        float paddingHorizontal = padding.left + padding.right;
        float paddingVertical = padding.top + padding.bottom;

        // 计算可用空间（减去padding）
        float availableWidth = containerWidth - paddingHorizontal;
        float availableHeight = containerHeight - paddingVertical;

        // 计算需要的间距
        int columns = MiningManager.LAYER_WIDTH; // 9列
        int rows = MiningManager.LAYER_HEIGHT; // 9行
        
        float spacingWidth = spacing.x * (columns - 1);
        float spacingHeight = spacing.y * (rows - 1);

        // 计算每个格子的大小
        // 公式：可用空间 = 格子数量 * 格子大小 + 间距总宽度
        // 因此：格子大小 = (可用空间 - 间距总宽度) / 格子数量
        float cellWidth = (availableWidth - spacingWidth) / columns;
        float cellHeight = (availableHeight - spacingHeight) / rows;

        // 确保大小为正数且合理
        if (cellWidth > 0 && cellHeight > 0)
        {
            gridLayout.cellSize = new Vector2(cellWidth, cellHeight);
        }
        else
        {
            // 如果计算失败，使用默认值
            Debug.LogWarning($"MiningMapView: 无法计算自适应格子大小（容器大小: {containerWidth}x{containerHeight}），使用默认值60x60");
            gridLayout.cellSize = new Vector2(60, 60);
        }
    }

    /// <summary>
    /// 加载中文字体
    /// </summary>
    private void LoadChineseFont()
    {
        // 尝试从Resources加载中文字体（可能的名称）
        string[] possibleFontNames = {
            "Fonts & Materials/微软雅黑 SDF",
            "Fonts & Materials/Microsoft YaHei SDF",
            "Fonts & Materials/ChineseFont SDF",
            "Fonts & Materials/YaHei SDF"
        };

        foreach (string fontName in possibleFontNames)
        {
            _chineseFont = Resources.Load<TMP_FontAsset>(fontName);
            if (_chineseFont != null)
            {
                break;
            }
        }
    }

    /// <summary>
    /// 更新地图显示
    /// </summary>
    public void UpdateMap(int layerDepth)
    {
        _currentLayerDepth = layerDepth;

        if (_miningManager == null)
        {
            Debug.LogWarning("MiningManager未找到");
            return;
        }

        MiningTileData[,] grid = _miningManager.GetLayerTileGrid(layerDepth);
        if (grid == null)
        {
            Debug.LogWarning($"无法获取层 {layerDepth} 的地图数据");
            return;
        }

        // 如果启用自适应，在更新地图前重新计算大小（确保大小是最新的）
        if (autoResize)
        {
            CalculateCellSize();
        }

        // 清除旧的瓦片（会同时清除映射）
        ClearTiles();

        // 创建新的瓦片
        for (int y = MiningManager.LAYER_HEIGHT - 1; y >= 0; y--) // 从下往上显示
        {
            for (int x = 0; x < MiningManager.LAYER_WIDTH; x++)
            {
                CreateTile(x, y, grid[x, y]);
            }
        }

        // 更新高亮状态
        if (enableHighlight)
        {
            UpdateHighlight();
        }
    }

    /// <summary>
    /// 创建瓦片
    /// </summary>
    private void CreateTile(int x, int y, MiningTileData tileData)
    {
        GameObject tileObj;

        if (tilePrefab != null)
        {
            tileObj = Instantiate(tilePrefab, gridLayout.transform);
        }
        else
        {
            // 动态创建瓦片
            tileObj = new GameObject($"Tile_{x}_{y}");
            tileObj.transform.SetParent(gridLayout.transform, false);

            // 添加Image组件
            Image image = tileObj.AddComponent<Image>();
            image.color = GetTileColor(tileData);

            // 添加Text显示硬度
            GameObject textObj = new GameObject("HardnessText");
            textObj.transform.SetParent(tileObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = tileData.tileType == TileType.Ore && !tileData.isMined ? tileData.hardness.ToString() : "";
            text.fontSize = 14;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            
            // 如果已加载中文字体，应用它
            if (_chineseFont != null)
            {
                text.font = _chineseFont;
            }
        }

        _tileObjects.Add(tileObj);
        
        // 添加到映射表
        Vector2Int pos = new Vector2Int(x, y);
        _tileMap[pos] = tileObj;

        // 更新瓦片显示（这会存储基础颜色）
        UpdateTileVisual(tileObj, tileData);
    }

    /// <summary>
    /// 更新瓦片视觉效果
    /// </summary>
    private void UpdateTileVisual(GameObject tileObj, MiningTileData tileData)
    {
        Image image = tileObj.GetComponent<Image>();
        if (image != null)
        {
            Color baseColor = GetTileColor(tileData);
            image.color = baseColor;
            
            // 存储基础颜色（用于高亮计算）
            _baseColors[new Vector2Int(tileData.x, tileData.y)] = baseColor;
        }

        // 更新文本
        TextMeshProUGUI text = tileObj.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            if (tileData.tileType == TileType.Ore && !tileData.isMined)
            {
                text.text = tileData.hardness.ToString();
            }
            else
            {
                text.text = "";
            }
        }
    }

    /// <summary>
    /// 获取瓦片颜色
    /// </summary>
    private Color GetTileColor(MiningTileData tileData)
    {
        if (tileData.isMined)
        {
            return new Color(0.2f, 0.2f, 0.2f); // 已挖掘：深灰色
        }

        switch (tileData.tileType)
        {
            case TileType.Empty:
                return new Color(0.1f, 0.1f, 0.1f); // 空：黑色
            case TileType.Rock:
                return new Color(0.4f, 0.4f, 0.4f); // 岩石：灰色
            case TileType.Ore:
                if (_oreColors.TryGetValue(tileData.mineralType, out Color color))
                {
                    return color;
                }
                return Color.white; // 默认白色
            default:
                return Color.gray;
        }
    }

    /// <summary>
    /// 清除所有瓦片
    /// </summary>
    private void ClearTiles()
    {
        foreach (var tile in _tileObjects)
        {
            if (tile != null)
            {
                Destroy(tile);
            }
        }
        _tileObjects.Clear();
        _tileMap.Clear();
        _baseColors.Clear();
    }

    /// <summary>
    /// 更新高亮状态：高亮可挖格子，变暗不可挖格子
    /// </summary>
    private void UpdateHighlight()
    {
        if (_drillManager == null || _miningManager == null)
        {
            return;
        }

        DrillData drill = _drillManager.GetCurrentDrill();
        if (drill == null)
        {
            return;
        }

        // 获取当前层的钻头中心位置
        MiningLayerData layer = _miningManager.GetLayer(_currentLayerDepth);
        if (layer == null)
        {
            return;
        }

        Vector2Int drillCenter = layer.drillCenter;
        Vector2Int range = drill.GetEffectiveRange();

        // 计算攻击范围
        int halfRangeX = range.x / 2;
        int halfRangeY = range.y / 2;

        int minX = Mathf.Max(0, drillCenter.x - halfRangeX);
        int maxX = Mathf.Min(MiningManager.LAYER_WIDTH - 1, drillCenter.x + halfRangeX);
        int minY = Mathf.Max(0, drillCenter.y - halfRangeY);
        int maxY = Mathf.Min(MiningManager.LAYER_HEIGHT - 1, drillCenter.y + halfRangeY);

        // 遍历所有格子，更新高亮状态
        for (int x = 0; x < MiningManager.LAYER_WIDTH; x++)
        {
            for (int y = 0; y < MiningManager.LAYER_HEIGHT; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (!_tileMap.TryGetValue(pos, out GameObject tileObj))
                {
                    continue;
                }

                // 判断是否在攻击范围内
                bool inRange = (x >= minX && x <= maxX && y >= minY && y <= maxY);
                
                // 获取Image组件
                Image image = tileObj.GetComponent<Image>();
                if (image == null)
                {
                    continue;
                }

                // 获取基础颜色（从存储的字典中获取）
                if (!_baseColors.TryGetValue(pos, out Color baseColor))
                {
                    // 如果字典中没有，从tileData获取
                    MiningTileData[,] grid = _miningManager.GetLayerTileGrid(_currentLayerDepth);
                    if (grid != null && x < grid.GetLength(0) && y < grid.GetLength(1))
                    {
                        MiningTileData tileData = grid[x, y];
                        baseColor = GetTileColor(tileData);
                        _baseColors[pos] = baseColor;
                    }
                    else
                    {
                        continue;
                    }
                }

                if (inRange)
                {
                    // 在范围内：高亮（混合高亮颜色）
                    image.color = Color.Lerp(baseColor, highlightColor, 0.3f);
                }
                else
                {
                    // 不在范围内：变暗（降低透明度）
                    Color dimmedColor = baseColor;
                    dimmedColor.a = dimmedAlpha;
                    image.color = dimmedColor;
                }
            }
        }
    }

    /// <summary>
    /// 高亮显示钻头范围（可选功能）
    /// </summary>
    public void HighlightDrillRange(Vector2Int drillCenter, Vector2Int range)
    {
        // 更新高亮状态
        if (enableHighlight)
        {
            UpdateHighlight();
        }
    }

    /// <summary>
    /// 手动刷新高亮状态（可在外部调用）
    /// </summary>
    public void RefreshHighlight()
    {
        if (enableHighlight)
        {
            UpdateHighlight();
        }
    }
}
