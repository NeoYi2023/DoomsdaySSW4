using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 设置界面：管理分辨率设置和语言设置的UI
/// </summary>
public class SettingsScreen : MonoBehaviour
{
    [Header("分辨率设置")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown resolutionModeDropdown;
    [SerializeField] private TMP_InputField widthInputField;
    [SerializeField] private TMP_InputField heightInputField;
    [SerializeField] private Toggle fullscreenToggle;

    [Header("语言设置")]
    [SerializeField] private TMP_Dropdown languageDropdown;

    [Header("按钮")]
    [SerializeField] private Button applyButton;
    [SerializeField] private Button cancelButton;

    private SettingsManager _settingsManager;
    private LocalizationManager _localizationManager;

    // 临时设置（应用前不生效）
    private int _tempWidth;
    private int _tempHeight;
    private bool _tempFullscreen;
    private string _tempLanguage;

    private void Awake()
    {
        _settingsManager = SettingsManager.Instance;
        _localizationManager = LocalizationManager.Instance;
    }

    private void Start()
    {
        InitializeUI();
        LoadCurrentSettings();
        SetupEventListeners();
        
        // 应用动态字体
        ApplyDynamicFont();
    }

    /// <summary>
    /// 初始化UI
    /// </summary>
    private void InitializeUI()
    {
        // 初始化分辨率下拉菜单
        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();
            Resolution[] resolutions = _settingsManager.GetAvailableResolutions();
            List<string> options = new List<string>();

            foreach (var res in resolutions)
            {
                options.Add($"{res.width} x {res.height}");
            }

            resolutionDropdown.AddOptions(options);
        }

        // 初始化分辨率模式下拉菜单
        if (resolutionModeDropdown != null)
        {
            resolutionModeDropdown.ClearOptions();
            List<string> modeOptions = new List<string>
            {
                _localizationManager.GetLocalizedString("ui.settings.resolution.preset"),
                _localizationManager.GetLocalizedString("ui.settings.resolution.custom")
            };
            resolutionModeDropdown.AddOptions(modeOptions);
        }

        // 初始化语言下拉菜单
        if (languageDropdown != null)
        {
            languageDropdown.ClearOptions();
            List<string> languages = _localizationManager.GetSupportedLanguages();
            List<string> languageOptions = new List<string>();

            foreach (var lang in languages)
            {
                string displayName = _localizationManager.GetLocalizedString($"ui.settings.language.{lang}");
                languageOptions.Add(displayName);
            }

            languageDropdown.AddOptions(languageOptions);
        }
    }

    /// <summary>
    /// 加载当前设置
    /// </summary>
    private void LoadCurrentSettings()
    {
        Resolution currentRes = _settingsManager.GetCurrentResolution();
        _tempWidth = currentRes.width;
        _tempHeight = currentRes.height;
        _tempFullscreen = _settingsManager.IsFullscreen();
        _tempLanguage = _localizationManager.GetCurrentLanguage();

        // 更新UI显示
        UpdateResolutionUI();
        UpdateLanguageUI();
    }

    /// <summary>
    /// 更新分辨率UI显示
    /// </summary>
    private void UpdateResolutionUI()
    {
        if (widthInputField != null)
            widthInputField.text = _tempWidth.ToString();

        if (heightInputField != null)
            heightInputField.text = _tempHeight.ToString();

        if (fullscreenToggle != null)
            fullscreenToggle.isOn = _tempFullscreen;

        // 更新分辨率下拉菜单选中项
        if (resolutionDropdown != null)
        {
            Resolution[] resolutions = _settingsManager.GetAvailableResolutions();
            for (int i = 0; i < resolutions.Length; i++)
            {
                if (resolutions[i].width == _tempWidth && resolutions[i].height == _tempHeight)
                {
                    resolutionDropdown.value = i;
                    break;
                }
            }
        }
    }

    /// <summary>
    /// 更新语言UI显示
    /// </summary>
    private void UpdateLanguageUI()
    {
        if (languageDropdown != null)
        {
            List<string> languages = _localizationManager.GetSupportedLanguages();
            int index = languages.IndexOf(_tempLanguage);
            if (index >= 0)
            {
                languageDropdown.value = index;
            }
        }
    }

    /// <summary>
    /// 设置事件监听器
    /// </summary>
    private void SetupEventListeners()
    {
        // 分辨率下拉菜单
        if (resolutionDropdown != null)
        {
            resolutionDropdown.onValueChanged.AddListener(OnResolutionDropdownChanged);
        }

        // 分辨率模式下拉菜单
        if (resolutionModeDropdown != null)
        {
            resolutionModeDropdown.onValueChanged.AddListener(OnResolutionModeChanged);
        }

        // 宽度输入框
        if (widthInputField != null)
        {
            widthInputField.onEndEdit.AddListener(OnWidthInputChanged);
        }

        // 高度输入框
        if (heightInputField != null)
        {
            heightInputField.onEndEdit.AddListener(OnHeightInputChanged);
        }

        // 全屏切换
        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggleChanged);
        }

        // 语言下拉菜单
        if (languageDropdown != null)
        {
            languageDropdown.onValueChanged.AddListener(OnLanguageDropdownChanged);
        }

        // 应用按钮
        if (applyButton != null)
        {
            applyButton.onClick.AddListener(OnApplyButtonClicked);
        }

        // 取消按钮
        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancelButtonClicked);
        }
    }

    /// <summary>
    /// 分辨率下拉菜单变更
    /// </summary>
    private void OnResolutionDropdownChanged(int index)
    {
        if (resolutionModeDropdown != null && resolutionModeDropdown.value == 0) // 预设模式
        {
            Resolution[] resolutions = _settingsManager.GetAvailableResolutions();
            if (index >= 0 && index < resolutions.Length)
            {
                _tempWidth = resolutions[index].width;
                _tempHeight = resolutions[index].height;
                UpdateResolutionUI();
            }
        }
    }

    /// <summary>
    /// 分辨率模式变更
    /// </summary>
    private void OnResolutionModeChanged(int mode)
    {
        bool isPreset = (mode == 0);
        
        if (resolutionDropdown != null)
            resolutionDropdown.interactable = isPreset;

        if (widthInputField != null)
            widthInputField.interactable = !isPreset;

        if (heightInputField != null)
            heightInputField.interactable = !isPreset;
    }

    /// <summary>
    /// 宽度输入变更
    /// </summary>
    private void OnWidthInputChanged(string value)
    {
        if (int.TryParse(value, out int width))
        {
            _tempWidth = width;
        }
    }

    /// <summary>
    /// 高度输入变更
    /// </summary>
    private void OnHeightInputChanged(string value)
    {
        if (int.TryParse(value, out int height))
        {
            _tempHeight = height;
        }
    }

    /// <summary>
    /// 全屏切换变更
    /// </summary>
    private void OnFullscreenToggleChanged(bool value)
    {
        _tempFullscreen = value;
    }

    /// <summary>
    /// 语言下拉菜单变更
    /// </summary>
    private void OnLanguageDropdownChanged(int index)
    {
        List<string> languages = _localizationManager.GetSupportedLanguages();
        if (index >= 0 && index < languages.Count)
        {
            _tempLanguage = languages[index];
        }
    }

    /// <summary>
    /// 应用按钮点击
    /// </summary>
    private void OnApplyButtonClicked()
    {
        // 验证分辨率
        if (!_settingsManager.IsValidResolution(_tempWidth, _tempHeight))
        {
            Debug.LogWarning($"无效的分辨率: {_tempWidth}x{_tempHeight}");
            // 这里可以显示错误提示给用户
            return;
        }

        // 应用分辨率设置
        _settingsManager.SetResolution(_tempWidth, _tempHeight, _tempFullscreen);

        // 应用语言设置
        _localizationManager.SetLanguage(_tempLanguage);

        Debug.Log("设置已应用");
    }

    /// <summary>
    /// 取消按钮点击
    /// </summary>
    private void OnCancelButtonClicked()
    {
        // 恢复原始设置
        LoadCurrentSettings();
    }

    /// <summary>
    /// 应用动态字体到所有文本组件
    /// </summary>
    private void ApplyDynamicFont()
    {
        FontHelper.ApplyFontToGameObject(gameObject);
    }
}
