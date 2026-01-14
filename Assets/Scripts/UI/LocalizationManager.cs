using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 本地化管理器：负责多语言文本的管理和切换
/// </summary>
public class LocalizationManager : MonoBehaviour
{
    private static LocalizationManager _instance;
    public static LocalizationManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("LocalizationManager");
                _instance = go.AddComponent<LocalizationManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // 当前语言代码
    private string _currentLanguage = "zh-CN";

    // 语言资源字典（键值对映射）
    private Dictionary<string, string> _localizedStrings = new Dictionary<string, string>();

    // 支持的语言列表
    private readonly List<string> _supportedLanguages = new List<string>
    {
        "zh-CN",  // 简体中文
        "zh-TW",  // 繁体中文
        "en-US"   // 英文
    };

    // 语言变更事件
    public UnityEvent<string> OnLanguageChanged = new UnityEvent<string>();

    // PlayerPrefs键名
    private const string PREF_LANGUAGE = "GameLanguage";

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSavedLanguage();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 设置当前语言
    /// </summary>
    /// <param name="languageCode">语言代码（如：zh-CN, en-US）</param>
    public void SetLanguage(string languageCode)
    {
        if (!_supportedLanguages.Contains(languageCode))
        {
            Debug.LogWarning($"不支持的语言代码: {languageCode}");
            return;
        }

        _currentLanguage = languageCode;
        LoadLanguageResource(languageCode);
        SaveLanguage();
        OnLanguageChanged?.Invoke(languageCode);
    }

    /// <summary>
    /// 获取当前语言
    /// </summary>
    public string GetCurrentLanguage()
    {
        return _currentLanguage;
    }

    /// <summary>
    /// 获取本地化文本
    /// </summary>
    /// <param name="key">文本键</param>
    /// <returns>本地化文本，如果不存在则返回键本身</returns>
    public string GetLocalizedString(string key)
    {
        if (string.IsNullOrEmpty(key))
            return "";

        if (_localizedStrings.TryGetValue(key, out string value))
        {
            return value;
        }

        // 如果找不到，返回键本身（用于调试）
        Debug.LogWarning($"未找到本地化文本键: {key}");
        return key;
    }

    /// <summary>
    /// 获取本地化文本（带参数格式化）
    /// </summary>
    /// <param name="key">文本键</param>
    /// <param name="args">格式化参数</param>
    /// <returns>格式化后的本地化文本</returns>
    public string GetLocalizedString(string key, params object[] args)
    {
        string text = GetLocalizedString(key);
        try
        {
            return string.Format(text, args);
        }
        catch (FormatException e)
        {
            Debug.LogError($"格式化本地化文本失败: {key}, 错误: {e.Message}");
            return text;
        }
    }

    /// <summary>
    /// 加载语言资源
    /// </summary>
    /// <param name="languageCode">语言代码</param>
    public void LoadLanguageResource(string languageCode)
    {
        _localizedStrings.Clear();

        string resourcePath = $"Localization/{languageCode}";
        TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);

        if (textAsset == null)
        {
            Debug.LogError($"无法加载语言资源: {resourcePath}");
            return;
        }

        try
        {
            LocalizationData data = JsonUtility.FromJson<LocalizationData>(textAsset.text);
            
            if (data == null || data.entries == null)
            {
                Debug.LogError($"语言资源数据格式错误: {resourcePath}");
                return;
            }

            foreach (var entry in data.entries)
            {
                if (!string.IsNullOrEmpty(entry.key) && !string.IsNullOrEmpty(entry.value))
                {
                    _localizedStrings[entry.key] = entry.value;
                }
            }

            Debug.Log($"成功加载语言资源: {languageCode}, 共 {_localizedStrings.Count} 条文本");
        }
        catch (Exception e)
        {
            Debug.LogError($"解析语言资源失败: {resourcePath}, 错误: {e.Message}");
        }
    }

    /// <summary>
    /// 获取支持的语言列表
    /// </summary>
    public List<string> GetSupportedLanguages()
    {
        return new List<string>(_supportedLanguages);
    }

    /// <summary>
    /// 检查语言资源是否存在
    /// </summary>
    public bool HasLanguageResource(string languageCode)
    {
        string resourcePath = $"Localization/{languageCode}";
        TextAsset textAsset = Resources.Load<TextAsset>(resourcePath);
        return textAsset != null;
    }

    /// <summary>
    /// 加载保存的语言设置
    /// </summary>
    private void LoadSavedLanguage()
    {
        if (PlayerPrefs.HasKey(PREF_LANGUAGE))
        {
            string savedLanguage = PlayerPrefs.GetString(PREF_LANGUAGE);
            if (_supportedLanguages.Contains(savedLanguage))
            {
                SetLanguage(savedLanguage);
                return;
            }
        }

        // 如果没有保存的语言或语言无效，使用系统语言或默认语言
        SetLanguage(GetSystemLanguage());
    }

    /// <summary>
    /// 保存语言设置
    /// </summary>
    private void SaveLanguage()
    {
        PlayerPrefs.SetString(PREF_LANGUAGE, _currentLanguage);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 根据系统语言获取对应的语言代码
    /// </summary>
    private string GetSystemLanguage()
    {
        SystemLanguage systemLang = Application.systemLanguage;

        switch (systemLang)
        {
            case SystemLanguage.Chinese:
            case SystemLanguage.ChineseSimplified:
                return "zh-CN";
            case SystemLanguage.ChineseTraditional:
                return "zh-TW";
            case SystemLanguage.English:
                return "en-US";
            default:
                return "zh-CN"; // 默认使用简体中文
        }
    }

    /// <summary>
    /// 检查是否包含指定的文本键
    /// </summary>
    public bool HasKey(string key)
    {
        return _localizedStrings.ContainsKey(key);
    }
}
