using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// 挖矿地图视图：显示挖矿地图（逻辑为 9x9 网格，可在 UI 层通过 HexLayoutGroup 扩展为 94 格正六边形布局）
/// </summary>
public class MiningMapView : MonoBehaviour
{
    [Header("地图设置")]
    [SerializeField] private GridLayoutGroup gridLayout;
    [SerializeField] private HexLayoutGroup hexLayout; // 六边形布局组件（与 PlatformGrid 保持一致）
    [SerializeField] private RectTransform mapGridRoot; // 静态格子容器（MapGridRoot，挂 HexLayoutGroup + 若干 MiningMapCell，支持 81 逻辑格 + 额外装饰格）
    [SerializeField] private bool useStaticCells = false; // 为 true 时从 mapGridRoot 子节点初始化格子，不动态创建/销毁
    [SerializeField] private GameObject tilePrefab; // 瓦片预制体（动态创建模式时使用，若为空则代码创建）
    [Header("自适应设置")]
    [SerializeField] private bool autoResize = true; // 是否自动调整大小
    [SerializeField] private Vector2 spacing = new Vector2(5, 5); // 格子间距
    [SerializeField] private bool useParentSize = true; // 是否使用父容器（LeftPanel）的大小

    private MiningManager _miningManager;
    private DrillManager _drillManager;
    private ConfigManager _configManager;
    private List<GameObject> _tileObjects = new List<GameObject>();
    private Dictionary<Vector2Int, GameObject> _tileMap = new Dictionary<Vector2Int, GameObject>(); // 坐标到GameObject的映射
    private Dictionary<Vector2Int, Color> _baseColors = new Dictionary<Vector2Int, Color>(); // 存储每个格子的基础颜色
    private int _currentLayerDepth = 1;
    private TMP_FontAsset _chineseFont;
    private RectTransform _containerRectTransform;
    private RectTransform _parentRectTransform;
    private bool _loggedEmptyTileThisUpdate = false;
    
    [Header("晃动动效设置")]
    [SerializeField] private float shakeDuration = 0.5f; // 晃动持续时间（秒）
    [SerializeField] private float shakeAmplitude = 4.8f; // 晃动幅度（像素）
    [SerializeField] private float shakeFrequency = 12f; // 晃动频率（次/秒）
    private Dictionary<Vector2Int, Vector2> _originalPositions = new Dictionary<Vector2Int, Vector2>(); // 存储格子的原始位置
    private List<Coroutine> _activeShakeCoroutines = new List<Coroutine>(); // 当前活动的晃动协程
    private bool _isAnimating = false; // 是否正在播放动画（防止UpdateMap中断动画）
    
    /// <summary>
    /// 检查是否正在播放动画
    /// </summary>
    public bool IsAnimating => _isAnimating;
    
    [Header("高亮设置")]
    [SerializeField] private bool enableHighlight = true; // 是否启用高亮
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.5f, 1f); // 高亮颜色（淡黄色）
    [SerializeField] private float dimmedAlpha = 0.3f; // 变暗的透明度
    
    [Header("伤害高亮设置")]
    [SerializeField] private Color damageHighlightColor = new Color(1f, 0f, 0f, 0.4f); // 红色半透明高亮
    
    [Header("迷雾遮罩设置")]
    [SerializeField] private FogMaskView fogMaskView; // 迷雾遮罩视图引用（可选，如果为空则自动查找）

    [Header("六边形布局设置")]
    [SerializeField] private bool useHexLayout = true; // 是否使用平顶六边形视觉布局
    
    private readonly Color _defaultOreColor = new Color32(0xE3, 0xC1, 0x76, 0xFF);
    
    // 已挖掘格子图片路径
    private const string MINED_TILE_SPRITE_PATH = "UI/Lattice/Lattice_null";
    
    // 矿石图片缓存
    private Dictionary<string, Sprite> _oreSpriteCache = new Dictionary<string, Sprite>();
    private Sprite _minedTileSprite; // 已挖掘格子图片缓存
    private Dictionary<Vector2Int, string> _tileOreIds = new Dictionary<Vector2Int, string>(); // 存储每个格子的矿石ID
    
    // 未完全挖掉的格子记录（用于红色高亮）
    private HashSet<Vector2Int> _damagedButNotMinedTiles = new HashSet<Vector2Int>();

    private void Awake()
    {
        _miningManager = MiningManager.Instance;
        _drillManager = DrillManager.Instance;
        _configManager = ConfigManager.Instance;
        _containerRectTransform = GetComponent<RectTransform>();
        
        // 获取父容器（LeftPanel）的RectTransform
        if (useParentSize && transform.parent != null)
        {
            _parentRectTransform = transform.parent.GetComponent<RectTransform>();
        }

        if (useHexLayout)
        {
            // 静态格子模式：HexLayoutGroup 在 MapGridRoot 上，与 PlatformGrid/GridRoot 一致
            if (useStaticCells && mapGridRoot != null)
            {
                hexLayout = mapGridRoot.GetComponent<HexLayoutGroup>();
                if (hexLayout == null)
                    hexLayout = mapGridRoot.gameObject.AddComponent<HexLayoutGroup>();
            }
            // 动态模式：HexLayoutGroup 可在 Inspector 指定或挂在自身
            if (hexLayout == null)
            {
                hexLayout = GetComponent<HexLayoutGroup>();
                if (hexLayout == null)
                    hexLayout = gameObject.AddComponent<HexLayoutGroup>();
            }

            if (hexLayout != null)
            {
                // 与 Inspector 中 spacing 保持一致，便于和 PlatformGrid 参数统一
                hexLayout.Spacing = spacing;

                // 缺省参数设置为 9x9 平顶六边形 odd-r，与 SPEC 中约定保持一致（逻辑 9x9，可在 UI 中扩展为 94 格正六边形）
                if (hexLayout.ConstraintCountEven <= 0) hexLayout.ConstraintCountEven = MiningManager.LAYER_WIDTH;
                if (hexLayout.ConstraintCountOdd <= 0) hexLayout.ConstraintCountOdd = MiningManager.LAYER_WIDTH;
                if (hexLayout.Orientation != HexLayoutGroup.HexOrientation.FlatTop)
                {
                    hexLayout.Orientation = HexLayoutGroup.HexOrientation.FlatTop;
                }
                if (hexLayout.StaggerAxis != HexLayoutGroup.HexStaggerAxis.Row)
                {
                    hexLayout.StaggerAxis = HexLayoutGroup.HexStaggerAxis.Row;
                }
                if (hexLayout.StaggerIndex != HexLayoutGroup.HexStaggerIndex.Odd)
                {
                    hexLayout.StaggerIndex = HexLayoutGroup.HexStaggerIndex.Odd;
                }
            }

            // 六边形模式下不再依赖 GridLayoutGroup
            if (gridLayout != null)
            {
                gridLayout.enabled = false;
            }
        }
        else
        {
            // 矩形网格模式：仍然使用 GridLayoutGroup
            if (gridLayout == null)
            {
                gridLayout = GetComponent<GridLayoutGroup>();
                if (gridLayout == null)
                {
                    gridLayout = gameObject.AddComponent<GridLayoutGroup>();
                }
            }

            if (gridLayout != null)
            {
                gridLayout.spacing = spacing;
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayout.constraintCount = MiningManager.LAYER_WIDTH; // 9列

                if (!autoResize)
                {
                    gridLayout.cellSize = new Vector2(60, 60);
                }
            }
        }
    }

    private void Start()
    {
        LoadChineseFont();
        InitializeFogMaskView();

        if (autoResize)
            CalculateCellSize();

        if (useStaticCells && mapGridRoot != null)
        {
            InitTilesFromChildren();
            if (autoResize)
                CalculateCellSize();
        }

        UpdateMap(1);
    }
    
    /// <summary>
    /// 从 MapGridRoot 子节点中的 MiningMapCell 初始化 _tileMap（静态格子方案，与 PlatformGrid 一致）
    /// </summary>
    private void InitTilesFromChildren()
    {
        if (mapGridRoot == null) return;

        _tileMap.Clear();
        _tileObjects.Clear();

        MiningMapCell[] cells = mapGridRoot.GetComponentsInChildren<MiningMapCell>(true);
        if (cells == null || cells.Length == 0)
        {
            Debug.LogWarning("MiningMapView: mapGridRoot 下未找到 MiningMapCell，请使用编辑器至少生成覆盖 9x9 逻辑网格的格子（可在外圈补充装饰格）或关闭 useStaticCells。");
            return;
        }

        int expectedLogicCount = MiningManager.LAYER_WIDTH * MiningManager.LAYER_HEIGHT;
        foreach (MiningMapCell cell in cells)
        {
            if (cell == null) continue;
            Vector2Int pos = cell.GridPosition;
            if (pos.x < 0 || pos.x >= MiningManager.LAYER_WIDTH || pos.y < 0 || pos.y >= MiningManager.LAYER_HEIGHT)
            {
                Debug.LogWarning($"MiningMapView: MiningMapCell 坐标越界 ({pos.x},{pos.y})，节点 {cell.gameObject.name}");
                continue;
            }
            if (_tileMap.ContainsKey(pos))
            {
                Debug.LogWarning($"MiningMapView: 重复坐标 ({pos.x},{pos.y})，节点 {cell.gameObject.name}");
                continue;
            }
            if (cell.image == null) cell.image = cell.GetComponent<Image>();
            if (cell.text == null) cell.text = cell.GetComponentInChildren<TextMeshProUGUI>(true);

            _tileMap[pos] = cell.gameObject;
            _tileObjects.Add(cell.gameObject);
        }

        if (_tileMap.Count != expectedLogicCount)
            Debug.LogWarning($"MiningMapView: 参与逻辑的静态格子数量为 {_tileMap.Count}，期望覆盖 {expectedLogicCount} 个逻辑坐标 (0,0)..(8,8)。如使用 94 格正六边形布局，请确保至少 81 个格子映射到 9x9 逻辑网格，其余外圈格子仅作为装饰。");
    }

    /// <summary>
    /// 初始化迷雾遮罩视图
    /// </summary>
    private void InitializeFogMaskView()
    {
        // 如果未在Inspector中指定，尝试自动查找
        if (fogMaskView == null)
        {
            // 在子对象中查找FogMaskView
            fogMaskView = GetComponentInChildren<FogMaskView>();
            
            // 如果还是找不到，尝试在父对象的子对象中查找
            if (fogMaskView == null && transform.parent != null)
            {
                fogMaskView = transform.parent.GetComponentInChildren<FogMaskView>();
            }
        }
        
        // 如果找到了FogMaskView，同步布局设置
        if (fogMaskView != null && gridLayout != null)
        {
            fogMaskView.SyncLayoutWithMiningMap(gridLayout);
        }
    }

    private void OnRectTransformDimensionsChange()
    {
        // 当RectTransform大小改变时，重新计算格子大小
        if (autoResize)
        {
            CalculateCellSize();
        }
    }

    /// <summary>
    /// 计算格子大小，使其自适应容器
    /// </summary>
    private void CalculateCellSize()
    {
        RectTransform targetRect = null;
        
        // 决定使用哪个RectTransform的大小
        // 静态格子模式：格子实际在 MapGridRoot 内，必须用 MapGridRoot 的 rect 计算，否则会按父节点（更大）算出 cellSize 导致格子偏大
        if (useStaticCells && mapGridRoot != null)
        {
            targetRect = mapGridRoot;
        }
        else if (useParentSize && _parentRectTransform != null)
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

        // 如果使用父容器大小，需要考虑当前容器的padding（根据当前使用的布局组件获取）
        RectOffset padding;
        if (useHexLayout && hexLayout != null)
        {
            padding = hexLayout.padding;
        }
        else if (gridLayout != null)
        {
            padding = gridLayout.padding;
        }
        else
        {
            padding = new RectOffset();
        }
        float paddingHorizontal = padding.left + padding.right;
        float paddingVertical = padding.top + padding.bottom;

        // 计算可用空间（减去padding）
        float availableWidth = containerWidth - paddingHorizontal;
        float availableHeight = containerHeight - paddingVertical;

        int columns = MiningManager.LAYER_WIDTH; // 9列
        int rows = MiningManager.LAYER_HEIGHT;   // 9行

        // 计算每个格子的大小
        float cellWidth;
        float cellHeight;

        if (!useHexLayout)
        {
            // 原有矩形网格计算：考虑间距
            float spacingWidth = spacing.x * (columns - 1);
            float spacingHeight = spacing.y * (rows - 1);
            cellWidth = (availableWidth - spacingWidth) / columns;
            cellHeight = (availableHeight - spacingHeight) / rows;
        }
        else
        {
            // 六边形布局：根据蜂窝整体宽高反推单个格子宽高
            // 需要考虑：(1) 实际子节点行数 (2) spacing (3) stagger 偏移

            // ── Step 1: 计算实际行列数（与 HexLayoutGroup.GetGridSize 一致） ──
            int actualChildCount = 0;
            if (useStaticCells && mapGridRoot != null)
                actualChildCount = mapGridRoot.childCount;
            else if (hexLayout != null)
                actualChildCount = hexLayout.transform.childCount;

            int countEven = hexLayout != null ? hexLayout.ConstraintCountEven : columns;
            int countOdd = hexLayout != null ? hexLayout.ConstraintCountOdd : columns;
            int maxCols = Mathf.Max(countEven, countOdd);

            int actualRows = 0;
            if (actualChildCount > 0)
            {
                int idx = 0;
                for (int r = 0; idx < actualChildCount; r++)
                {
                    idx += (r % 2 == 0) ? countEven : countOdd;
                    actualRows = r + 1;
                }
            }
            else
            {
                actualRows = rows; // fallback: 使用逻辑行数 9
                maxCols = columns;
            }

            // ── Step 2: 检查是否存在 stagger 偏移（奇数行水平偏移 0.5*horizontalStep） ──
            bool hasStagger = actualRows > 1;

            // ── Step 3: 从宽度约束反推 cellWidth ──
            // 实际所需宽度 = Nw * (0.75*w + sx) + w
            //   其中 Nw = (maxCols - 0.5) 若有 stagger，否则 (maxCols - 1)
            // 解出 w = (availW - Nw * sx) / (0.75 * Nw + 1)
            float Nw = hasStagger ? (maxCols - 0.5f) : (maxCols - 1f);
            float cellWidthFromW = (availableWidth - Nw * spacing.x) / Mathf.Max(0.75f * Nw + 1f, 1f);

            // ── Step 4: 从高度约束反推 cellHeight ──
            // 实际所需高度 = Nh * (0.866*h + sy) + h
            //   其中 Nh = actualRows - 1
            // 解出 h = (availH - Nh * sy) / (0.866 * Nh + 1)
            float Nh = actualRows - 1f;
            float cellHeightFromH = (availableHeight - Nh * spacing.y) / Mathf.Max(0.866f * Nh + 1f, 1f);

            // ── Step 5: 独立设置宽高，让格子同时填满水平和垂直方向 ──
            cellWidth = cellWidthFromW;
            cellHeight = cellHeightFromH;

        }

        // 确保大小为正数且合理
        if (cellWidth > 0 && cellHeight > 0)
        {
            Vector2 cellSize = new Vector2(cellWidth, cellHeight);

            if (useHexLayout && hexLayout != null)
            {
                hexLayout.CellSize = cellSize;
                // 同步 spacing，保证与 PlatformGrid 的 HexLayoutGroup 视觉效果一致
                hexLayout.Spacing = spacing;
            }
            else if (gridLayout != null)
            {
                gridLayout.cellSize = cellSize;
            }
        }
        else if (gridLayout != null && !useHexLayout)
        {
            Debug.LogWarning($"MiningMapView: 无法计算自适应格子大小（容器大小: {containerWidth}x{containerHeight}），使用默认值60x60");
            gridLayout.cellSize = new Vector2(60, 60);
        }

        // 同步迷雾遮罩的布局（目前主要用于层级顺序，对六边形/矩形布局均兼容）
        if (fogMaskView != null)
        {
            fogMaskView.SyncLayoutWithMiningMap(gridLayout);
        }
    }

    /// <summary>
    /// 加载中文字体
    /// </summary>
    private void LoadChineseFont()
    {
        // 使用动态字体加载器获取字体
        DynamicChineseFontLoader fontLoader = FindObjectOfType<DynamicChineseFontLoader>();
        if (fontLoader != null)
        {
            _chineseFont = fontLoader.DynamicFont;
            if (_chineseFont != null)
            {
                Debug.Log($"MiningMapView: 已从动态字体加载器获取字体: {_chineseFont.name}");
            }
            else
            {
                Debug.LogWarning("MiningMapView: 动态字体加载器存在但字体未创建");
            }
        }
        else
        {
            Debug.LogWarning("MiningMapView: 未找到 DynamicChineseFontLoader，字体可能未初始化");
        }
    }

    /// <summary>
    /// 更新地图显示
    /// </summary>
    public void UpdateMap(int layerDepth)
    {
        // 如果正在播放动画，跳过更新（防止动画被中断）
        if (_isAnimating)
        {
            return;
        }
        
        _currentLayerDepth = layerDepth;
        _loggedEmptyTileThisUpdate = false;

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

        if (!useHexLayout && gridLayout != null && !gridLayout.enabled)
        {
            gridLayout.enabled = true;
        }
        if (useHexLayout && hexLayout != null && !hexLayout.enabled)
            hexLayout.enabled = true;

        bool staticMode = useStaticCells && mapGridRoot != null && _tileMap.Count > 0;

        if (staticMode)
        {
            _tileOreIds.Clear();
            for (int x = 0; x < MiningManager.LAYER_WIDTH; x++)
            {
                for (int y = 0; y < MiningManager.LAYER_HEIGHT; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (!_tileMap.TryGetValue(pos, out GameObject tileObj)) continue;
                    MiningTileData tileData = grid[x, y];
                    UpdateTileVisual(tileObj, tileData);
                    if (tileData.tileType == TileType.Ore && !tileData.isMined)
                    {
                        string oreId = GetOreIdFromMineralType(tileData.mineralType);
                        if (!string.IsNullOrEmpty(oreId))
                            _tileOreIds[pos] = oreId;
                    }
                }
            }
        }
        else
        {
            ClearTiles();
            for (int y = MiningManager.LAYER_HEIGHT - 1; y >= 0; y--)
            {
                for (int x = 0; x < MiningManager.LAYER_WIDTH; x++)
                    CreateTile(x, y, grid[x, y]);
            }
        }

        if (enableHighlight)
            UpdateHighlight();
        if (fogMaskView != null)
            fogMaskView.UpdateFog(layerDepth);
    }

    /// <summary>
    /// 创建瓦片
    /// </summary>
    private void CreateTile(int x, int y, MiningTileData tileData)
    {
        GameObject tileObj;

        if (tilePrefab != null)
        {
            Transform parent = useHexLayout && hexLayout != null
                ? hexLayout.transform
                : (gridLayout != null ? gridLayout.transform : transform);
            tileObj = Instantiate(tilePrefab, parent);
        }
        else
        {
            // 动态创建瓦片
            Transform parent = useHexLayout && hexLayout != null
                ? hexLayout.transform
                : (gridLayout != null ? gridLayout.transform : transform);
            tileObj = new GameObject($"Tile_{x}_{y}");
            tileObj.transform.SetParent(parent, false);

            // 添加Image组件
            Image image = tileObj.AddComponent<Image>();
            
            // 优先处理已挖掘的格子：使用图片
            if (tileData.isMined)
            {
                Sprite minedSprite = GetMinedTileSprite();
                if (minedSprite != null)
                {
                    image.sprite = minedSprite;
                    image.color = Color.white; // 使用白色让图片显示原色
                }
                else
                {
                    image.color = GetTileColor(tileData);
                }
            }
            // 尝试加载矿石图片
            else
            {
                Sprite oreSprite = GetOreSpriteForTile(tileData);
                if (oreSprite != null)
                {
                    image.sprite = oreSprite;
                    image.color = Color.white; // 使用白色让图片显示原色
                }
                else
                {
                    image.color = GetTileColor(tileData);
                }
            }

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
            
            // 应用动态字体
            FontHelper.ApplyFontToText(text);
        }

        _tileObjects.Add(tileObj);
        
        // 添加到映射表
        Vector2Int pos = new Vector2Int(x, y);
        _tileMap[pos] = tileObj;
        
        // 存储矿石ID（用于后续效果）
        if (tileData.tileType == TileType.Ore && !tileData.isMined)
        {
            string oreId = GetOreIdFromMineralType(tileData.mineralType);
            if (!string.IsNullOrEmpty(oreId))
            {
                _tileOreIds[pos] = oreId;
            }
        }

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
            // 优先处理已挖掘的格子：使用图片
            if (tileData.isMined)
            {
                Sprite minedSprite = GetMinedTileSprite();
                if (minedSprite != null)
                {
                    image.sprite = minedSprite;
                    image.color = Color.white;
                    // 存储白色作为基础颜色（用于高亮计算）
                    _baseColors[new Vector2Int(tileData.x, tileData.y)] = Color.white;
                }
                else
                {
                    // 图片加载失败，回退到颜色显示
                    image.sprite = null;
                    Color baseColor = GetTileColor(tileData);
                    image.color = baseColor;
                    _baseColors[new Vector2Int(tileData.x, tileData.y)] = baseColor;
                }
            }
            // 尝试使用矿石图片
            else
            {
                Sprite oreSprite = GetOreSpriteForTile(tileData);
                if (oreSprite != null)
                {
                    image.sprite = oreSprite;
                    image.color = Color.white;
                    // 存储白色作为基础颜色（用于高亮计算）
                    _baseColors[new Vector2Int(tileData.x, tileData.y)] = Color.white;
                }
                else
                {
                    // 回退到颜色显示
                    image.sprite = null;
                    Color baseColor = GetTileColor(tileData);
                    image.color = baseColor;
                    _baseColors[new Vector2Int(tileData.x, tileData.y)] = baseColor;
                }
            }
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
                if (_configManager != null)
                {
                    return _configManager.GetHardnessColor(tileData.hardness);
                }
                return _defaultOreColor;
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
        _tileOreIds.Clear();
    }
    
    /// <summary>
    /// 获取已挖掘格子的Sprite
    /// </summary>
    private Sprite GetMinedTileSprite()
    {
        // 如果已缓存，直接返回
        if (_minedTileSprite != null)
        {
            return _minedTileSprite;
        }
        
        // 从Resources加载
        _minedTileSprite = Resources.Load<Sprite>(MINED_TILE_SPRITE_PATH);
        return _minedTileSprite;
    }
    
    /// <summary>
    /// 获取瓦片的矿石格子Sprite（用于地图显示）
    /// </summary>
    private Sprite GetOreSpriteForTile(MiningTileData tileData)
    {
        if (tileData.tileType != TileType.Ore || tileData.isMined)
        {
            return null;
        }
        
        // 获取矿石ID
        string oreId = GetOreIdFromMineralType(tileData.mineralType);
        if (string.IsNullOrEmpty(oreId))
        {
            return null;
        }
        
        // 获取矿石配置
        OreConfig config = _configManager?.GetOreConfig(oreId);
        if (config == null || string.IsNullOrEmpty(config.latticeSpritePath))
        {
            return null;
        }
        
        // 从缓存或Resources加载格子Sprite
        return LoadLatticeSprite(config.latticeSpritePath);
    }
    
    /// <summary>
    /// 加载矿石格子Sprite（带缓存）
    /// </summary>
    private Sprite LoadLatticeSprite(string spritePath)
    {
        if (string.IsNullOrEmpty(spritePath))
        {
            return null;
        }
        
        // 检查缓存
        if (_oreSpriteCache.TryGetValue(spritePath, out Sprite cachedSprite))
        {
            return cachedSprite;
        }
        
        // 从Resources加载
        Sprite sprite = Resources.Load<Sprite>(spritePath);
        if (sprite != null)
        {
            _oreSpriteCache[spritePath] = sprite;
        }
        
        return sprite;
    }
    
    /// <summary>
    /// 根据MineralType获取矿石ID
    /// </summary>
    private string GetOreIdFromMineralType(MineralType mineralType)
    {
        switch (mineralType)
        {
            case MineralType.Iron: return "iron";
            case MineralType.Gold: return "gold";
            case MineralType.Diamond: return "diamond";
            case MineralType.Crystal: return "crystal";
            case MineralType.EnergyCore: return "energy_core";
            default: return null;
        }
    }
    
    /// <summary>
    /// 获取瓦片的矿石ID
    /// </summary>
    public string GetTileOreId(Vector2Int position)
    {
        return _tileOreIds.TryGetValue(position, out string oreId) ? oreId : null;
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

        // 获取攻击范围
        HashSet<Vector2Int> attackRange;
        
        if (drill.UsesShapeSystem())
        {
            // 使用造型系统获取攻击范围
            attackRange = GetAttackRangeFromShapeSystem();
        }
        else
        {
            // 使用旧的矩形范围计算
            attackRange = GetAttackRangeLegacy(drill);
        }

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
                bool inRange = attackRange.Contains(pos);
                
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
    /// 从造型系统获取攻击范围
    /// </summary>
    private HashSet<Vector2Int> GetAttackRangeFromShapeSystem()
    {
        DrillAttackCalculator calculator = DrillAttackCalculator.Instance;
        return calculator.GetAttackRange();
    }

    /// <summary>
    /// 使用旧的矩形范围计算（向后兼容）
    /// </summary>
    private HashSet<Vector2Int> GetAttackRangeLegacy(DrillData drill)
    {
        HashSet<Vector2Int> range = new HashSet<Vector2Int>();
        
        // 获取当前层的钻头中心位置
        MiningLayerData layer = _miningManager.GetLayer(_currentLayerDepth);
        if (layer == null)
        {
            return range;
        }

        Vector2Int drillCenter = layer.drillCenter;
        #pragma warning disable 612, 618
        Vector2Int drillRange = drill.GetEffectiveRange();
        #pragma warning restore 612, 618

        // 计算攻击范围
        int halfRangeX = drillRange.x / 2;
        int halfRangeY = drillRange.y / 2;

        int minX = Mathf.Max(0, drillCenter.x - halfRangeX);
        int maxX = Mathf.Min(MiningManager.LAYER_WIDTH - 1, drillCenter.x + halfRangeX);
        int minY = Mathf.Max(0, drillCenter.y - halfRangeY);
        int maxY = Mathf.Min(MiningManager.LAYER_HEIGHT - 1, drillCenter.y + halfRangeY);

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                range.Add(new Vector2Int(x, y));
            }
        }

        return range;
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

    /// <summary>
    /// 播放晃动动画
    /// </summary>
    /// <param name="attackedTiles">被攻击的格子信息列表</param>
    /// <returns>协程，用于等待动画完成</returns>
    public IEnumerator PlayShakeAnimation(List<AttackedTileInfo> attackedTiles)
    {
        if (attackedTiles == null || attackedTiles.Count == 0)
        {
            yield break;
        }

        // 标记正在播放动画
        _isAnimating = true;
        
        // 停止所有正在进行的晃动动画
        StopAllShakeAnimations();
        
        // 清除并记录未完全挖掉的格子（用于红色高亮）
        _damagedButNotMinedTiles.Clear();
        foreach (var tile in attackedTiles)
        {
            if (!tile.isFullyMined)
            {
                _damagedButNotMinedTiles.Add(tile.position);
            }
        }

        // 按攻击强度分组格子
        var tilesByStrength = attackedTiles
            .GroupBy(t => t.attackStrength)
            .ToDictionary(g => g.Key, g => g.ToList());

        // 为每组启动同步的晃动协程
        List<Coroutine> coroutines = new List<Coroutine>();
        foreach (var group in tilesByStrength)
        {
            int strength = group.Key;
            List<AttackedTileInfo> tiles = group.Value;
            
            // 保存坐标列表而不是GameObject引用（因为UpdateMap可能会重建格子）
            List<Vector2Int> tilePositions = new List<Vector2Int>();
            foreach (var tileInfo in tiles)
            {
                tilePositions.Add(tileInfo.position);
            }

            if (tilePositions.Count > 0)
            {
                Coroutine coroutine = StartCoroutine(ShakeTilesCoroutine(tilePositions, strength));
                coroutines.Add(coroutine);
                _activeShakeCoroutines.Add(coroutine);
            }
        }

        // 等待所有协程完成
        foreach (var coroutine in coroutines)
        {
            yield return coroutine;
        }
        
        // 晃动结束，清除红色高亮记录
        _damagedButNotMinedTiles.Clear();
        
        // 清除动画标志
        _isAnimating = false;
    }

    /// <summary>
    /// 停止所有晃动动画
    /// </summary>
    private void StopAllShakeAnimations()
    {
        foreach (var coroutine in _activeShakeCoroutines)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
        _activeShakeCoroutines.Clear();

        // 恢复所有格子的原始位置
        foreach (var kvp in _originalPositions)
        {
            if (_tileMap.TryGetValue(kvp.Key, out GameObject tileObj) && tileObj != null)
            {
                RectTransform rect = tileObj.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.anchoredPosition = kvp.Value;
                }
            }
        }
        _originalPositions.Clear();
        
        // 如果有动画被强制停止，清除动画标志
        if (_isAnimating)
        {
            _isAnimating = false;
        }
    }

    /// <summary>
    /// 晃动协程
    /// </summary>
    /// <param name="tilePositions">要晃动的格子坐标列表</param>
    /// <param name="strength">攻击强度值（用于生成一致的随机参数）</param>
    private IEnumerator ShakeTilesCoroutine(List<Vector2Int> tilePositions, int strength)
    {
        if (tilePositions == null || tilePositions.Count == 0)
        {
            yield break;
        }

        bool wasGridLayoutEnabled = false;
        bool wasHexLayoutEnabled = false;
        if (gridLayout != null)
        {
            wasGridLayoutEnabled = gridLayout.enabled;
            gridLayout.enabled = false;
        }
        if (hexLayout != null)
        {
            wasHexLayoutEnabled = hexLayout.enabled;
            hexLayout.enabled = false; // 暂停六边形布局，避免覆盖晃动偏移
        }

        // 保存原始位置和颜色（使用坐标作为key，因为GameObject可能被重建）
        Dictionary<Vector2Int, Vector2> originalPositions = new Dictionary<Vector2Int, Vector2>();
        Dictionary<Vector2Int, Vector2> shakeDirections = new Dictionary<Vector2Int, Vector2>();
        Dictionary<Vector2Int, Color> originalColors = new Dictionary<Vector2Int, Color>();
        int rectTransformCount = 0;
        
        foreach (var pos in tilePositions)
        {
            // 从_tileMap中查找GameObject（每次循环都重新查找，因为可能被重建）
            if (_tileMap.TryGetValue(pos, out GameObject tile) && tile != null)
            {
                RectTransform rect = tile.GetComponent<RectTransform>();
                if (rect != null)
                {
                    originalPositions[pos] = rect.anchoredPosition;
                    _originalPositions[pos] = rect.anchoredPosition;
                    rectTransformCount++;
                    
                    // 使用强度值和格子位置生成一致的随机方向
                    int seed = strength * 1000 + pos.x * 100 + pos.y;
                    System.Random tileRandom = new System.Random(seed);
                    float angle = (float)(tileRandom.NextDouble() * 2 * Mathf.PI);
                    shakeDirections[pos] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    
                    // 保存原始颜色（用于红色高亮后恢复）
                    Image image = tile.GetComponent<Image>();
                    if (image != null)
                    {
                        originalColors[pos] = image.color;
                    }
                }
            }
        }
        
        Debug.Log($"[MiningMapView] ShakeTilesCoroutine: Found {rectTransformCount} valid tiles out of {tilePositions.Count} positions, shakeDuration={shakeDuration}s, shakeAmplitude={shakeAmplitude}");

        float startTime = Time.time; // 记录开始时间
        while (true)
        {
            float elapsedTime = Time.time - startTime; // 使用绝对时间计算，避免累积误差
            if (elapsedTime >= shakeDuration)
            {
                break; // 动画结束
            }
            
            float progress = elapsedTime / shakeDuration; // progress在0-1之间
            
            // 使用缓入缓出的动画曲线
            float curveValue = Mathf.SmoothStep(0f, 1f, progress);
            // 使用正弦波实现晃动效果
            float shakeValue = Mathf.Sin(elapsedTime * shakeFrequency * Mathf.PI * 2) * (1f - curveValue);
            float currentAmplitude = shakeAmplitude * (1f - curveValue); // 逐渐减小幅度
            
            // 红色高亮闪烁效果（使用正弦波）
            float highlightIntensity = (Mathf.Sin(elapsedTime * 8f) + 1f) / 2f; // 0-1 之间闪烁

            // 更新每个格子的位置和颜色（使用坐标从_tileMap重新查找GameObject）
            foreach (var pos in tilePositions)
            {
                // 每次循环都重新从_tileMap查找（因为UpdateMap可能重建了格子）
                if (!_tileMap.TryGetValue(pos, out GameObject tile) || tile == null)
                {
                    continue;
                }
                
                if (!originalPositions.ContainsKey(pos))
                {
                    continue;
                }
                
                RectTransform rect = tile.GetComponent<RectTransform>();
                if (rect == null)
                {
                    continue;
                }
                
                if (!shakeDirections.ContainsKey(pos))
                {
                    continue;
                }
                
                // 更新位置
                Vector2 offset = shakeDirections[pos] * shakeValue * currentAmplitude;
                rect.anchoredPosition = originalPositions[pos] + offset;
                
                // 如果是未完全挖掉的格子，添加红色高亮
                if (_damagedButNotMinedTiles.Contains(pos))
                {
                    Image image = tile.GetComponent<Image>();
                    if (image != null && originalColors.ContainsKey(pos))
                    {
                        // 红色高亮与原色混合（闪烁效果）
                        Color blendedColor = Color.Lerp(originalColors[pos], damageHighlightColor, highlightIntensity * 0.5f);
                        image.color = blendedColor;
                    }
                }
            }

            yield return null;
        }

        if (gridLayout != null)
            gridLayout.enabled = wasGridLayoutEnabled;
        if (hexLayout != null)
            hexLayout.enabled = wasHexLayoutEnabled;

        // 恢复原始位置和颜色（使用坐标从_tileMap重新查找GameObject）
        foreach (var pos in tilePositions)
        {
            if (originalPositions.ContainsKey(pos))
            {
                // 从_tileMap重新查找（因为可能被重建）
                if (_tileMap.TryGetValue(pos, out GameObject tile) && tile != null)
                {
                    RectTransform rect = tile.GetComponent<RectTransform>();
                    if (rect != null)
                    {
                        rect.anchoredPosition = originalPositions[pos];
                    }
                    
                    // 恢复原始颜色
                    if (originalColors.ContainsKey(pos))
                    {
                        Image image = tile.GetComponent<Image>();
                        if (image != null)
                        {
                            image.color = originalColors[pos];
                        }
                    }
                }
            }
            
            // 从全局字典中移除
            _originalPositions.Remove(pos);
        }
    }

    /// <summary>
    /// 获取格子的坐标位置
    /// </summary>
    private Vector2Int GetTilePosition(GameObject tile)
    {
        foreach (var kvp in _tileMap)
        {
            if (kvp.Value == tile)
            {
                return kvp.Key;
            }
        }
        return Vector2Int.one * -1; // 返回无效位置
    }
}
