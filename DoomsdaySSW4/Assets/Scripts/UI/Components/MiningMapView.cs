using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// 挖矿地图视图：显示挖矿地图（逻辑网格尺寸由 MiningManager.LAYER_WIDTH/LAYER_HEIGHT 决定，默认 9x11，可在 UI 层通过 HexLayoutGroup 扩展为蜂窝状布局）
/// </summary>
public class MiningMapView : MonoBehaviour
{
    [Header("地图设置")]
    [SerializeField] private GridLayoutGroup gridLayout;
    [SerializeField] private HexLayoutGroup hexLayout; // 六边形布局组件（与 PlatformGrid 保持一致）
    [SerializeField] private RectTransform mapGridRoot; // 静态格子容器（MapGridRoot，挂 HexLayoutGroup + 若干 MiningMapCell，支持逻辑格 + 额外装饰格）
    [SerializeField] private bool useStaticCells = false; // 为 true 时从 mapGridRoot 子节点初始化格子，不动态创建/销毁
    [SerializeField] private RectTransform platformGridRoot; // 平台格子容器（PlatformGrid 或其子 GridRoot），用于静态格子对齐
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
    private bool _syncedWithPlatform = false; // 标记是否已成功与平台格子完成世界坐标对齐（对齐后不再由 HexLayoutGroup 自动排布）
    
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

    [Header("钻头旋转")]
    [SerializeField] private bool enableDrillRotation = true; // 是否启用挖掘中钻头旋转
    [SerializeField] private float drillRotationDegreesPerSecond = 60f; // 旋转角速度（度/秒）

    [Header("挖掘描边")]
    [SerializeField] private bool enableMiningOutline = true; // 是否在挖掘过程中描边显示「当前正在被挖」的格子
    [SerializeField] private float miningOutlineWindowSeconds = 1f; // 视为「当前正在被挖掘」的时间窗口（秒）
    [SerializeField] private Color miningOutlineColor = Color.white; // 描边颜色（白色加粗描边）
    [SerializeField] private Vector2 miningOutlineDistance = new Vector2(5f, 5f); // 描边扩散距离（像素），越大描边越粗
    [SerializeField] private Color miningOutlineHighlightColor = new Color(1f, 1f, 1f, 1f); // 被描边格子的高亮色
    [SerializeField] [Range(0f, 1f)] private float miningOutlineHighlightBlend = 0.45f; // 高亮混合强度（0=无高亮，1=纯高亮色）
    
    private readonly Color _defaultOreColor = new Color32(0xE3, 0xC1, 0x76, 0xFF);
    
    // 已挖掘格子图片路径
    private const string MINED_TILE_SPRITE_PATH = "UI/Lattice/Lattice_null";
    
    // 矿石图片缓存
    private Dictionary<string, Sprite> _oreSpriteCache = new Dictionary<string, Sprite>();
    private Sprite _minedTileSprite; // 已挖掘格子图片缓存
    private Dictionary<Vector2Int, string> _tileOreIds = new Dictionary<Vector2Int, string>(); // 存储每个格子的矿石ID
    
    // 未完全挖掉的格子记录（用于红色高亮）
    private HashSet<Vector2Int> _damagedButNotMinedTiles = new HashSet<Vector2Int>();

    private bool _visualRotationPaused = false;
    private bool _isRotationAnimating = false;
    private HashSet<Vector2Int> _currentMiningOutlineTiles = new HashSet<Vector2Int>(); // 当前显示挖掘描边的格子

    /// <summary>
    /// 暂停/恢复视觉旋转（钻头编辑界面打开时暂停）
    /// </summary>
    public void SetVisualRotationPaused(bool paused)
    {
        _visualRotationPaused = paused;
        if (paused && platformGridRoot != null && !_isRotationAnimating)
        {
            platformGridRoot.localRotation = Quaternion.identity;
        }
    }

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

                // 缺省参数设置为与逻辑网格宽度一致的平顶六边形 odd-r，与 SPEC 中约定保持一致（逻辑尺寸由 LAYER_WIDTH/LAYER_HEIGHT 决定，默认 9x11，可在 UI 中扩展为蜂窝状布局）
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
                gridLayout.constraintCount = MiningManager.LAYER_WIDTH; // 列数由 LAYER_WIDTH 决定

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

            // 在静态格子方案下，如果绑定了平台格子容器，则在初始化完 MiningMapCell 后尝试根据 DrillPlatformCell 对齐一次位置和尺寸
            if (platformGridRoot != null)
            {
                SyncCellsWithPlatform();
            }

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
            Debug.LogWarning($"MiningMapView: mapGridRoot 下未找到 MiningMapCell，请使用编辑器至少生成覆盖逻辑网格 [0,0]..({MiningManager.LAYER_WIDTH - 1},{MiningManager.LAYER_HEIGHT - 1}) 的格子（可在外圈补充装饰格）或关闭 useStaticCells。");
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
            Debug.LogWarning($"MiningMapView: 参与逻辑的静态格子数量为 {_tileMap.Count}，期望覆盖 {expectedLogicCount} 个逻辑坐标 (0,0)..({MiningManager.LAYER_WIDTH - 1},{MiningManager.LAYER_HEIGHT - 1})。如使用多于逻辑格子的正六边形布局，请确保至少 LAYER_WIDTH×LAYER_HEIGHT 个格子映射到逻辑网格，其余外圈格子仅作为装饰。");
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
        if (fogMaskView != null)
        {
            if (gridLayout != null)
                fogMaskView.SyncLayoutWithMiningMap(gridLayout);
            // 六边形模式下传入 PlatformGrid，使迷雾按 DrillPlatformCell 中心对齐
            if (useHexLayout && platformGridRoot != null)
                fogMaskView.SetHexLayoutSource(platformGridRoot, mapGridRoot);
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
        // 已与平台格子完成对齐后，HexLayoutGroup 已禁用，格子尺寸由平台侧驱动，无需再自动计算
        if (_syncedWithPlatform)
            return;

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

        // 同步迷雾遮罩的布局（层级顺序 + 六边形时传入 platformGridRoot 以对齐中心）
        if (fogMaskView != null)
        {
            fogMaskView.SyncLayoutWithMiningMap(gridLayout);
            if (useHexLayout && platformGridRoot != null)
                fogMaskView.SetHexLayoutSource(platformGridRoot, mapGridRoot);
        }
    }

    /// <summary>
    /// 在静态格子方案下，根据 PlatformGrid 中 DrillPlatformCell 的 RectTransform
    /// 对 MapGridRoot 下的 MiningMapCell 做一次**几何对齐**（位置 + 尺寸）。
    /// 设计约定（与 SPEC 保持一致）：
    /// - 坐标映射：使用 (x,y) 建立 DrillPlatformCell ↔ MiningMapCell 的 1:1 对应关系，
    ///   逻辑范围为 [0, MiningManager.LAYER_WIDTH) × [0, MiningManager.LAYER_HEIGHT)（当前 9×11，即 [0,8]×[0,10]）；
    /// - 宽高同步：MiningMapCell 的 RectTransform.sizeDelta 直接拷贝同坐标 DrillPlatformCell 的 sizeDelta，
    ///   即“挖矿地图格子的 Width/Height 参数由平台格子的 Width/Height 直接驱动”；
    /// - 中心点语义：要求 platformGridRoot / mapGridRoot 下子格子的 anchor 与 pivot 统一为中心（推荐 (0.5,0.5)），
    ///   此时复制 anchoredPosition 即可保证两侧格子的几何中心点在父容器空间内完全重合；
        /// - 若未来启用基于 RectTransform.rect.center + 世界坐标转换的精确中心对齐方案（参见 SPEC 中方案 B），
        ///   则需要在此处改为通过 TransformPoint / InverseTransformPoint 计算 localPosition，并确保布局组件不再重排子节点；
        /// - 超出逻辑网格范围的外圈装饰格子不参与同步，仅作为视觉装饰。
    /// </summary>
    public void SyncCellsWithPlatform()
    {
        // 仅在静态格子 + 参数完整时执行
        if (!useStaticCells || mapGridRoot == null || platformGridRoot == null)
        {
            return;
        }

        // ── 关键修复：强制 Canvas 立即执行一轮布局重建 ──
        // 确保平台侧 HexLayoutGroup 已经对子节点执行了 LayoutChildren，
        // 否则平台格子的 anchoredPosition / sizeDelta 仍为初始零值。
        Canvas.ForceUpdateCanvases();

        // 收集平台格子
        DrillPlatformCell[] platformCells = platformGridRoot.GetComponentsInChildren<DrillPlatformCell>(true);
        if (platformCells == null || platformCells.Length == 0)
        {
            Debug.LogWarning("MiningMapView.SyncCellsWithPlatform: 在 platformGridRoot 下未找到 DrillPlatformCell，跳过对齐。");
            return;
        }

        int maxX = MiningManager.LAYER_WIDTH;
        int maxY = MiningManager.LAYER_HEIGHT;

        var platformMap = new Dictionary<Vector2Int, RectTransform>();

        foreach (var cell in platformCells)
        {
            if (cell == null) continue;
            Vector2Int pos = cell.GridPosition;

            if (pos.x < 0 || pos.x >= maxX || pos.y < 0 || pos.y >= maxY)
                continue;

            RectTransform rect = cell.transform as RectTransform;
            if (rect == null) continue;

            if (platformMap.ContainsKey(pos))
            {
                Debug.LogWarning($"MiningMapView.SyncCellsWithPlatform: 平台侧存在重复坐标 ({pos.x},{pos.y})，节点 {cell.gameObject.name}");
                continue;
            }

            platformMap[pos] = rect;
        }

        if (platformMap.Count == 0)
        {
            Debug.LogWarning("MiningMapView.SyncCellsWithPlatform: 未找到任何有效的平台格子坐标，跳过对齐。");
            return;
        }

        if (_tileMap == null || _tileMap.Count == 0)
        {
            Debug.LogWarning("MiningMapView.SyncCellsWithPlatform: _tileMap 为空，请确认已调用 InitTilesFromChildren 且启用了 useStaticCells。");
            return;
        }

        // ── 禁用 MapGridRoot 上的 HexLayoutGroup，防止布局组件在后续帧覆盖手动设置 ──
        HexLayoutGroup mapHexLayout = mapGridRoot.GetComponent<HexLayoutGroup>();
        if (mapHexLayout != null)
        {
            mapHexLayout.enabled = false;
        }

        int syncCount = 0;
        // #region agent log: drill-mining-align-sync
        try
        {
            // 仅用于当前 AI 调试会话的对齐诊断，记录若干关键坐标的世界坐标
            Vector2Int[] probeCoords = new Vector2Int[]
            {
                new Vector2Int(4, 4), // 逻辑中心
                new Vector2Int(0, 0), // 左下角
                new Vector2Int(MiningManager.LAYER_WIDTH - 1, 0), // 右下角
                new Vector2Int(0, MiningManager.LAYER_HEIGHT - 1) // 左上角
            };

            foreach (var probe in probeCoords)
            {
                if (platformMap.TryGetValue(probe, out RectTransform platRect))
                {
                    Vector3 platCenterWorld = platRect.TransformPoint(platRect.rect.center);
                    long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    string logLine =
                        "{" +
                        "\"sessionId\":\"d33074ff-81b8-4f92-a63c-c6ba6401768d\"," +
                        "\"runId\":\"pre-fix\"," +
                        "\"hypothesisId\":\"H1_platform_vs_mining_layout\"," +
                        "\"location\":\"MiningMapView.SyncCellsWithPlatform:platformProbe\"," +
                        "\"message\":\"Platform cell world center probe\"," +
                        "\"data\":{" +
                            "\"gridX\":" + probe.x + "," +
                            "\"gridY\":" + probe.y + "," +
                            "\"worldX\":" + platCenterWorld.x.ToString("F4") + "," +
                            "\"worldY\":" + platCenterWorld.y.ToString("F4") +
                        "}," +
                        "\"timestamp\":" + ts +
                        "}";
                    System.IO.File.AppendAllText("e:/Work/Cursor/DoomsdaySSW4/debug-d33074ff-81b8-4f92-a63c-c6ba6401768d.log", logLine + "\n");
                }
            }
        }
        catch { }
        // #endregion

        foreach (var kvp in _tileMap)
        {
            Vector2Int pos = kvp.Key;

            if (pos.x < 0 || pos.x >= maxX || pos.y < 0 || pos.y >= maxY)
                continue;

            if (!platformMap.TryGetValue(pos, out RectTransform platformRect))
                continue;

            GameObject miningObj = kvp.Value;
            if (miningObj == null) continue;

            RectTransform miningRect = miningObj.transform as RectTransform;
            if (miningRect == null) continue;

            // 方案 B：基于世界坐标中心 + 尺寸对齐
            // 1. 计算平台格子的世界中心坐标
            Vector3 worldCenter = platformRect.TransformPoint(platformRect.rect.center);
            // 2. 转换到 MapGridRoot 的本地坐标
            Vector3 localCenterInMap = mapGridRoot.InverseTransformPoint(worldCenter);
            // 3. 设置挖矿格子的本地位置（中心点对齐）
            miningRect.localPosition = localCenterInMap;
            // 4. 通过世界空间四角反算在 MapGridRoot 本地空间中的正确尺寸
            //    （不能直接复制 sizeDelta，因为两个父容器的缩放可能不同）
            Vector3[] platCorners = new Vector3[4];
            platformRect.GetWorldCorners(platCorners);
            // GetWorldCorners: [0]=bottom-left, [1]=top-left, [2]=top-right, [3]=bottom-right
            Vector3 localBL = mapGridRoot.InverseTransformPoint(platCorners[0]);
            Vector3 localTR = mapGridRoot.InverseTransformPoint(platCorners[2]);
            float localWidth = Mathf.Abs(localTR.x - localBL.x);
            float localHeight = Mathf.Abs(localTR.y - localBL.y);
            miningRect.sizeDelta = new Vector2(localWidth, localHeight);

            syncCount++;
        }

        if (syncCount > 0)
        {
            _syncedWithPlatform = true;
            if (fogMaskView != null && platformGridRoot != null)
                fogMaskView.SetHexLayoutSource(platformGridRoot, mapGridRoot);
            Debug.Log($"MiningMapView.SyncCellsWithPlatform: 已通过世界坐标对齐 {syncCount} 个格子，已禁用 MapGridRoot 的 HexLayoutGroup。");

            // #region agent log: drill-mining-align-synced
            try
            {
                // 对齐完成后，再采样少量格子，比较平台与挖掘格子的世界中心差值
                Vector2Int[] sampleCoords = new Vector2Int[]
                {
                    new Vector2Int(4, 4),
                    new Vector2Int(2, 2),
                    new Vector2Int(6, 6)
                };

                foreach (var coord in sampleCoords)
                {
                    if (!platformMap.TryGetValue(coord, out RectTransform platRect)) continue;
                    if (!_tileMap.TryGetValue(coord, out GameObject miningObj) || miningObj == null) continue;

                    RectTransform miningRect = miningObj.transform as RectTransform;
                    if (miningRect == null) continue;

                    Vector3 platCenterWorld = platRect.TransformPoint(platRect.rect.center);
                    Vector3 miningCenterWorld = miningRect.TransformPoint(miningRect.rect.center);

                    long ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    string logLine =
                        "{" +
                        "\"sessionId\":\"d33074ff-81b8-4f92-a63c-c6ba6401768d\"," +
                        "\"runId\":\"pre-fix\"," +
                        "\"hypothesisId\":\"H1_platform_vs_mining_layout\"," +
                        "\"location\":\"MiningMapView.SyncCellsWithPlatform:postSyncSample\"," +
                        "\"message\":\"Post-sync world center diff\"," +
                        "\"data\":{" +
                            "\"gridX\":" + coord.x + "," +
                            "\"gridY\":" + coord.y + "," +
                            "\"platformWorldX\":" + platCenterWorld.x.ToString("F4") + "," +
                            "\"platformWorldY\":" + platCenterWorld.y.ToString("F4") + "," +
                            "\"miningWorldX\":" + miningCenterWorld.x.ToString("F4") + "," +
                            "\"miningWorldY\":" + miningCenterWorld.y.ToString("F4") + "," +
                            "\"deltaX\":" + (miningCenterWorld.x - platCenterWorld.x).ToString("F4") + "," +
                            "\"deltaY\":" + (miningCenterWorld.y - platCenterWorld.y).ToString("F4") +
                        "}," +
                        "\"timestamp\":" + ts +
                        "}";
                    System.IO.File.AppendAllText("e:/Work/Cursor/DoomsdaySSW4/debug-d33074ff-81b8-4f92-a63c-c6ba6401768d.log", logLine + "\n");
                }
            }
            catch { }
            // #endregion
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
        // 已与平台对齐时不重新启用 HexLayoutGroup，避免覆盖手动设置的位置/尺寸
        if (useHexLayout && hexLayout != null && !hexLayout.enabled && !_syncedWithPlatform)
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
    /// 从圆环扫掠系统获取攻击范围
    /// </summary>
    private HashSet<Vector2Int> GetAttackRangeFromShapeSystem()
    {
        DrillAttackCalculator calculator = DrillAttackCalculator.Instance;
        return calculator.GetCircularSweepRange(MiningManager.LAYER_WIDTH, MiningManager.LAYER_HEIGHT);
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
    /// 获取在角度区间 [angleStart, angleEnd] 内被攻击的格子集合（用于「当前 N 秒正在被挖掘」描边）
    /// </summary>
    private static HashSet<Vector2Int> GetTilesInAngleRange(
        List<float> sortedAngles,
        Dictionary<float, List<Vector2Int>> angleToTargets,
        float angleStart,
        float angleEnd)
    {
        HashSet<Vector2Int> set = new HashSet<Vector2Int>();
        if (sortedAngles == null || angleToTargets == null) return set;
        foreach (float angle in sortedAngles)
        {
            if (angle < angleStart) continue;
            if (angle > angleEnd) break;
            if (angleToTargets.TryGetValue(angle, out List<Vector2Int> targets) && targets != null)
            {
                foreach (var pos in targets)
                    set.Add(pos);
            }
        }
        return set;
    }

    /// <summary>
    /// 为指定格子设置或清除「正在被挖掘」描边（Unity Outline 组件）并叠加高亮效果
    /// </summary>
    private void SetMiningOutlineForTiles(IEnumerable<Vector2Int> positions)
    {
        HashSet<Vector2Int> newSet = positions == null ? new HashSet<Vector2Int>() : new HashSet<Vector2Int>(positions);

        // 移除不再需要描边的格子：关闭描边并恢复格子原始颜色
        foreach (var pos in _currentMiningOutlineTiles.ToList())
        {
            if (newSet.Contains(pos)) continue;
            if (_tileMap.TryGetValue(pos, out GameObject tileObj) && tileObj != null)
            {
                Image img = tileObj.GetComponent<Image>();
                if (img != null)
                {
                    Outline outline = img.GetComponent<Outline>();
                    if (outline != null)
                        outline.enabled = false;
                    if (_baseColors.TryGetValue(pos, out Color baseColor))
                        img.color = baseColor;
                }
            }
            _currentMiningOutlineTiles.Remove(pos);
        }

        // 为需要描边的格子：加粗白色描边 + 高亮
        foreach (var pos in newSet)
        {
            if (!_tileMap.TryGetValue(pos, out GameObject tileObj) || tileObj == null) continue;
            Image img = tileObj.GetComponent<Image>();
            if (img == null) continue;
            Outline outline = img.GetComponent<Outline>();
            if (outline == null)
                outline = img.gameObject.AddComponent<Outline>();
            outline.effectColor = miningOutlineColor;
            outline.effectDistance = miningOutlineDistance;
            outline.enabled = true;
            if (_baseColors.TryGetValue(pos, out Color baseColor))
                img.color = Color.Lerp(baseColor, miningOutlineHighlightColor, miningOutlineHighlightBlend);
            _currentMiningOutlineTiles.Add(pos);
        }
    }

    /// <summary>
    /// 播放旋转挖掘动画：platformGridRoot 以指定角速度顺时针旋转360度，
    /// 过程中按角度触发各矿石格的攻击特效。
    /// </summary>
    /// <param name="degreesPerSecond">旋转角速度（度/秒），默认60</param>
    /// <param name="angleToTargets">角度 -> 该角度应触发攻击的目标格列表</param>
    /// <param name="attackedTiles">预计算的攻击结果（用于特效展示）</param>
    public IEnumerator PlayRotationMiningAnimation(
        float degreesPerSecond,
        Dictionary<float, List<Vector2Int>> angleToTargets,
        List<AttackedTileInfo> attackedTiles)
    {
        if (platformGridRoot == null || !enableDrillRotation)
        {
            yield break;
        }

        _isAnimating = true;
        _isRotationAnimating = true;

        // 构建攻击结果查找表
        Dictionary<Vector2Int, AttackedTileInfo> attackLookup = new Dictionary<Vector2Int, AttackedTileInfo>();
        if (attackedTiles != null)
        {
            foreach (var tile in attackedTiles)
            {
                if (!attackLookup.ContainsKey(tile.position))
                    attackLookup[tile.position] = tile;
            }
        }

        // 将 angleToTargets 的 key 排序到列表中，便于按角度顺序触发
        List<float> sortedAngles = new List<float>();
        if (angleToTargets != null)
        {
            sortedAngles.AddRange(angleToTargets.Keys);
            sortedAngles.Sort();
        }

        int nextAngleIndex = 0;
        float totalRotated = 0f;

        // 保存初始旋转
        Quaternion startRotation = platformGridRoot.localRotation;

        // 收集旋转过程中被触发的格子，旋转结束后统一播放晃动
        List<AttackedTileInfo> pendingShakeTiles = new List<AttackedTileInfo>();
        float lastOutlineShakeTime = Time.time; // 用于「每秒当前被挖掘格子振动1次」

        while (totalRotated < 360f)
        {
            if (_visualRotationPaused)
            {
                yield return null;
                continue;
            }

            float deltaAngle = degreesPerSecond * Time.deltaTime;
            totalRotated += deltaAngle;
            if (totalRotated > 360f) totalRotated = 360f;

            // 顺时针旋转（Unity UI 中 Z 轴负方向为顺时针）
            platformGridRoot.localRotation = startRotation * Quaternion.Euler(0f, 0f, -totalRotated);

            // 检查是否有新的角度被经过，仅收集待晃动格子（不在此处更新矿石格视觉，等振动后再刷新）
            while (nextAngleIndex < sortedAngles.Count && sortedAngles[nextAngleIndex] <= totalRotated)
            {
                float angle = sortedAngles[nextAngleIndex];
                if (angleToTargets.TryGetValue(angle, out List<Vector2Int> targets))
                {
                    foreach (var pos in targets)
                    {
                        if (attackLookup.TryGetValue(pos, out AttackedTileInfo tileInfo))
                        {
                            if (!pendingShakeTiles.Any(t => t.position == pos))
                                pendingShakeTiles.Add(tileInfo);
                        }
                    }
                }
                nextAngleIndex++;
            }

            // 描边：显示「当前 miningOutlineWindowSeconds 秒内」正在被挖掘的矿石格
            HashSet<Vector2Int> outlineTiles = null;
            if (enableMiningOutline && angleToTargets != null && sortedAngles.Count > 0)
            {
                float angleWindow = degreesPerSecond * miningOutlineWindowSeconds;
                float angleStart = Mathf.Max(0f, totalRotated - angleWindow);
                float angleEnd = totalRotated;
                outlineTiles = GetTilesInAngleRange(sortedAngles, angleToTargets, angleStart, angleEnd);
                SetMiningOutlineForTiles(outlineTiles);
            }

            // 每秒让当前被挖掘的格子振动 1 次（不阻塞旋转，不清除 animating 状态）
            if (outlineTiles != null && outlineTiles.Count > 0 && Time.time - lastOutlineShakeTime >= 1f)
            {
                lastOutlineShakeTime = Time.time;
                List<AttackedTileInfo> outlineShakeList = new List<AttackedTileInfo>();
                foreach (var pos in outlineTiles)
                {
                    if (attackLookup.TryGetValue(pos, out AttackedTileInfo info))
                        outlineShakeList.Add(info);
                }
                if (outlineShakeList.Count > 0)
                    StartCoroutine(PlayShakeAnimation(outlineShakeList, clearAnimatingAtEnd: false, stopExistingShakes: false));
            }

            yield return null;
        }

        // 旋转结束后清除挖掘描边，再播放振动
        SetMiningOutlineForTiles(new List<Vector2Int>());

        // 旋转结束后：先播放挖掘振动（晃动），再由外部在振动结束后调用 UpdateMap 刷新矿石格状态
        if (pendingShakeTiles.Count > 0)
        {
            yield return PlayShakeAnimation(pendingShakeTiles);
        }

        // 旋转完成，重置到初始角度
        platformGridRoot.localRotation = startRotation;

        _isRotationAnimating = false;
        _isAnimating = false;
    }

    /// <summary>
    /// 播放晃动动画
    /// </summary>
    /// <param name="attackedTiles">被攻击的格子信息列表</param>
    /// <param name="clearAnimatingAtEnd">结束时是否清除 _isAnimating（从旋转中每秒触发的描边振动传 false）</param>
    /// <param name="stopExistingShakes">是否先停止已有晃动（从旋转中触发的描边振动传 false 以允许多段重叠）</param>
    /// <returns>协程，用于等待动画完成</returns>
    public IEnumerator PlayShakeAnimation(List<AttackedTileInfo> attackedTiles, bool clearAnimatingAtEnd = true, bool stopExistingShakes = true)
    {
        if (attackedTiles == null || attackedTiles.Count == 0)
        {
            yield break;
        }

        if (clearAnimatingAtEnd)
            _isAnimating = true;
        if (stopExistingShakes)
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
        
        if (clearAnimatingAtEnd)
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
