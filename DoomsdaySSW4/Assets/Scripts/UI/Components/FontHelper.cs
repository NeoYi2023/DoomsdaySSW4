using UnityEngine;
using TMPro;

/// <summary>
/// 字体辅助工具类：提供便捷的字体获取和应用方法
/// </summary>
public static class FontHelper
{
    /// <summary>
    /// 获取动态字体资源
    /// </summary>
    public static TMP_FontAsset GetDynamicFont()
    {
        DynamicChineseFontLoader loader = Object.FindObjectOfType<DynamicChineseFontLoader>();
        if (loader != null)
        {
            return loader.DynamicFont;
        }
        return null;
    }
    
    /// <summary>
    /// 为指定GameObject及其子对象应用动态字体
    /// </summary>
    public static void ApplyFontToGameObject(GameObject target)
    {
        if (target == null) return;
        
        DynamicChineseFontLoader loader = Object.FindObjectOfType<DynamicChineseFontLoader>();
        if (loader != null)
        {
            TextMeshProUGUI[] texts = target.GetComponentsInChildren<TextMeshProUGUI>(true);
            int count = 0;
            foreach (TextMeshProUGUI text in texts)
            {
                if (text != null)
                {
                    loader.ApplyFontToText(text);
                    count++;
                }
            }
            if (count > 0)
            {
                Debug.Log($"FontHelper: 已为 {count} 个文本组件应用动态字体");
            }
        }
        else
        {
            Debug.LogWarning("FontHelper: 未找到 DynamicChineseFontLoader");
        }
    }
    
    /// <summary>
    /// 为指定文本组件应用动态字体
    /// </summary>
    public static void ApplyFontToText(TextMeshProUGUI textComponent)
    {
        if (textComponent == null) return;
        
        DynamicChineseFontLoader loader = Object.FindObjectOfType<DynamicChineseFontLoader>();
        if (loader != null)
        {
            loader.ApplyFontToText(textComponent);
        }
        else
        {
            Debug.LogWarning("FontHelper: 未找到 DynamicChineseFontLoader，无法应用字体");
        }
    }
    
    /// <summary>
    /// 检查动态字体是否已加载
    /// </summary>
    public static bool IsDynamicFontAvailable()
    {
        DynamicChineseFontLoader loader = Object.FindObjectOfType<DynamicChineseFontLoader>();
        return loader != null && loader.DynamicFont != null;
    }
}
