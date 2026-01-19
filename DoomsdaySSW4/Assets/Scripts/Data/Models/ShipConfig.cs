using System;
using System.Collections.Generic;

/// <summary>
/// 船只配置数据结构（从JSON配置表加载）
/// </summary>
[Serializable]
public class ShipConfig
{
    public string shipId;
    public string shipName;
    public int initialDebt;                    // 初始债务
    public string initialShapeIds;             // 初始可用造型ID列表（分号分隔）
    public string description;
    
    /// <summary>
    /// 获取初始造型ID列表
    /// </summary>
    public List<string> GetInitialShapeIdList()
    {
        List<string> result = new List<string>();
        if (string.IsNullOrEmpty(initialShapeIds))
            return result;
            
        string[] ids = initialShapeIds.Split(';');
        foreach (string id in ids)
        {
            string trimmed = id.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                result.Add(trimmed);
            }
        }
        return result;
    }
}

/// <summary>
/// 船只配置集合，便于从JSON反序列化
/// </summary>
[Serializable]
public class ShipConfigCollection
{
    public ShipConfig[] ships;
}
