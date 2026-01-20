# 矿石素材目录

此目录存放矿石格子的显示图片素材。

## 文件列表

| 文件名 | 矿石类型 | 建议颜色/风格 |
|--------|----------|---------------|
| ore_iron.png | 铁矿石 | 灰褐色金属质感 |
| ore_gold.png | 金矿石 | 金黄色闪光 |
| ore_diamond.png | 钻石 | 蓝白色透明晶体 |
| ore_crystal.png | 水晶 | 紫色/粉色透明 |
| ore_energy_core.png | 能源核心 | 绿色发光 |

## 素材规格

- **格式**: PNG（支持透明度）
- **尺寸**: 建议 64x64 或 128x128 像素
- **颜色模式**: RGBA

## 加载方式

素材通过 `Resources.Load<Sprite>()` 加载，路径配置在 `OreConfigs.csv` 的 `spritePath` 字段中。

示例：`spritePath = UI/Ores/ore_iron`（不含 .png 扩展名）
