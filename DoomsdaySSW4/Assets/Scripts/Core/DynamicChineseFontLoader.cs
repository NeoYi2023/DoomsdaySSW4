using UnityEngine;
using TMPro;
using UnityEngine.TextCore.LowLevel;
using System.Collections.Generic;

/// <summary>
/// 动态中文字体加载器：使用动态字体模式，按需生成字符，节省内存
/// </summary>
public class DynamicChineseFontLoader : MonoBehaviour
{
    [Header("字体配置")]
    [SerializeField] private string sourceFontPath = "Fonts/STXIHEI"; // Resources 路径，不含扩展名
    [SerializeField] private int samplingPointSize = 64; // 采样点大小（降低可节省内存）
    [SerializeField] private int padding = 9; // 字符间距
    [SerializeField] private int atlasWidth = 512; // 纹理图集宽度（降低可节省内存）
    [SerializeField] private int atlasHeight = 512; // 纹理图集高度
    [SerializeField] private GlyphRenderMode renderMode = GlyphRenderMode.SDFAA; // 渲染模式

    [Header("自动应用设置")]
    [SerializeField] private bool applyToAllTextsOnStart = true; // 启动时自动应用到所有文本
    [SerializeField] private bool setAsDefaultFont = true; // 设置为 TextMeshPro 默认字体

    private TMP_FontAsset _dynamicFont;
    private Font _sourceFont;

    /// <summary>
    /// 获取动态字体资源（如果未创建则自动创建）
    /// </summary>
    public TMP_FontAsset DynamicFont
    {
        get
        {
            if (_dynamicFont == null)
            {
                CreateDynamicFont();
            }
            return _dynamicFont;
        }
    }

    private void Awake()
    {
        // 确保单例
        DynamicChineseFontLoader[] loaders = FindObjectsOfType<DynamicChineseFontLoader>();
        if (loaders.Length > 1)
        {
            Debug.LogWarning("检测到多个 DynamicChineseFontLoader，保留第一个，销毁其他");
            for (int i = 1; i < loaders.Length; i++)
            {
                Destroy(loaders[i].gameObject);
            }
        }
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        CreateDynamicFont();

        if (applyToAllTextsOnStart)
        {
            ApplyFontToAllTexts();
        }

        if (setAsDefaultFont)
        {
            SetAsDefaultFont();
        }
    }

    /// <summary>
    /// 创建动态字体资源
    /// </summary>
    public void CreateDynamicFont()
    {
        if (_dynamicFont != null)
        {
            Debug.Log("动态字体已存在，跳过创建");
            return;
        }

        // 加载源字体文件
        _sourceFont = Resources.Load<Font>(sourceFontPath);
        if (_sourceFont == null)
        {
            Debug.LogError($"无法加载源字体文件: {sourceFontPath}，请确保字体文件已放在 Assets/Resources/{sourceFontPath}.ttf 或 .otf");
            return;
        }

        Debug.Log($"开始创建动态字体资源，源字体: {_sourceFont.name}");

        // 创建动态字体资源
        _dynamicFont = TMP_FontAsset.CreateFontAsset(
            _sourceFont,
            samplingPointSize,
            padding,
            renderMode,
            atlasWidth,
            atlasHeight,
            AtlasPopulationMode.Dynamic // 关键：使用动态模式
        );

        if (_dynamicFont == null)
        {
            Debug.LogError("创建动态字体资源失败");
            return;
        }

        // 设置字体名称
        _dynamicFont.name = $"{_sourceFont.name} Dynamic SDF";

        // 配置动态字体设置
        _dynamicFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        
        Debug.Log($"动态字体资源创建成功: {_dynamicFont.name}");
        Debug.Log($"初始内存占用: 约 {GetFontMemorySize() / 1024f / 1024f:F2} MB");
    }

    /// <summary>
    /// 应用到所有 TextMeshPro 文本组件
    /// </summary>
    public void ApplyFontToAllTexts()
    {
        if (_dynamicFont == null)
        {
            Debug.LogWarning("动态字体未创建，无法应用");
            return;
        }

        TextMeshProUGUI[] allTexts = FindObjectsOfType<TextMeshProUGUI>(true);
        int count = 0;

        foreach (TextMeshProUGUI text in allTexts)
        {
            if (text.font != _dynamicFont)
            {
                text.font = _dynamicFont;
                count++;
            }
        }

        Debug.Log($"已为 {count} 个 TextMeshProUGUI 组件设置动态字体");
    }

    /// <summary>
    /// 应用到指定文本组件
    /// </summary>
    public void ApplyFontToText(TextMeshProUGUI textComponent)
    {
        if (_dynamicFont == null)
        {
            Debug.LogWarning("动态字体未创建，无法应用");
            return;
        }

        if (textComponent != null)
        {
            textComponent.font = _dynamicFont;
        }
    }

    /// <summary>
    /// 设置为 TextMeshPro 默认字体
    /// 注意：TMP_Settings.defaultFontAsset 是只读属性，无法在运行时修改
    /// 此方法会记录日志，实际效果通过 ApplyFontToAllTexts() 实现
    /// </summary>
    public void SetAsDefaultFont()
    {
        if (_dynamicFont == null)
        {
            Debug.LogWarning("动态字体未创建，无法设置为默认字体");
            return;
        }

        // TMP_Settings.defaultFontAsset 是只读属性，无法在运行时修改
        // 但我们已经通过 ApplyFontToAllTexts() 将所有现有文本设置为使用动态字体
        // 新创建的文本组件如果没有指定字体，可以通过其他机制（如监听文本创建事件）来设置
        Debug.Log($"动态字体已应用到所有现有文本组件。注意：TMP_Settings.defaultFontAsset 是只读的，无法在运行时修改。");
    }

    /// <summary>
    /// 获取字体内存占用（估算）
    /// </summary>
    private long GetFontMemorySize()
    {
        if (_dynamicFont == null || _dynamicFont.atlasTexture == null)
            return 0;

        // 计算纹理图集大小
        Texture2D atlas = _dynamicFont.atlasTexture;
        long textureSize = atlas.width * atlas.height * 4; // RGBA32 = 4 bytes per pixel

        // 加上字体资源本身的大小（估算）
        return textureSize + (1024 * 100); // 基础资源约 100KB
    }

    /// <summary>
    /// 获取当前字体内存占用信息
    /// </summary>
    public string GetMemoryInfo()
    {
        if (_dynamicFont == null)
            return "字体未创建";

        long memoryBytes = GetFontMemorySize();
        float memoryMB = memoryBytes / 1024f / 1024f;
        int characterCount = _dynamicFont.characterLookupTable != null 
            ? _dynamicFont.characterLookupTable.Count 
            : 0;

        return $"字体内存占用: {memoryMB:F2} MB, 已生成字符数: {characterCount}";
    }

    /// <summary>
    /// 清理字体资源（如果需要）
    /// </summary>
    public void ClearFont()
    {
        if (_dynamicFont != null)
        {
            Destroy(_dynamicFont);
            _dynamicFont = null;
        }
    }

    private void OnDestroy()
    {
        // 清理资源
        if (_dynamicFont != null)
        {
            // 注意：不要在这里销毁，因为可能被其他对象引用
            // 让 Unity 自动管理生命周期
        }
    }
}
