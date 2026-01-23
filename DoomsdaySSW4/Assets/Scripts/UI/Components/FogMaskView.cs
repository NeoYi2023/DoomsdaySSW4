using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 迷雾遮罩视图：在挖矿地图上显示黑色迷雾遮罩，从边缘向中心扩展
/// 钻头格子及其周围区域无迷雾
/// 使用Shader实现，性能更好，效果更平滑
/// </summary>
public class FogMaskView : MonoBehaviour
{
    [Header("迷雾设置")]
    [SerializeField] private Color fogColor = new Color(0f, 0f, 0f, 1f); // 迷雾颜色（纯黑色，RGB=0,0,0）
    [SerializeField] private float maxFogAlpha = 1.0f; // 最大迷雾透明度
    [SerializeField] private float revealRadius = 2.0f; // 钻头周围完全无迷雾的半径（格子数）
    [SerializeField] private float fadeDistance = 3.0f; // 从无迷雾到完全迷雾的渐变距离（格子数）
    
    [Header("Shader引用")]
    [SerializeField] private Shader fogMaskShader; // 迷雾Shader（如果为空则自动查找）
    
    private Image _fogImage; // 单个Image组件
    private Material _fogMaterial; // Shader Material实例
    private Texture2D _attackRangeTexture; // 攻击范围掩码纹理
    private MiningManager _miningManager;
    private DrillManager _drillManager;
    private RectTransform _containerRectTransform;
    private int _currentLayerDepth = 1;
    private Vector2Int? _lastDrillCenter; // 上次的钻头中心位置（用于检测变化）
    private HashSet<Vector2Int> _lastAttackRange; // 上次的攻击范围（用于检测变化）
    
    private void Awake()
    {
        _miningManager = MiningManager.Instance;
        _drillManager = DrillManager.Instance;
        _containerRectTransform = GetComponent<RectTransform>();
        
        // 确保FogMaskContainer使用绝对定位，填充整个父容器
        if (_containerRectTransform != null)
        {
            // 设置为填充整个父容器，使用锚点拉伸
            _containerRectTransform.anchorMin = Vector2.zero;
            _containerRectTransform.anchorMax = Vector2.one;
            _containerRectTransform.sizeDelta = Vector2.zero;
            _containerRectTransform.anchoredPosition = Vector2.zero;
        }
        
        // 添加LayoutElement组件，确保FogMaskContainer忽略父容器的GridLayoutGroup布局
        LayoutElement layoutElement = GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = gameObject.AddComponent<LayoutElement>();
        }
        if (layoutElement != null)
        {
            layoutElement.ignoreLayout = true; // 忽略父容器的布局系统
        }
        
        // 确保FogMaskContainer在MiningMapView的GridLayoutGroup子对象之后（更高的SiblingIndex）
        // 这样遮罩会显示在格子之上
        Transform parent = transform.parent;
        if (parent != null)
        {
            // 查找MiningMapView组件
            MiningMapView miningMapView = parent.GetComponent<MiningMapView>();
            if (miningMapView != null)
            {
                // 将FogMaskContainer移动到最后一个子对象之后
                transform.SetAsLastSibling();
            }
        }
    }
    
    private void Start()
    {
        // 初始化迷雾遮罩
        InitializeFogMask();
    }
    
    private void Update()
    {
        // 实时检测钻头位置和攻击范围变化
        CheckAndUpdateFog();
    }
    
    private void OnDestroy()
    {
        // 清理资源
        if (_fogMaterial != null)
        {
            Destroy(_fogMaterial);
        }
        if (_attackRangeTexture != null)
        {
            Destroy(_attackRangeTexture);
        }
    }
    
    /// <summary>
    /// 初始化迷雾遮罩（使用Shader实现）
    /// </summary>
    private void InitializeFogMask()
    {
        // 创建或获取Image组件
        _fogImage = GetComponent<Image>();
        if (_fogImage == null)
        {
            _fogImage = gameObject.AddComponent<Image>();
        }
        
        // 配置Image组件
        _fogImage.raycastTarget = false; // 不阻挡鼠标事件
        _fogImage.type = Image.Type.Simple;
        _fogImage.color = Color.white; // 颜色由Shader控制
        
        // 重要：Unity UI的Image组件需要Sprite才能渲染
        // 如果没有sprite，即使有Material也不会显示
        // 使用默认的白色sprite（1x1白色纹理）
        if (_fogImage.sprite == null)
        {
            Texture2D whiteTexture = Texture2D.whiteTexture;
            _fogImage.sprite = Sprite.Create(whiteTexture, new Rect(0, 0, whiteTexture.width, whiteTexture.height), new Vector2(0.5f, 0.5f));
        }
        
        // 加载或查找Shader
        if (fogMaskShader == null)
        {
            fogMaskShader = Shader.Find("UI/FogMask");
            if (fogMaskShader == null)
            {
                Debug.LogError("FogMaskView: 无法找到UI/FogMask Shader，请确保Shader已正确导入");
                return;
            }
        }
        
        // 创建Material实例
        _fogMaterial = new Material(fogMaskShader);
        _fogImage.material = _fogMaterial;
        
        // 初始化攻击范围纹理（9x9，每个像素代表一个格子）
        _attackRangeTexture = new Texture2D(MiningManager.LAYER_WIDTH, MiningManager.LAYER_HEIGHT, TextureFormat.R8, false);
        _attackRangeTexture.filterMode = FilterMode.Point; // 使用点过滤，确保精确的像素对应
        _attackRangeTexture.wrapMode = TextureWrapMode.Clamp;
        
        // 初始化Material参数
        UpdateFogMaterial();
    }
    
    /// <summary>
    /// 同步MiningMapView的布局设置（保留接口以兼容）
    /// </summary>
    public void SyncLayoutWithMiningMap(GridLayoutGroup miningMapGrid)
    {
        // 确保FogMaskContainer在MiningMapView的GridLayoutGroup之后（更高的SiblingIndex）
        Transform parent = transform.parent;
        if (parent != null)
        {
            transform.SetAsLastSibling();
        }
    }
    
    /// <summary>
    /// 更新迷雾遮罩（外部调用）
    /// </summary>
    public void UpdateFog(int layerDepth)
    {
        _currentLayerDepth = layerDepth;
        RefreshFog();
    }
    
    /// <summary>
    /// 检查并更新迷雾（实时更新）
    /// </summary>
    private void CheckAndUpdateFog()
    {
        if (_miningManager == null || _drillManager == null || _fogMaterial == null)
        {
            return;
        }
        
        // 获取当前层的钻头中心位置
        MiningLayerData layer = _miningManager.GetLayer(_currentLayerDepth);
        if (layer == null)
        {
            return;
        }
        
        Vector2Int currentDrillCenter = layer.drillCenter;
        
        // 获取当前攻击范围
        HashSet<Vector2Int> currentAttackRange = GetCurrentAttackRange();
        
        // 检查是否有变化
        bool needsUpdate = false;
        if (!_lastDrillCenter.HasValue || _lastDrillCenter.Value != currentDrillCenter)
        {
            needsUpdate = true;
        }
        
        if (currentAttackRange != null && !AreHashSetsEqual(_lastAttackRange, currentAttackRange))
        {
            needsUpdate = true;
        }
        
        if (needsUpdate)
        {
            _lastDrillCenter = currentDrillCenter;
            _lastAttackRange = new HashSet<Vector2Int>(currentAttackRange);
            RefreshFog();
        }
    }
    
    /// <summary>
    /// 刷新迷雾遮罩
    /// </summary>
    private void RefreshFog()
    {
        if (_miningManager == null || _fogMaterial == null)
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
        
        // 获取当前攻击范围（钻头占用的格子）
        HashSet<Vector2Int> attackRange = GetCurrentAttackRange();
        
        // 更新Material参数
        UpdateFogMaterial(drillCenter, attackRange);
    }
    
    /// <summary>
    /// 更新Material参数
    /// </summary>
    private void UpdateFogMaterial()
    {
        if (_miningManager == null || _fogMaterial == null)
        {
            return;
        }
        
        MiningLayerData layer = _miningManager.GetLayer(_currentLayerDepth);
        if (layer == null)
        {
            return;
        }
        
        Vector2Int drillCenter = layer.drillCenter;
        HashSet<Vector2Int> attackRange = GetCurrentAttackRange();
        
        UpdateFogMaterial(drillCenter, attackRange);
    }
    
    /// <summary>
    /// 更新Material参数
    /// </summary>
    private void UpdateFogMaterial(Vector2Int drillCenter, HashSet<Vector2Int> attackRange)
    {
        if (_fogMaterial == null)
        {
            return;
        }
        
        // 将钻头中心位置归一化到0-1范围
        Vector2 normalizedDrillCenter = new Vector2(
            drillCenter.x / (float)(MiningManager.LAYER_WIDTH - 1),
            drillCenter.y / (float)(MiningManager.LAYER_HEIGHT - 1)
        );
        
        // 更新攻击范围纹理
        UpdateAttackRangeTexture(attackRange);
        
        // 设置Material参数
        _fogMaterial.SetColor("_FogColor", fogColor);
        _fogMaterial.SetVector("_DrillCenter", normalizedDrillCenter);
        _fogMaterial.SetFloat("_RevealRadius", revealRadius);
        _fogMaterial.SetFloat("_FadeDistance", fadeDistance);
        _fogMaterial.SetFloat("_MaxFogAlpha", Mathf.Clamp01(maxFogAlpha));
        _fogMaterial.SetVector("_GridSize", new Vector2(MiningManager.LAYER_WIDTH, MiningManager.LAYER_HEIGHT));
        _fogMaterial.SetTexture("_AttackRangeTex", _attackRangeTexture);
    }
    
    /// <summary>
    /// 更新攻击范围纹理
    /// </summary>
    private void UpdateAttackRangeTexture(HashSet<Vector2Int> attackRange)
    {
        if (_attackRangeTexture == null)
        {
            return;
        }
        
        // 创建颜色数组，初始化所有像素为黑色（不在攻击范围内）
        Color[] pixels = new Color[MiningManager.LAYER_WIDTH * MiningManager.LAYER_HEIGHT];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.black; // R=0表示不在攻击范围内
        }
        
        // 将攻击范围内的格子标记为白色（R=1表示在攻击范围内）
        if (attackRange != null)
        {
            foreach (Vector2Int pos in attackRange)
            {
                // 确保坐标在有效范围内
                if (pos.x >= 0 && pos.x < MiningManager.LAYER_WIDTH &&
                    pos.y >= 0 && pos.y < MiningManager.LAYER_HEIGHT)
                {
                    // 注意：Unity的Texture2D坐标系统，y=0在底部
                    // 但我们的网格y=0也在底部，所以直接使用
                    int index = pos.y * MiningManager.LAYER_WIDTH + pos.x;
                    pixels[index] = Color.white; // R=1表示在攻击范围内
                }
            }
        }
        
        // 应用像素数据到纹理
        _attackRangeTexture.SetPixels(pixels);
        _attackRangeTexture.Apply(false); // 不更新mipmap
    }
    
    /// <summary>
    /// 获取当前攻击范围
    /// </summary>
    private HashSet<Vector2Int> GetCurrentAttackRange()
    {
        if (_drillManager == null)
        {
            return new HashSet<Vector2Int>();
        }
        
        DrillData drill = _drillManager.GetCurrentDrill();
        if (drill == null)
        {
            return new HashSet<Vector2Int>();
        }
        
        // 使用造型系统获取攻击范围
        if (drill.UsesShapeSystem())
        {
            DrillAttackCalculator calculator = DrillAttackCalculator.Instance;
            if (calculator != null)
            {
                return calculator.GetAttackRange();
            }
        }
        
        // 向后兼容：使用旧的矩形范围计算
        return GetAttackRangeLegacy(drill);
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
    /// 比较两个HashSet是否相等
    /// </summary>
    private bool AreHashSetsEqual(HashSet<Vector2Int> set1, HashSet<Vector2Int> set2)
    {
        if (set1 == null && set2 == null)
        {
            return true;
        }
        
        if (set1 == null || set2 == null)
        {
            return false;
        }
        
        if (set1.Count != set2.Count)
        {
            return false;
        }
        
        foreach (var item in set1)
        {
            if (!set2.Contains(item))
            {
                return false;
            }
        }
        
        return true;
    }
}
