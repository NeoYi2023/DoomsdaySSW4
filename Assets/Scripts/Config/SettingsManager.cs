using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 设置管理器：负责分辨率设置、全屏模式等游戏设置的管理
/// </summary>
public class SettingsManager : MonoBehaviour
{
    private static SettingsManager _instance;
    public static SettingsManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("SettingsManager");
                _instance = go.AddComponent<SettingsManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // 预设分辨率列表
    private readonly List<Resolution> _presetResolutions = new List<Resolution>
    {
        new Resolution { width = 1920, height = 1080 }, // Full HD
        new Resolution { width = 1680, height = 1050 },
        new Resolution { width = 1600, height = 900 },
        new Resolution { width = 1440, height = 900 },
        new Resolution { width = 1366, height = 768 },
        new Resolution { width = 1280, height = 720 },  // HD
        new Resolution { width = 1024, height = 768 }
    };

    // 当前设置
    private int _currentWidth;
    private int _currentHeight;
    private bool _isFullscreen;
    private string _resolutionMode = "Preset";

    // PlayerPrefs键名
    private const string PREF_RESOLUTION_WIDTH = "ResolutionWidth";
    private const string PREF_RESOLUTION_HEIGHT = "ResolutionHeight";
    private const string PREF_IS_FULLSCREEN = "IsFullscreen";
    private const string PREF_RESOLUTION_MODE = "ResolutionMode";

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            LoadResolutionSettings();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 设置分辨率
    /// </summary>
    /// <param name="width">宽度</param>
    /// <param name="height">高度</param>
    /// <param name="fullscreen">是否全屏</param>
    public void SetResolution(int width, int height, bool fullscreen)
    {
        if (!IsValidResolution(width, height))
        {
            Debug.LogWarning($"无效的分辨率: {width}x{height}");
            return;
        }

        _currentWidth = width;
        _currentHeight = height;
        _isFullscreen = fullscreen;

        ApplyResolutionSettings();
        SaveResolutionSettings();
    }

    /// <summary>
    /// 获取可用分辨率列表（包括预设和系统支持的分辨率）
    /// </summary>
    public Resolution[] GetAvailableResolutions()
    {
        // 获取系统支持的分辨率
        Resolution[] systemResolutions = Screen.resolutions;

        // 合并预设分辨率和系统分辨率，去重
        HashSet<string> seen = new HashSet<string>();
        List<Resolution> allResolutions = new List<Resolution>();

        // 先添加预设分辨率
        foreach (var preset in _presetResolutions)
        {
            string key = $"{preset.width}x{preset.height}";
            if (!seen.Contains(key))
            {
                seen.Add(key);
                allResolutions.Add(preset);
            }
        }

        // 再添加系统分辨率（如果不在预设列表中）
        foreach (var res in systemResolutions)
        {
            string key = $"{res.width}x{res.height}";
            if (!seen.Contains(key))
            {
                seen.Add(key);
                allResolutions.Add(res);
            }
        }

        // 按宽度和高度排序
        return allResolutions.OrderByDescending(r => r.width)
                           .ThenByDescending(r => r.height)
                           .ToArray();
    }

    /// <summary>
    /// 应用分辨率设置
    /// </summary>
    public void ApplyResolutionSettings()
    {
        try
        {
            Screen.SetResolution(_currentWidth, _currentHeight, _isFullscreen);
            Debug.Log($"分辨率已设置为: {_currentWidth}x{_currentHeight}, 全屏: {_isFullscreen}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"应用分辨率设置失败: {e.Message}");
        }
    }

    /// <summary>
    /// 保存分辨率设置
    /// </summary>
    public void SaveResolutionSettings()
    {
        PlayerPrefs.SetInt(PREF_RESOLUTION_WIDTH, _currentWidth);
        PlayerPrefs.SetInt(PREF_RESOLUTION_HEIGHT, _currentHeight);
        PlayerPrefs.SetInt(PREF_IS_FULLSCREEN, _isFullscreen ? 1 : 0);
        PlayerPrefs.SetString(PREF_RESOLUTION_MODE, _resolutionMode);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 加载分辨率设置
    /// </summary>
    public void LoadResolutionSettings()
    {
        // 如果存在保存的设置，则加载
        if (PlayerPrefs.HasKey(PREF_RESOLUTION_WIDTH))
        {
            _currentWidth = PlayerPrefs.GetInt(PREF_RESOLUTION_WIDTH);
            _currentHeight = PlayerPrefs.GetInt(PREF_RESOLUTION_HEIGHT);
            _isFullscreen = PlayerPrefs.GetInt(PREF_IS_FULLSCREEN) == 1;
            _resolutionMode = PlayerPrefs.GetString(PREF_RESOLUTION_MODE, "Preset");

            // 验证加载的设置是否有效
            if (IsValidResolution(_currentWidth, _currentHeight))
            {
                ApplyResolutionSettings();
            }
            else
            {
                // 如果无效，使用默认设置
                SetDefaultResolution();
            }
        }
        else
        {
            // 如果没有保存的设置，使用默认设置
            SetDefaultResolution();
        }
    }

    /// <summary>
    /// 获取当前分辨率
    /// </summary>
    public Resolution GetCurrentResolution()
    {
        Resolution res = new Resolution
        {
            width = _currentWidth > 0 ? _currentWidth : Screen.width,
            height = _currentHeight > 0 ? _currentHeight : Screen.height
        };
        return res;
    }

    /// <summary>
    /// 验证分辨率是否有效
    /// </summary>
    public bool IsValidResolution(int width, int height)
    {
        if (width <= 0 || height <= 0)
            return false;

        // 检查是否在系统支持的分辨率列表中
        Resolution[] availableResolutions = Screen.resolutions;
        foreach (var res in availableResolutions)
        {
            if (res.width == width && res.height == height)
                return true;
        }

        // 也检查预设分辨率
        foreach (var preset in _presetResolutions)
        {
            if (preset.width == width && preset.height == height)
                return true;
        }

        return false;
    }

    /// <summary>
    /// 设置默认分辨率
    /// </summary>
    private void SetDefaultResolution()
    {
        // 使用当前屏幕分辨率或第一个可用分辨率
        if (Screen.resolutions.Length > 0)
        {
            Resolution defaultRes = Screen.resolutions[Screen.resolutions.Length - 1]; // 通常最后一个是最高的
            _currentWidth = defaultRes.width;
            _currentHeight = defaultRes.height;
        }
        else
        {
            _currentWidth = 1920;
            _currentHeight = 1080;
        }

        _isFullscreen = Screen.fullScreen;
        _resolutionMode = "Preset";
        ApplyResolutionSettings();
    }

    /// <summary>
    /// 切换全屏模式
    /// </summary>
    public void ToggleFullscreen()
    {
        _isFullscreen = !_isFullscreen;
        ApplyResolutionSettings();
        SaveResolutionSettings();
    }

    /// <summary>
    /// 设置全屏模式
    /// </summary>
    public void SetFullscreen(bool fullscreen)
    {
        _isFullscreen = fullscreen;
        ApplyResolutionSettings();
        SaveResolutionSettings();
    }

    /// <summary>
    /// 获取当前是否全屏
    /// </summary>
    public bool IsFullscreen()
    {
        return _isFullscreen;
    }

    /// <summary>
    /// 设置分辨率模式
    /// </summary>
    public void SetResolutionMode(string mode)
    {
        _resolutionMode = mode;
        SaveResolutionSettings();
    }

    /// <summary>
    /// 获取分辨率模式
    /// </summary>
    public string GetResolutionMode()
    {
        return _resolutionMode;
    }
}
