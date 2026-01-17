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

        // 初始化动态中文字体
        if (initializeFontOnStart)
        {
            InitializeDynamicFont();
        }
    }

    /// <summary>
    /// 初始化动态中文字体
    /// </summary>
    private void InitializeDynamicFont()
    {
        // 创建字体加载器 GameObject（如果不存在）
        if (_fontLoader == null)
        {
            GameObject fontLoaderGO = new GameObject("DynamicChineseFontLoader");
            _fontLoader = fontLoaderGO.AddComponent<DynamicChineseFontLoader>();
        }

        // 字体加载器会在 Start() 中自动创建和应用字体
        Debug.Log("动态中文字体加载器已初始化");
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
