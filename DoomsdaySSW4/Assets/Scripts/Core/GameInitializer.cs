using UnityEngine;

/// <summary>
/// 游戏初始化器：在游戏启动时加载和应用设置
/// </summary>
public class GameInitializer : MonoBehaviour
{
    [Header("初始化设置")]
    [SerializeField] private bool initializeOnStart = true;
    
    [Header("字体加载")]
    [SerializeField] private bool initializeFontOnStart = true;

    private DynamicChineseFontLoader _fontLoader;

    private void Start()
    {
        if (initializeOnStart)
        {
            InitializeGame();
        }
    }

    /// <summary>
    /// 初始化游戏
    /// </summary>
    public void InitializeGame()
    {
        // 初始化设置管理器（会自动加载保存的设置）
        SettingsManager settingsManager = SettingsManager.Instance;
        
        // 初始化本地化管理器（会自动加载保存的语言设置）
        LocalizationManager localizationManager = LocalizationManager.Instance;

        Debug.Log("游戏设置已初始化");
        Debug.Log($"当前分辨率: {Screen.width}x{Screen.height}, 全屏: {Screen.fullScreen}");
        Debug.Log($"当前语言: {localizationManager.GetCurrentLanguage()}");

        // 初始化动态中文字体（优先初始化，确保字体在 UI 组件之前创建）
        if (initializeFontOnStart)
        {
            InitializeDynamicFont();
        }

        // 初始化游戏管理器并开始新游戏
        GameManager gameManager = GameManager.Instance;
        if (gameManager != null && !gameManager.IsGameInitialized())
        {
            gameManager.StartNewGame();
        }
    }

    /// <summary>
    /// 初始化动态中文字体
    /// </summary>
    private void InitializeDynamicFont()
    {
        // 检查是否已存在字体加载器
        _fontLoader = FindObjectOfType<DynamicChineseFontLoader>();
        
        if (_fontLoader == null)
        {
            // 创建字体加载器 GameObject
            GameObject fontLoaderGO = new GameObject("DynamicChineseFontLoader");
            _fontLoader = fontLoaderGO.AddComponent<DynamicChineseFontLoader>();
            
            // 立即创建字体，不等待 Start()，确保字体在 UI 组件 Start() 之前创建
            _fontLoader.CreateDynamicFont();
            
            Debug.Log("动态中文字体加载器已创建并初始化");
        }
        else
        {
            // 如果已存在，确保字体已创建
            if (_fontLoader.DynamicFont == null)
            {
                _fontLoader.CreateDynamicFont();
            }
            Debug.Log("动态中文字体加载器已存在");
        }
    }

    /// <summary>
    /// 手动初始化（可在其他脚本中调用）
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeOnLoad()
    {
        // 确保在场景加载前初始化设置管理器
        SettingsManager settingsManager = SettingsManager.Instance;
        LocalizationManager localizationManager = LocalizationManager.Instance;
    }
}
