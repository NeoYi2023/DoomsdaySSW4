using UnityEngine;
using TMPro;
using System.IO;
using System.Text;

/// <summary>
/// 字体诊断工具：检查TextMeshPro字体配置和字符支持
/// </summary>
public class FontDiagnostics : MonoBehaviour
{
    [Header("诊断设置")]
    [SerializeField] private bool runOnStart = true;
    [SerializeField] private bool logToFile = true;
    
    private void Start()
    {
        if (runOnStart)
        {
            DiagnoseAllFonts();
        }
    }

    /// <summary>
    /// 诊断所有TextMeshPro字体
    /// </summary>
    [ContextMenu("诊断所有字体")]
    public void DiagnoseAllFonts()
    {
        StringBuilder log = new StringBuilder();
        log.AppendLine("=== TextMeshPro 字体诊断报告 ===");
        log.AppendLine($"诊断时间: {System.DateTime.Now}");
        log.AppendLine();

        // 检查所有TextMeshProUGUI组件
        TextMeshProUGUI[] allTexts = FindObjectsOfType<TextMeshProUGUI>(true);
        log.AppendLine($"找到 {allTexts.Length} 个 TextMeshProUGUI 组件");
        log.AppendLine();

        // 测试字符（常用中文字符）
        string testChars = "任务进度剩余回合债务金钱能源钻头一二三四五六七八九十";
        
        int fontIssueCount = 0;
        int missingCharCount = 0;

        foreach (TextMeshProUGUI text in allTexts)
        {
            if (text == null) continue;

            log.AppendLine($"--- {text.gameObject.name} ---");
            log.AppendLine($"文本内容: {text.text}");
            
            // 检查字体资源
            TMP_FontAsset font = text.font;
            if (font == null)
            {
                log.AppendLine("❌ 字体资源: NULL（未分配）");
                fontIssueCount++;
            }
            else
            {
                log.AppendLine($"✓ 字体资源: {font.name}");
                
                // 检查字符查找表
                if (font.characterLookupTable == null || font.characterLookupTable.Count == 0)
                {
                    log.AppendLine("❌ 字符查找表: NULL 或 空");
                    fontIssueCount++;
                }
                else
                {
                    log.AppendLine($"✓ 字符查找表: {font.characterLookupTable.Count} 个字符");
                    
                    // 检查测试字符是否在字体中
                    int foundCount = 0;
                    foreach (char c in testChars)
                    {
                        if (font.characterLookupTable.ContainsKey(c))
                        {
                            foundCount++;
                        }
                    }
                    
                    log.AppendLine($"测试字符支持: {foundCount}/{testChars.Length}");
                    
                    if (foundCount < testChars.Length)
                    {
                        log.AppendLine($"⚠️ 缺少 {testChars.Length - foundCount} 个中文字符");
                        missingCharCount++;
                        
                        // 列出缺失的字符
                        log.Append("缺失字符: ");
                        foreach (char c in testChars)
                        {
                            if (!font.characterLookupTable.ContainsKey(c))
                            {
                                log.Append($"{c} ");
                            }
                        }
                        log.AppendLine();
                    }
                }
            }
            
            log.AppendLine();
        }

        // 总结
        log.AppendLine("=== 诊断总结 ===");
        log.AppendLine($"总组件数: {allTexts.Length}");
        log.AppendLine($"字体问题数: {fontIssueCount}");
        log.AppendLine($"缺少字符的组件数: {missingCharCount}");
        
        if (fontIssueCount == 0 && missingCharCount == 0)
        {
            log.AppendLine("✓ 所有字体配置正常！");
        }
        else
        {
            log.AppendLine("⚠️ 发现字体配置问题，请检查上述详细信息");
        }

        // 输出到Console
        Debug.Log(log.ToString());

        // 输出到文件
        if (logToFile)
        {
            string logPath = Path.Combine(Application.persistentDataPath, "FontDiagnostics.log");
            File.WriteAllText(logPath, log.ToString(), Encoding.UTF8);
            Debug.Log($"诊断报告已保存到: {logPath}");
        }
    }

    /// <summary>
    /// 检查特定字符是否在字体中
    /// </summary>
    public static bool CheckCharacterInFont(TMP_FontAsset font, char character)
    {
        if (font == null) return false;
        if (font.characterLookupTable == null) return false;
        return font.characterLookupTable.ContainsKey(character);
    }

    /// <summary>
    /// 检查字符串中的所有字符是否都在字体中
    /// </summary>
    public static int CountSupportedCharacters(TMP_FontAsset font, string text)
    {
        if (font == null || font.characterLookupTable == null) return 0;
        
        int count = 0;
        foreach (char c in text)
        {
            if (font.characterLookupTable.ContainsKey(c))
            {
                count++;
            }
        }
        return count;
    }
}
