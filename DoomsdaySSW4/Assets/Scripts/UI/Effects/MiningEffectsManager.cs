using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 挖矿特效管理器：协调所有挖矿视觉特效
/// 包括格子消失、矿石图标出现、变为金币、飞向金钱显示位置的完整动画序列
/// </summary>
public class MiningEffectsManager : MonoBehaviour
{
    private static MiningEffectsManager _instance;
    public static MiningEffectsManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("MiningEffectsManager");
                _instance = go.AddComponent<MiningEffectsManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    [Header("素材引用")]
    [SerializeField] private Sprite coinSprite;           // 金币图标
    [SerializeField] private RectTransform moneyUITarget; // 金钱显示位置
    
    [Header("动画参数")]
    [SerializeField] private float tileDisappearDelay = 0.2f;  // 晃动结束后延迟消失
    [SerializeField] private float oreIconDuration = 0.3f;     // 矿石图标显示时间
    [SerializeField] private float coinFlyDuration = 0.5f;     // 金币飞行时间
    [SerializeField] private float iconSize = 48f;             // 图标大小
    
    [Header("飞行曲线")]
    [SerializeField] private AnimationCurve flyCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    // 配置管理器引用
    private ConfigManager _configManager;
    
    // 矿石Sprite缓存
    private Dictionary<string, Sprite> _oreSpriteCache = new Dictionary<string, Sprite>();
    
    // 效果根节点（用于生成飞行图标）
    private Canvas _effectCanvas;
    private RectTransform _effectRoot;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _configManager = ConfigManager.Instance;
        
        // 尝试加载默认金币图标
        if (coinSprite == null)
        {
            coinSprite = Resources.Load<Sprite>("UI/Icons/coin");
        }
    }

    private void Start()
    {
        // 初始化效果Canvas
        InitializeEffectCanvas();
    }

    /// <summary>
    /// 初始化效果Canvas（用于显示飞行图标）
    /// </summary>
    private void InitializeEffectCanvas()
    {
        // 查找现有的效果Canvas或创建新的
        _effectCanvas = FindEffectCanvas();
        
        if (_effectCanvas == null)
        {
            // 创建效果Canvas
            GameObject canvasObj = new GameObject("EffectCanvas");
            _effectCanvas = canvasObj.AddComponent<Canvas>();
            _effectCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _effectCanvas.sortingOrder = 100; // 确保在最上层
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(canvasObj);
        }
        
        _effectRoot = _effectCanvas.GetComponent<RectTransform>();
    }
    
    /// <summary>
    /// 查找合适的效果Canvas
    /// </summary>
    private Canvas FindEffectCanvas()
    {
        // 首先查找名为EffectCanvas的Canvas
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        foreach (var canvas in canvases)
        {
            if (canvas.gameObject.name == "EffectCanvas")
            {
                return canvas;
            }
        }
        return null;
    }

    /// <summary>
    /// 设置金钱UI目标位置
    /// </summary>
    public void SetMoneyUITarget(RectTransform target)
    {
        moneyUITarget = target;
    }

    /// <summary>
    /// 播放完整的挖矿特效序列（矿石消失→矿石图标→金币→飞向金钱UI）
    /// </summary>
    /// <param name="minedTiles">被完全挖掉的格子信息</param>
    /// <param name="miningMapView">挖矿地图视图（用于获取格子位置）</param>
    public IEnumerator PlayMiningEffectSequence(List<AttackedTileInfo> minedTiles, MiningMapView miningMapView)
    {
        if (minedTiles == null || minedTiles.Count == 0)
        {
            yield break;
        }
        
        // 筛选出被完全挖掉的格子
        List<AttackedTileInfo> fullyMinedTiles = new List<AttackedTileInfo>();
        foreach (var tile in minedTiles)
        {
            if (tile.isFullyMined && tile.moneyValue > 0)
            {
                fullyMinedTiles.Add(tile);
            }
        }
        
        if (fullyMinedTiles.Count == 0)
        {
            yield break;
        }
        
        // 确保有效果Canvas
        if (_effectCanvas == null || _effectRoot == null)
        {
            InitializeEffectCanvas();
        }
        
        // 等待格子消失延迟
        yield return new WaitForSeconds(tileDisappearDelay);
        
        // 为每个被挖掉的格子播放特效
        List<Coroutine> effectCoroutines = new List<Coroutine>();
        foreach (var tileInfo in fullyMinedTiles)
        {
            // 获取格子的屏幕位置
            Vector2 tileScreenPos = GetTileScreenPosition(tileInfo.position, miningMapView);
            if (tileScreenPos != Vector2.zero)
            {
                Coroutine coroutine = StartCoroutine(PlaySingleTileEffect(tileInfo, tileScreenPos));
                effectCoroutines.Add(coroutine);
            }
        }
        
        // 等待所有特效完成
        foreach (var coroutine in effectCoroutines)
        {
            yield return coroutine;
        }
    }

    /// <summary>
    /// 播放单个格子的特效序列
    /// </summary>
    private IEnumerator PlaySingleTileEffect(AttackedTileInfo tileInfo, Vector2 startPosition)
    {
        // 创建矿石图标
        GameObject oreIcon = CreateIconObject(tileInfo.oreId, startPosition);
        if (oreIcon == null)
        {
            yield break;
        }
        
        Image iconImage = oreIcon.GetComponent<Image>();
        
        // 显示矿石图标一段时间
        yield return new WaitForSeconds(oreIconDuration);
        
        // 变为金币图标
        if (coinSprite != null && iconImage != null)
        {
            iconImage.sprite = coinSprite;
        }
        
        // 飞向金钱UI位置
        if (moneyUITarget != null)
        {
            Vector2 targetPosition = GetScreenPosition(moneyUITarget);
            yield return StartCoroutine(FlyToTarget(oreIcon, startPosition, targetPosition));
        }
        else
        {
            // 如果没有目标位置，简单淡出
            yield return StartCoroutine(FadeOutAndDestroy(oreIcon));
        }
        
        // 销毁图标
        if (oreIcon != null)
        {
            Destroy(oreIcon);
        }
    }

    /// <summary>
    /// 创建图标对象
    /// </summary>
    private GameObject CreateIconObject(string oreId, Vector2 screenPosition)
    {
        if (_effectRoot == null)
        {
            return null;
        }
        
        // 创建图标GameObject
        GameObject iconObj = new GameObject($"OreIcon_{oreId}");
        iconObj.transform.SetParent(_effectRoot, false);
        
        // 添加RectTransform
        RectTransform rectTransform = iconObj.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(iconSize, iconSize);
        rectTransform.position = screenPosition;
        
        // 添加Image组件
        Image image = iconObj.AddComponent<Image>();
        
        // 加载矿石图片
        Sprite oreSprite = LoadOreSprite(oreId);
        if (oreSprite != null)
        {
            image.sprite = oreSprite;
        }
        else
        {
            // 没有图片时使用颜色代替
            image.color = GetOreColor(oreId);
        }
        
        return iconObj;
    }

    /// <summary>
    /// 加载矿石Sprite（带缓存）
    /// </summary>
    private Sprite LoadOreSprite(string oreId)
    {
        if (string.IsNullOrEmpty(oreId))
        {
            return null;
        }
        
        // 获取配置
        OreConfig config = _configManager?.GetOreConfig(oreId);
        if (config == null || string.IsNullOrEmpty(config.spritePath))
        {
            return null;
        }
        
        // 检查缓存
        if (_oreSpriteCache.TryGetValue(config.spritePath, out Sprite cachedSprite))
        {
            return cachedSprite;
        }
        
        // 从Resources加载
        Sprite sprite = Resources.Load<Sprite>(config.spritePath);
        if (sprite != null)
        {
            _oreSpriteCache[config.spritePath] = sprite;
        }
        
        return sprite;
    }

    /// <summary>
    /// 获取矿石默认颜色（当没有图片时使用）
    /// </summary>
    private Color GetOreColor(string oreId)
    {
        switch (oreId)
        {
            case "iron": return new Color(0.5f, 0.4f, 0.35f, 1f);
            case "gold": return new Color(1f, 0.84f, 0f, 1f);
            case "diamond": return new Color(0.7f, 0.9f, 1f, 1f);
            case "crystal": return new Color(0.8f, 0.5f, 0.9f, 1f);
            case "energy_core": return new Color(0.2f, 1f, 0.4f, 1f);
            default: return Color.gray;
        }
    }

    /// <summary>
    /// 获取瓦片在屏幕上的位置
    /// </summary>
    private Vector2 GetTileScreenPosition(Vector2Int tilePosition, MiningMapView miningMapView)
    {
        if (miningMapView == null)
        {
            return Vector2.zero;
        }
        
        // 尝试从MiningMapView获取瓦片的GameObject
        // 通过反射或公共方法获取_tileMap
        // 简化实现：使用Transform.position
        Transform mapTransform = miningMapView.transform;
        if (mapTransform == null)
        {
            return Vector2.zero;
        }
        
        // 查找指定位置的瓦片
        string tileName = $"Tile_{tilePosition.x}_{tilePosition.y}";
        Transform tileTransform = mapTransform.Find(tileName);
        
        if (tileTransform == null)
        {
            // 尝试在GridLayout子对象中查找
            foreach (Transform child in mapTransform)
            {
                if (child.name == tileName)
                {
                    tileTransform = child;
                    break;
                }
            }
        }
        
        if (tileTransform != null)
        {
            return tileTransform.position;
        }
        
        return Vector2.zero;
    }

    /// <summary>
    /// 获取RectTransform在屏幕上的位置
    /// </summary>
    private Vector2 GetScreenPosition(RectTransform rectTransform)
    {
        if (rectTransform == null)
        {
            return Vector2.zero;
        }
        return rectTransform.position;
    }

    /// <summary>
    /// 飞向目标位置的协程
    /// </summary>
    private IEnumerator FlyToTarget(GameObject iconObj, Vector2 startPos, Vector2 targetPos)
    {
        if (iconObj == null)
        {
            yield break;
        }
        
        RectTransform rectTransform = iconObj.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            yield break;
        }
        
        float elapsed = 0f;
        while (elapsed < coinFlyDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / coinFlyDuration);
            float curveT = flyCurve.Evaluate(t);
            
            // 插值位置
            Vector2 currentPos = Vector2.Lerp(startPos, targetPos, curveT);
            
            // 添加抛物线效果
            float arcHeight = Mathf.Sin(t * Mathf.PI) * 50f; // 弧线高度
            currentPos.y += arcHeight;
            
            rectTransform.position = currentPos;
            
            // 缩放效果（接近目标时变小）
            float scale = Mathf.Lerp(1f, 0.5f, t);
            rectTransform.localScale = new Vector3(scale, scale, 1f);
            
            yield return null;
        }
        
        // 确保到达目标位置
        rectTransform.position = targetPos;
    }

    /// <summary>
    /// 淡出并销毁的协程
    /// </summary>
    private IEnumerator FadeOutAndDestroy(GameObject iconObj)
    {
        if (iconObj == null)
        {
            yield break;
        }
        
        Image image = iconObj.GetComponent<Image>();
        if (image == null)
        {
            yield break;
        }
        
        float fadeTime = 0.3f;
        float elapsed = 0f;
        Color startColor = image.color;
        
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeTime;
            
            Color currentColor = startColor;
            currentColor.a = Mathf.Lerp(1f, 0f, t);
            image.color = currentColor;
            
            yield return null;
        }
    }
}
