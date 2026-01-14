using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 本地化数据结构
/// </summary>
[Serializable]
public class LocalizationData
{
    public string languageCode;              // 语言代码（如：zh-CN, en-US）
    public List<LocalizationEntry> entries;  // 本地化条目列表

    public LocalizationData()
    {
        entries = new List<LocalizationEntry>();
    }
}

/// <summary>
/// 本地化条目
/// </summary>
[Serializable]
public class LocalizationEntry
{
    public string key;    // 文本键
    public string value;  // 文本值

    public LocalizationEntry()
    {
    }

    public LocalizationEntry(string key, string value)
    {
        this.key = key;
        this.value = value;
    }
}
