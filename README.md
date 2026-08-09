# FieldAchievements - 特殊地图成就追踪

一个 Dalamud 插件，用于追踪特殊场景探索相关成就的进度。

## 功能

- 追踪四个特殊地图的探索成就进度：
  - 蜃景幻界新月岛 南征之章
  - 蜃景幻界新月岛 北征之章
  - 南方博兹雅战线
  - 扎杜诺尔高原
- 主窗口分 Tab 显示各个地图的成就追踪
- 悬浮窗显示当前地图的成就进度
- 相同成就的不同数量目标只追踪最高数量
- 像素风进度条显示
- 成就进度自动刷新

## 命令

- `/fach` - 打开主窗口
- `/fach config` - 打开设置窗口

## 项目结构

```
FieldAchievements/
├── Data/
│   └── MapData.cs                    —— 地图 TerritoryType、成就 ID/名称/描述等静态数据
├── Models/
│   └── AchievementProgressInfo.cs    —— 成就进度数据模型（Current/Max/Ratio/IsComplete）
├── Windows/
│   ├── MainWindow.cs                 —— 主窗口（Tab 分页显示各地图成就 + 设置 Tab）
│   ├── FloatingWindow.cs             —— 悬浮窗（当前地图成就进度，可折叠，跟随地图自动开闭）
│   ├── ConfigWindow.cs               —— 独立设置窗口
│   └── Style.cs                      —— 窗口样式常量（像素风配色 / 进度条配色 / Push-Pop 计数）
├── AchievementTracker.cs             —— 成就进度 Hook（ReceiveAchievementProgress + 主动请求）
├── Plugin.cs                         —— Dalamud 插件生命周期、Framework 轮询刷新、悬浮窗自动开闭
├── Configuration.cs                  —— 插件配置
└── FieldAchievements.json            —— Dalamud 插件清单
```

## 构建

1. 打开 `FieldAchievements.sln`
2. 构建解决方案（Debug 或 Release）
3. 将生成的 DLL 添加到 Dalamud 的 Dev Plugin Locations

## 技术参考

- 主 UI 设计参考 [Chronicler](https://github.com/anmili2022/Chronicler) 的 Tab 布局和悬浮窗模式
- 成就进度获取和进度条绘制参考 [TreasureForecast](https://github.com/yimo0908/TreasureForecast) 的 `AchievementTracker` 和像素风进度条
- 地图 TerritoryType、成就 ID 和成就名称通过 EXDViewer MCP 从游戏数据查询获得

## 许可证

AGPL-3.0-or-later
