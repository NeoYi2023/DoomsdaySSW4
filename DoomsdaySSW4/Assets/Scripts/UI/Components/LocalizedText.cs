using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 本地化文本组件：自动根据当前语言更新文本内容
/// 支持Unity UI Text和TextMeshPro组件
/// </summary>
public class LocalizedText : MonoBehaviour
{
    [SerializeField]
    [Tooltip("本地化文本键（如：ui.menu.start）")]
    private string _localizationKey;

    private Text _textComponent;
    private TextMeshProUGUI _tmpComponent;

    private void Awake()
    {
        _textComponent = GetComponent<Text>();
        _tmpComponent = GetComponent<TextMeshProUGUI>();

        if (_textComponent == null && _tmpComponent == null)
        {
            Debug.LogWarning($"LocalizedText组件需要Text或TextMeshProUGUI组件: {gameObject.name}");
        }
    }

    private void Start()
    {
        // 应用动态字体（如果是TextMeshPro组件）
        if (_tmpComponent != null)
        {
            FontHelper.ApplyFontToText(_tmpComponent);
        }
        
        UpdateText();
        
        // 订阅语言变更事件
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged.AddListener(OnLanguageChanged);
        }
    }

    private void OnDestroy()
    {
        // 取消订阅
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged.RemoveListener(OnLanguageChanged);
        }
    }

    /// <summary>
    /// 设置本地化键
    /// </summary>
    public void SetKey(string key)
    {
        _localizationKey = key;
        UpdateText();
    }

    /// <summary>
    /// 获取本地化键
    /// </summary>
    public string GetKey()
    {
        return _localizationKey;
    }

    /// <summary>
    /// 更新文本
    /// </summary>
    private void UpdateText()
    {
        if (string.IsNullOrEmpty(_localizationKey))
            return;

        if (LocalizationManager.Instance == null)
            return;

        string localizedText = LocalizationManager.Instance.GetLocalizedString(_localizationKey);

        if (_textComponent != null)
        {
            _textComponent.text = localizedText;
        }

        if (_tmpComponent != null)
        {
            _tmpComponent.text = localizedText;
        }
    }

    /// <summary>
    /// 语言变更回调
    /// </summary>
    private void OnLanguageChanged(string languageCode)
    {
        UpdateText();
    }
}
