using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// 矿石素材生成器：用于生成临时占位素材
/// 在 Unity 编辑器中选择 Tools > 生成矿石临时素材
/// </summary>
public class OreAssetGenerator : EditorWindow
{
    [MenuItem("Tools/生成矿石临时素材")]
    public static void GenerateOreAssets()
    {
        // 矿石配置：ID, 颜色
        var oreConfigs = new (string id, Color color)[]
        {
            ("ore_iron", new Color(0.5f, 0.4f, 0.35f, 1f)),       // 铁矿石 - 灰褐色
            ("ore_gold", new Color(1f, 0.84f, 0f, 1f)),           // 金矿石 - 金黄色
            ("ore_diamond", new Color(0.7f, 0.9f, 1f, 1f)),       // 钻石 - 蓝白色
            ("ore_crystal", new Color(0.8f, 0.5f, 0.9f, 1f)),     // 水晶 - 紫色
            ("ore_energy_core", new Color(0.2f, 1f, 0.4f, 1f)),   // 能源核心 - 绿色
        };

        // 素材放在 Resources 目录下以支持 Resources.Load()
        string oresPath = "Assets/Resources/UI/Ores";
        string iconsPath = "Assets/Resources/UI/Icons";

        // 确保目录存在
        if (!Directory.Exists(oresPath))
        {
            Directory.CreateDirectory(oresPath);
        }
        if (!Directory.Exists(iconsPath))
        {
            Directory.CreateDirectory(iconsPath);
        }

        // 生成矿石素材
        foreach (var config in oreConfigs)
        {
            GenerateOrePlaceholder(oresPath, config.id, config.color);
        }

        // 生成金币图标
        GenerateCoinPlaceholder(iconsPath);

        AssetDatabase.Refresh();
        Debug.Log("矿石临时素材生成完成！");
    }

    private static void GenerateOrePlaceholder(string basePath, string id, Color baseColor)
    {
        int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);

        // 创建简单的矿石图案（带边框和渐变）
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Color pixelColor;
                
                // 边框（2像素）
                if (x < 2 || x >= size - 2 || y < 2 || y >= size - 2)
                {
                    pixelColor = baseColor * 0.6f;
                    pixelColor.a = 1f;
                }
                // 角落圆角效果
                else if (IsCorner(x, y, size, 6))
                {
                    pixelColor = Color.clear;
                }
                else
                {
                    // 内部渐变效果（模拟3D感）
                    float gradient = 1f - (Mathf.Abs(x - size / 2f) + Mathf.Abs(y - size / 2f)) / size * 0.5f;
                    pixelColor = Color.Lerp(baseColor * 0.7f, baseColor * 1.2f, gradient);
                    pixelColor.a = 1f;
                    
                    // 添加一些噪点纹理
                    float noise = Mathf.PerlinNoise(x * 0.2f, y * 0.2f) * 0.2f - 0.1f;
                    pixelColor.r = Mathf.Clamp01(pixelColor.r + noise);
                    pixelColor.g = Mathf.Clamp01(pixelColor.g + noise);
                    pixelColor.b = Mathf.Clamp01(pixelColor.b + noise);
                }

                texture.SetPixel(x, y, pixelColor);
            }
        }

        texture.Apply();

        // 保存为PNG
        byte[] pngData = texture.EncodeToPNG();
        string filePath = Path.Combine(basePath, $"{id}.png");
        File.WriteAllBytes(filePath, pngData);

        // 清理
        Object.DestroyImmediate(texture);

        Debug.Log($"生成矿石素材: {filePath}");
    }

    private static void GenerateCoinPlaceholder(string basePath)
    {
        int size = 48;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);

        Color goldColor = new Color(1f, 0.84f, 0f, 1f);
        Color darkGold = new Color(0.8f, 0.6f, 0f, 1f);

        // 创建圆形金币
        float centerX = size / 2f;
        float centerY = size / 2f;
        float radius = size / 2f - 2;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Mathf.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
                
                if (dist <= radius)
                {
                    // 内圆
                    if (dist <= radius - 3)
                    {
                        // 渐变效果
                        float gradient = 1f - dist / radius * 0.3f;
                        Color pixelColor = Color.Lerp(darkGold, goldColor, gradient);
                        
                        // 高光
                        if (x < centerX - 5 && y > centerY + 5)
                        {
                            pixelColor = Color.Lerp(pixelColor, Color.white, 0.3f);
                        }
                        
                        texture.SetPixel(x, y, pixelColor);
                    }
                    else
                    {
                        // 边缘
                        texture.SetPixel(x, y, darkGold);
                    }
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }

        // 添加 $ 符号（简化版）
        DrawDollarSign(texture, size);

        texture.Apply();

        // 保存为PNG
        byte[] pngData = texture.EncodeToPNG();
        string filePath = Path.Combine(basePath, "coin.png");
        File.WriteAllBytes(filePath, pngData);

        // 清理
        Object.DestroyImmediate(texture);

        Debug.Log($"生成金币素材: {filePath}");
    }

    private static void DrawDollarSign(Texture2D texture, int size)
    {
        Color darkGold = new Color(0.6f, 0.45f, 0f, 1f);
        int centerX = size / 2;
        int centerY = size / 2;

        // 简化的 $ 符号（用几条线表示）
        // 垂直线
        for (int y = centerY - 8; y <= centerY + 8; y++)
        {
            texture.SetPixel(centerX, y, darkGold);
        }
        
        // 上半部分S
        for (int x = centerX - 4; x <= centerX + 4; x++)
        {
            texture.SetPixel(x, centerY + 6, darkGold);
            texture.SetPixel(x, centerY, darkGold);
        }
        texture.SetPixel(centerX - 4, centerY + 5, darkGold);
        texture.SetPixel(centerX - 4, centerY + 4, darkGold);
        texture.SetPixel(centerX - 4, centerY + 3, darkGold);
        texture.SetPixel(centerX + 4, centerY + 1, darkGold);
        texture.SetPixel(centerX + 4, centerY + 2, darkGold);
        
        // 下半部分S
        texture.SetPixel(centerX + 4, centerY - 1, darkGold);
        texture.SetPixel(centerX + 4, centerY - 2, darkGold);
        texture.SetPixel(centerX + 4, centerY - 3, darkGold);
        texture.SetPixel(centerX - 4, centerY - 4, darkGold);
        texture.SetPixel(centerX - 4, centerY - 5, darkGold);
        for (int x = centerX - 4; x <= centerX + 4; x++)
        {
            texture.SetPixel(x, centerY - 6, darkGold);
        }
    }

    private static bool IsCorner(int x, int y, int size, int cornerSize)
    {
        // 左上角
        if (x < cornerSize && y >= size - cornerSize)
        {
            int dx = cornerSize - x;
            int dy = y - (size - cornerSize);
            return dx * dx + dy * dy > cornerSize * cornerSize;
        }
        // 右上角
        if (x >= size - cornerSize && y >= size - cornerSize)
        {
            int dx = x - (size - cornerSize);
            int dy = y - (size - cornerSize);
            return dx * dx + dy * dy > cornerSize * cornerSize;
        }
        // 左下角
        if (x < cornerSize && y < cornerSize)
        {
            int dx = cornerSize - x;
            int dy = cornerSize - y;
            return dx * dx + dy * dy > cornerSize * cornerSize;
        }
        // 右下角
        if (x >= size - cornerSize && y < cornerSize)
        {
            int dx = x - (size - cornerSize);
            int dy = cornerSize - y;
            return dx * dx + dy * dy > cornerSize * cornerSize;
        }
        return false;
    }
}
