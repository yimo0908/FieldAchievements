# FieldAchievements——特殊地图成就追踪

FieldAchievements 是一款基于 Dalamud 框架的《最终幻想14》插件，用于追踪特殊地图相关成就的进度。插件覆盖蜃景幻界新月岛（南征之章、北征之章）、南方博兹雅战线与扎杜诺尔高原四处特殊地图，当前版本为 0.1.2.0。

## 主要功能

**追踪范围。** 插件对四处特殊地图的成就进度实施追踪：

- 蜃景幻界新月岛 南征之章
- 蜃景幻界新月岛 北征之章
- 南方博兹雅战线
- 扎杜诺尔高原

**成就覆盖全面。** 追踪范围涵盖调查笔记等记录类成就、危命任务、紧急遭遇战、强敌单挑、副本挑战、宝箱收集与辅助职业养成等类别。

**跨地图同步追踪。** 同一成就可同时归属多处地图，例如"辅助职业之王"兼属新月岛南征、北征之章，"博兹雅的彗星"兼属南方博兹雅战线与扎杜诺尔高原。插件对跨地图成就统一维护进度，确保各标签页口径一致。

**副本成就追踪。** 支持两歧塔（力之塔、魔之塔、超魔之塔）、帝国湖岸堡、女王古殿（含零式）与旗舰达尔里阿达号等副本的挑战次数类成就。

**实时进度获取。** 插件通过挂钩（Hook）游戏成就进度回调、辅以帧循环轮询，自动刷新成就进度。对依赖场景状态累计的成就，如"辅助职业之王""博兹雅的彗星"，插件在对应地图内以状态效果（buff）层数换算实时进度。

**像素风进度条。** 以像素字符绘制进度条，按完成度分段着色；已完成成就以高亮色标识。

**悬浮窗展示。** 悬浮窗随所在地图自动开闭，支持折叠、位置锁定与透明度调节；右键悬浮窗可直接打开主界面。

**显示定制。** 可分别控制主窗口与悬浮窗中已完成成就、成就描述的显示，并调节进度条宽度。

## 命令

- `/fach`——打开主窗口
- `/fach config`——打开设置窗口
- `/fach cfg`——打开设置窗口（`config` 的别名）

## 项目结构

```
FieldAchievements/
├── Data/
│   └── MapData.cs                    地图静态数据：TerritoryType、成就 ID、跨地图映射与 buff 进度规则
├── Models/
│   └── AchievementProgressInfo.cs    成就进度数据模型（当前值、上限、进度比、完成状态）
├── Windows/
│   ├── MainWindow.cs                 主窗口：标签页分地图展示成就
│   ├── FloatingWindow.cs             悬浮窗：当前地图成就进度，可折叠、锁定、调透明度
│   ├── ConfigWindow.cs               设置窗口
│   └── Style.cs                      界面样式常量（像素风配色、进度条配色）
├── AchievementTracker.cs             成就进度挂钩与主动请求
├── Plugin.cs                         插件生命周期、轮询刷新、buff 进度换算与悬浮窗开闭
├── Configuration.cs                  插件配置
└── FieldAchievements.json            Dalamud 插件清单
```

## 构建

1. 打开 `FieldAchievements.sln`。
2. 构建解决方案（Debug 或 Release）。构建环境需安装 .NET 10 SDK。
3. 将生成的 DLL 添加至 Dalamud 的 Dev Plugin Locations。

## 技术参考

- 主界面标签页布局与悬浮窗模式，参考 [Chronicler](https://github.com/anmili2022/Chronicler)。
- 成就进度获取与像素风进度条绘制，参考 [TreasureForecast](https://github.com/yimo0908/TreasureForecast)。
- 地图 TerritoryType 与成就 ID 经 EXDViewer MCP 从游戏数据查询核实；成就名称与描述运行时自 Lumina 游戏数据获取。

## 许可证

本插件以 AGPL-3.0-or-later 许可证发布。
