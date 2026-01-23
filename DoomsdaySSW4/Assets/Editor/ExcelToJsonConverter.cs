using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;

/// <summary>
/// Excel配置表转JSON转换器
/// 支持格式：配置表名_中文备注字段.xlsx 或 .csv
/// </summary>
public class ExcelToJsonConverter : EditorWindow
{
    private string excelConfigsPath = "Assets/ExcelConfigs";
    private string outputPath = "Assets/Resources/Configs";
    private Vector2 scrollPosition;
    private List<string> logMessages = new List<string>();

    [MenuItem("Tools/配置表/Excel转JSON")]
    public static void ShowWindow()
    {
        ExcelToJsonConverter window = GetWindow<ExcelToJsonConverter>("Excel转JSON转换器");
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Excel配置表转JSON工具", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("配置路径：", EditorStyles.boldLabel);
        excelConfigsPath = EditorGUILayout.TextField("Excel目录：", excelConfigsPath);
        outputPath = EditorGUILayout.TextField("JSON输出目录：", outputPath);

        EditorGUILayout.Space();

        if (GUILayout.Button("转换所有Excel文件", GUILayout.Height(30)))
        {
            ConvertAllExcelToJson();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("说明：", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "1. Excel文件命名格式：配置表名_中文备注字段.xlsx 或 .csv\n" +
            "2. 支持的文件：TaskConfigs, OreConfigs, OreSpawnConfigs, DrillConfigs, DrillShapeConfigs, DrillBitConfigs, ShipConfigs, EnergyUpgradeConfigs, EnergyThresholds\n" +
            "3. Excel第一行必须是表头（字段名）\n" +
            "4. 字段名必须与C#类属性名完全匹配（区分大小写）\n" +
            "5. 如果Excel文件不可用，可以导出为CSV格式（UTF-8编码）\n" +
            "6. 包含逗号的字段值建议用引号括起来（如energyThresholds: \"50,100,150\"）",
            MessageType.Info);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("日志：", EditorStyles.boldLabel);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
        foreach (var log in logMessages)
        {
            EditorGUILayout.LabelField(log, EditorStyles.wordWrappedLabel);
        }
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("清空日志"))
        {
            logMessages.Clear();
        }
    }

    /// <summary>
    /// 转换所有Excel文件
    /// </summary>
    public void ConvertAllExcelToJson()
    {
        logMessages.Clear();
        AddLog("开始转换...");

        string fullExcelPath = Path.Combine(Application.dataPath, excelConfigsPath.Replace("Assets/", ""));
        string fullOutputPath = Path.Combine(Application.dataPath, outputPath.Replace("Assets/", ""));

        if (!Directory.Exists(fullExcelPath))
        {
            AddLog($"错误：Excel目录不存在 - {fullExcelPath}");
            EditorUtility.DisplayDialog("错误", $"Excel目录不存在：{fullExcelPath}", "确定");
            return;
        }

        if (!Directory.Exists(fullOutputPath))
        {
            Directory.CreateDirectory(fullOutputPath);
            AddLog($"创建输出目录：{fullOutputPath}");
        }

        // 查找所有Excel和CSV文件
        string[] excelFiles = Directory.GetFiles(fullExcelPath, "*.xlsx", SearchOption.TopDirectoryOnly);
        string[] csvFiles = Directory.GetFiles(fullExcelPath, "*.csv", SearchOption.TopDirectoryOnly);
        string[] allFiles = excelFiles.Concat(csvFiles).ToArray();

        if (allFiles.Length == 0)
        {
            AddLog("未找到Excel或CSV文件");
            EditorUtility.DisplayDialog("提示", "未找到Excel或CSV文件", "确定");
            return;
        }

        int successCount = 0;
        int failCount = 0;

        foreach (string filePath in allFiles)
        {
            try
            {
                string fileName = Path.GetFileName(filePath);
                AddLog($"处理文件：{fileName}");

                string configName = ExtractConfigName(fileName);
                if (string.IsNullOrEmpty(configName))
                {
                    AddLog($"  跳过：无法识别配置表名 - {fileName}");
                    failCount++;
                    continue;
                }

                AddLog($"  识别为：{configName}");

                // 读取文件
                List<Dictionary<string, string>> data;
                if (filePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                {
                    data = ReadCsvFile(filePath);
                }
                else
                {
                    AddLog($"  错误：暂不支持直接读取.xlsx文件，请导出为CSV格式");
                    failCount++;
                    continue;
                }

                if (data == null || data.Count == 0)
                {
                    AddLog($"  错误：文件为空或格式错误");
                    failCount++;
                    continue;
                }

                // 转换并保存
                string outputFile = Path.Combine(fullOutputPath, $"{configName}.json");
                bool success = ConvertDataToJson(configName, data, outputFile);

                if (success)
                {
                    AddLog($"  ✓ 成功：{outputFile}");
                    successCount++;
                }
                else
                {
                    AddLog($"  ✗ 失败：转换错误");
                    failCount++;
                }
            }
            catch (Exception e)
            {
                AddLog($"  错误：{e.Message}");
                failCount++;
            }
        }

        AddLog($"\n转换完成：成功 {successCount} 个，失败 {failCount} 个");

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成", $"转换完成：成功 {successCount} 个，失败 {failCount} 个", "确定");
    }

    /// <summary>
    /// 从文件名提取配置表名
    /// </summary>
    private string ExtractConfigName(string fileName)
    {
        // 移除扩展名
        string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);

        // 查找下划线分隔符
        int underscoreIndex = nameWithoutExt.IndexOf('_');
        if (underscoreIndex > 0)
        {
            return nameWithoutExt.Substring(0, underscoreIndex);
        }

        // 如果没有下划线，返回整个文件名（去除扩展名）
        return nameWithoutExt;
    }

    /// <summary>
    /// 读取CSV文件
    /// </summary>
    private List<Dictionary<string, string>> ReadCsvFile(string filePath)
    {
        List<Dictionary<string, string>> result = new List<Dictionary<string, string>>();
        List<string> headers = new List<string>();

        try
        {
            // 尝试多种编码
            string[] lines = null;
            Encoding[] encodings = { Encoding.UTF8, Encoding.GetEncoding("GB2312"), Encoding.Default };

            foreach (var encoding in encodings)
            {
                try
                {
                    lines = File.ReadAllLines(filePath, encoding);
                    break;
                }
                catch { }
            }

            if (lines == null || lines.Length == 0)
            {
                AddLog("  错误：无法读取CSV文件");
                return null;
            }

            // 读取表头
            if (lines.Length > 0)
            {
                headers = ParseCsvLine(lines[0]);
                if (headers.Count == 0)
                {
                    AddLog("  错误：表头为空");
                    return null;
                }
            }

            // 读取数据行
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                List<string> values = ParseCsvLine(lines[i]);
                
                // 特殊处理：如果列数多于表头，可能是energyThresholds字段包含逗号
                // 将多余的列合并到最后一个字段
                if (values.Count > headers.Count && headers.Count > 0)
                {
                    string lastHeader = headers[headers.Count - 1];
                    // 将多余的列合并到最后一个字段（用逗号连接）
                    List<string> extraValues = new List<string>();
                    for (int j = headers.Count - 1; j < values.Count; j++)
                    {
                        if (!string.IsNullOrEmpty(values[j]))
                        {
                            extraValues.Add(values[j]);
                        }
                    }
                    if (extraValues.Count > 0)
                    {
                        // 如果最后一个字段已经有值，用逗号连接；否则直接使用合并的值
                        if (values.Count >= headers.Count && !string.IsNullOrEmpty(values[headers.Count - 1]))
                        {
                            values[headers.Count - 1] = values[headers.Count - 1] + "," + string.Join(",", extraValues);
                        }
                        else
                        {
                            values[headers.Count - 1] = string.Join(",", extraValues);
                        }
                        // 移除多余的列
                        values = values.Take(headers.Count).ToList();
                    }
                }
                
                if (values.Count != headers.Count)
                {
                    AddLog($"  警告：第 {i + 1} 行列数不匹配（表头：{headers.Count}，数据：{values.Count}）");
                    continue;
                }

                Dictionary<string, string> row = new Dictionary<string, string>();
                for (int j = 0; j < headers.Count; j++)
                {
                    row[headers[j]] = values[j];
                }
                result.Add(row);
            }

            return result;
        }
        catch (Exception e)
        {
            AddLog($"  错误：读取CSV文件失败 - {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// 解析CSV行（处理引号和逗号）
    /// </summary>
    private List<string> ParseCsvLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        StringBuilder currentField = new StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    // 转义的引号
                    currentField.Append('"');
                    i++;
                }
                else
                {
                    // 切换引号状态
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                // 字段分隔符
                result.Add(currentField.ToString().Trim());
                currentField.Clear();
            }
            else
            {
                currentField.Append(c);
            }
        }

        // 添加最后一个字段
        result.Add(currentField.ToString().Trim());

        return result;
    }

    /// <summary>
    /// 转换数据为JSON
    /// </summary>
    private bool ConvertDataToJson(string configName, List<Dictionary<string, string>> data, string outputPath)
    {
        try
        {
            switch (configName)
            {
                case "TaskConfigs":
                    return ConvertTaskConfigs(data, outputPath);
                case "OreConfigs":
                    return ConvertOreConfigs(data, outputPath);
                case "OreSpawnConfigs":
                    return ConvertOreSpawnConfigs(data, outputPath);
                case "ShipConfigs":
                    return ConvertShipConfigs(data, outputPath);
                case "ShipInitialDrillConfigs":
                    return ConvertShipInitialDrillConfigs(data, outputPath);
                case "EnergyUpgradeConfigs":
                    return ConvertEnergyUpgradeConfigs(data, outputPath);
                case "EnergyThresholds":
                    return ConvertEnergyThresholdConfigs(data, outputPath);
                case "DrillShapeConfigs":
                    return ConvertDrillShapeConfigs(data, outputPath);
                case "DrillBitConfigs":
                    return ConvertDrillBitConfigs(data, outputPath);
                default:
                    AddLog($"  错误：不支持的配置表类型 - {configName}");
                    return false;
            }
        }
        catch (Exception e)
        {
            AddLog($"  错误：转换失败 - {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// 转换TaskConfigs
    /// </summary>
    private bool ConvertTaskConfigs(List<Dictionary<string, string>> data, string outputPath)
    {
        List<TaskConfig> tasks = new List<TaskConfig>();

        foreach (var row in data)
        {
            try
            {
                TaskConfig task = new TaskConfig
                {
                    taskId = GetString(row, "taskId"),
                    taskName = GetString(row, "taskName"),
                    taskType = ParseEnum<TaskType>(GetString(row, "taskType")),
                    maxTurns = GetInt(row, "maxTurns"),
                    targetDebtAmount = GetInt(row, "targetDebtAmount"),
                    nextTaskId = GetString(row, "nextTaskId"),
                    description = GetString(row, "description")
                };
                tasks.Add(task);
            }
            catch (Exception e)
            {
                AddLog($"  警告：跳过无效行 - {e.Message}");
            }
        }

        TaskConfigCollection collection = new TaskConfigCollection { tasks = tasks };
        string json = JsonUtility.ToJson(collection, true);
        File.WriteAllText(outputPath, json, Encoding.UTF8);
        return true;
    }

    /// <summary>
    /// 转换OreConfigs
    /// </summary>
    private bool ConvertOreConfigs(List<Dictionary<string, string>> data, string outputPath)
    {
        List<OreConfig> ores = new List<OreConfig>();

        foreach (var row in data)
        {
            try
            {
                OreConfig ore = new OreConfig
                {
                    oreId = GetString(row, "oreId"),
                    oreName = GetString(row, "oreName"),
                    oreType = ParseEnum<OreType>(GetString(row, "oreType")),
                    hardness = GetInt(row, "hardness"),
                    requiredAttributeKey = GetString(row, "requiredAttributeKey"),
                    requiredAttributeValue = GetInt(row, "requiredAttributeValue"),
                    value = GetInt(row, "value"),
                    isEnergyOre = GetBool(row, "isEnergyOre"),
                    energyValue = GetInt(row, "energyValue"),
                    minDepth = GetInt(row, "minDepth"),
                    maxDepth = GetInt(row, "maxDepth"),
                    spawnProbability = GetFloat(row, "spawnProbability"),
                    spritePath = GetString(row, "spritePath"),
                    latticeSpritePath = GetString(row, "latticeSpritePath")
                };
                ores.Add(ore);
            }
            catch (Exception e)
            {
                AddLog($"  警告：跳过无效行 - {e.Message}");
            }
        }

        OreConfigCollection collection = new OreConfigCollection { ores = ores.ToArray() };
        string json = JsonUtility.ToJson(collection, true);
        File.WriteAllText(outputPath, json, Encoding.UTF8);
        return true;
    }

    /// <summary>
    /// 转换OreSpawnConfigs（需要按layerDepth分组）
    /// </summary>
    private bool ConvertOreSpawnConfigs(List<Dictionary<string, string>> data, string outputPath)
    {
        Dictionary<int, OreSpawnConfig> layerDict = new Dictionary<int, OreSpawnConfig>();

        foreach (var row in data)
        {
            try
            {
                int layerDepth = GetInt(row, "layerDepth");
                if (!layerDict.ContainsKey(layerDepth))
                {
                    layerDict[layerDepth] = new OreSpawnConfig
                    {
                        layerDepth = layerDepth,
                        spawnRules = new List<OreSpawnRule>()
                    };
                }

                OreSpawnRule rule = new OreSpawnRule
                {
                    oreId = GetString(row, "oreId"),
                    weight = GetInt(row, "weight"),
                    maxCount = GetInt(row, "maxCount"),
                    spawnProbability = GetFloat(row, "spawnProbability"),
                    @default = GetInt(row, "default")
                };

                layerDict[layerDepth].spawnRules.Add(rule);
            }
            catch (Exception e)
            {
                AddLog($"  警告：跳过无效行 - {e.Message}");
            }
        }

        List<OreSpawnConfig> layers = layerDict.Values.OrderBy(l => l.layerDepth).ToList();
        OreSpawnConfigCollection collection = new OreSpawnConfigCollection { layers = layers.ToArray() };
        string json = JsonUtility.ToJson(collection, true);
        File.WriteAllText(outputPath, json, Encoding.UTF8);
        return true;
    }

    /// <summary>
    /// 转换ShipConfigs
    /// </summary>
    private bool ConvertShipConfigs(List<Dictionary<string, string>> data, string outputPath)
    {
        List<ShipConfig> ships = new List<ShipConfig>();

        foreach (var row in data)
        {
            try
            {
                ShipConfig ship = new ShipConfig
                {
                    shipId = GetString(row, "shipId"),
                    shipName = GetString(row, "shipName"),
                    initialDebt = GetInt(row, "initialDebt"),
                    initialShapeIds = GetString(row, "initialShapeIds"),
                    description = GetString(row, "description")
                };
                ships.Add(ship);
            }
            catch (Exception e)
            {
                AddLog($"  警告：跳过无效行 - {e.Message}");
            }
        }

        ShipConfigCollection collection = new ShipConfigCollection { ships = ships.ToArray() };
        string json = JsonUtility.ToJson(collection, true);
        File.WriteAllText(outputPath, json, Encoding.UTF8);
        return true;
    }

    /// <summary>
    /// 转换ShipInitialDrillConfigs
    /// </summary>
    private bool ConvertShipInitialDrillConfigs(List<Dictionary<string, string>> data, string outputPath)
    {
        List<ShipInitialDrillConfig> configs = new List<ShipInitialDrillConfig>();

        foreach (var row in data)
        {
            try
            {
                ShipInitialDrillConfig config = new ShipInitialDrillConfig
                {
                    shipId = GetString(row, "shipId"),
                    shapeId = GetString(row, "shapeId"),
                    positionX = GetInt(row, "positionX"),
                    positionY = GetInt(row, "positionY"),
                    rotation = GetInt(row, "rotation")
                };
                configs.Add(config);
            }
            catch (Exception e)
            {
                AddLog($"  警告：跳过无效行 - {e.Message}");
            }
        }

        ShipInitialDrillConfigCollection collection = new ShipInitialDrillConfigCollection { configs = configs };
        string json = JsonUtility.ToJson(collection, true);
        File.WriteAllText(outputPath, json, Encoding.UTF8);
        return true;
    }

    /// <summary>
    /// 转换EnergyUpgradeConfigs
    /// </summary>
    private bool ConvertEnergyUpgradeConfigs(List<Dictionary<string, string>> data, string outputPath)
    {
        List<EnergyUpgradeConfig> upgrades = new List<EnergyUpgradeConfig>();

        foreach (var row in data)
        {
            try
            {
                EnergyUpgradeConfig upgrade = new EnergyUpgradeConfig
                {
                    upgradeId = GetString(row, "upgradeId"),
                    type = GetString(row, "type"),
                    name = GetString(row, "name"),
                    description = GetString(row, "description"),
                    value = GetInt(row, "value"),
                    weight = GetInt(row, "weight"),
                    iconPath = GetString(row, "iconPath")
                };
                upgrades.Add(upgrade);
            }
            catch (Exception e)
            {
                AddLog($"  警告：跳过无效行 - {e.Message}");
            }
        }

        EnergyUpgradeConfigCollection collection = new EnergyUpgradeConfigCollection { upgrades = upgrades.ToArray() };
        string json = JsonUtility.ToJson(collection, true);
        File.WriteAllText(outputPath, json, Encoding.UTF8);
        return true;
    }

    /// <summary>
    /// 转换EnergyThresholdConfigs
    /// </summary>
    private bool ConvertEnergyThresholdConfigs(List<Dictionary<string, string>> data, string outputPath)
    {
        List<EnergyThresholdConfig> thresholds = new List<EnergyThresholdConfig>();

        foreach (var row in data)
        {
            try
            {
                string shipId = GetString(row, "shipId");
                string thresholdsStr = GetString(row, "energyThresholds");

                // 解析逗号分隔的整数列表
                List<int> thresholdList = new List<int>();
                if (!string.IsNullOrEmpty(thresholdsStr))
                {
                    string[] parts = thresholdsStr.Split(',');
                    foreach (string part in parts)
                    {
                        string trimmed = part.Trim();
                        if (int.TryParse(trimmed, out int value))
                        {
                            thresholdList.Add(value);
                        }
                    }
                }

                EnergyThresholdConfig config = new EnergyThresholdConfig
                {
                    shipId = shipId,
                    energyThresholds = thresholdList.ToArray()
                };
                thresholds.Add(config);
            }
            catch (Exception e)
            {
                AddLog($"  警告：跳过无效行 - {e.Message}");
            }
        }

        EnergyThresholdConfigCollection collection = new EnergyThresholdConfigCollection { thresholds = thresholds.ToArray() };
        string json = JsonUtility.ToJson(collection, true);
        File.WriteAllText(outputPath, json, Encoding.UTF8);
        return true;
    }

    /// <summary>
    /// 转换DrillShapeConfigs
    /// </summary>
    private bool ConvertDrillShapeConfigs(List<Dictionary<string, string>> data, string outputPath)
    {
        List<DrillShapeConfigJson> shapes = new List<DrillShapeConfigJson>();

        foreach (var row in data)
        {
            try
            {
                DrillShapeConfigJson shape = new DrillShapeConfigJson
                {
                    shapeId = GetString(row, "shapeId"),
                    shapeName = GetString(row, "shapeName"),
                    baseAttackStrength = GetInt(row, "baseAttackStrength"),
                    description = GetString(row, "description")
                };

                // 解析cells字段（格式：x,y;x,y;x,y）
                string cellsStr = GetString(row, "cells");
                if (!string.IsNullOrEmpty(cellsStr))
                {
                    List<CellPositionJson> cells = new List<CellPositionJson>();
                    string[] cellPairs = cellsStr.Split(';');
                    foreach (string cellPair in cellPairs)
                    {
                        string trimmed = cellPair.Trim();
                        if (string.IsNullOrEmpty(trimmed))
                            continue;

                        string[] coords = trimmed.Split(',');
                        if (coords.Length >= 2)
                        {
                            if (int.TryParse(coords[0].Trim(), out int x) &&
                                int.TryParse(coords[1].Trim(), out int y))
                            {
                                cells.Add(new CellPositionJson(x, y));
                            }
                        }
                    }
                    shape.cells = cells;
                }
                else
                {
                    shape.cells = new List<CellPositionJson>();
                }

                // 解析traits字段（JSON字符串）
                string traitsStr = GetString(row, "traits");
                if (!string.IsNullOrEmpty(traitsStr))
                {
                    try
                    {
                        // 解析JSON字符串为ShapeTraitConfigJson数组
                        // JsonUtility需要包装类来解析数组
                        string wrappedJson = "{\"traits\":" + traitsStr + "}";
                        ShapeTraitConfigJsonArrayWrapper wrapper = JsonUtility.FromJson<ShapeTraitConfigJsonArrayWrapper>(wrappedJson);
                        if (wrapper != null && wrapper.traits != null)
                        {
                            shape.traits = new List<ShapeTraitConfigJson>(wrapper.traits);
                        }
                        else
                        {
                            shape.traits = new List<ShapeTraitConfigJson>();
                        }
                    }
                    catch (Exception e)
                    {
                        AddLog($"  警告：解析traits失败 - {e.Message}，跳过该造型的特性");
                        shape.traits = new List<ShapeTraitConfigJson>();
                    }
                }
                else
                {
                    shape.traits = new List<ShapeTraitConfigJson>();
                }

                shapes.Add(shape);
            }
            catch (Exception e)
            {
                AddLog($"  警告：跳过无效行 - {e.Message}");
            }
        }

        DrillShapeConfigCollectionJson collection = new DrillShapeConfigCollectionJson { shapes = shapes };
        string json = JsonUtility.ToJson(collection, true);
        File.WriteAllText(outputPath, json, Encoding.UTF8);
        return true;
    }

    /// <summary>
    /// 转换DrillBitConfigs
    /// </summary>
    private bool ConvertDrillBitConfigs(List<Dictionary<string, string>> data, string outputPath)
    {
        List<DrillBitConfigJson> bits = new List<DrillBitConfigJson>();

        foreach (var row in data)
        {
            try
            {
                DrillBitConfigJson bit = new DrillBitConfigJson
                {
                    bitId = GetString(row, "bitId"),
                    bitName = GetString(row, "bitName"),
                    description = GetString(row, "description"),
                    requiredSlotType = GetString(row, "requiredSlotType"),
                    strengthBonus = GetInt(row, "strengthBonus"),
                    strengthMultiplier = GetFloat(row, "strengthMultiplier"),
                    effectRange = GetInt(row, "effectRange"),
                    includeDiagonal = GetBool(row, "includeDiagonal"),
                    iconPath = GetString(row, "iconPath"),
                    effects = new List<DrillBitEffectJson>()
                };

                // 解析effects字段（JSON字符串）
                string effectsStr = GetString(row, "effects");
                if (!string.IsNullOrEmpty(effectsStr))
                {
                    try
                    {
                        // 解析JSON字符串为DrillBitEffectJson数组
                        // JsonUtility需要包装类来解析数组
                        string wrappedJson = "{\"effects\":" + effectsStr + "}";
                        DrillBitEffectJsonArrayWrapper wrapper = JsonUtility.FromJson<DrillBitEffectJsonArrayWrapper>(wrappedJson);
                        if (wrapper != null && wrapper.effects != null)
                        {
                            bit.effects = new List<DrillBitEffectJson>(wrapper.effects);
                        }
                    }
                    catch (Exception e)
                    {
                        AddLog($"  警告：解析effects失败 - {e.Message}，跳过该钻头的后效");
                        bit.effects = new List<DrillBitEffectJson>();
                    }
                }

                bits.Add(bit);
            }
            catch (Exception e)
            {
                AddLog($"  警告：跳过无效行 - {e.Message}");
            }
        }

        DrillBitConfigCollectionJson collection = new DrillBitConfigCollectionJson { bits = bits };
        string json = JsonUtility.ToJson(collection, true);
        File.WriteAllText(outputPath, json, Encoding.UTF8);
        return true;
    }

    // 辅助方法：从字典获取值
    private string GetString(Dictionary<string, string> row, string key)
    {
        return row.ContainsKey(key) ? row[key] : "";
    }

    private int GetInt(Dictionary<string, string> row, string key)
    {
        string value = GetString(row, key);
        if (int.TryParse(value, out int result))
            return result;
        return 0;
    }

    private float GetFloat(Dictionary<string, string> row, string key)
    {
        string value = GetString(row, key);
        if (float.TryParse(value, out float result))
            return result;
        return 0f;
    }

    private bool GetBool(Dictionary<string, string> row, string key)
    {
        string value = GetString(row, key).ToLower().Trim();
        return value == "true" || value == "1" || value == "yes" || value == "是";
    }

    private T ParseEnum<T>(string value) where T : struct, Enum
    {
        if (Enum.TryParse<T>(value, true, out T result))
            return result;
        return default(T);
    }

    private void AddLog(string message)
    {
        logMessages.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        Debug.Log($"[ExcelToJsonConverter] {message}");
    }
}

/// <summary>
/// Traits数组包装类（用于JsonUtility解析）
/// </summary>
[System.Serializable]
public class ShapeTraitConfigJsonArrayWrapper
{
    public ShapeTraitConfigJson[] traits;
}

/// <summary>
/// DrillBitEffect数组包装类（用于JsonUtility解析）
/// </summary>
[System.Serializable]
public class DrillBitEffectJsonArrayWrapper
{
    public DrillBitEffectJson[] effects;
}
