using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using FieldAchievements.Data;
using FieldAchievements.Models;
using FieldAchievements.Windows;
using Dalamud.Game.ClientState.Statuses;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace FieldAchievements;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IChatGui Chat { get; private set; } = null!;
    [PluginService] internal static IGameInteropProvider GameInteropProvider { get; private set; } = null!;
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;

    private const string CommandName = "/fach";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("FieldAchievements");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    private FloatingWindow FloatingWindow { get; init; }

    internal AchievementTracker AchievementTracker { get; }
    internal List<AchievementProgressInfo> Achievements { get; }

    private int achRetryCounter;
    private int nextUninitializedIdx;
    private int uninitializedCount;
    private readonly Queue<int> pendingRefresh = new();
    private readonly Dictionary<uint, List<int>> achIndicesByID = new();

    // 首次加载时需主动向成就系统请求进度的 buff 追踪成就
    private readonly uint[] buffProgressRequestQueue = MapData.BuffProgressRules.Keys.ToArray();
    private int buffProgressRequestIdx;

    private const int AchInitRetryInterval = 30;
    private const int AchRefreshDefaultInterval = 300;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        var achievementSheet = DataManager.GetExcelSheet<Achievement>();

        Achievements = MapData.MapAchievements
            .SelectMany(kv => kv.Value.Select(id => new AchievementProgressInfo
            {
                AchievementID = id,
                AchievementName = ResolveAchievementName(achievementSheet, id),
                Description = ResolveAchievementDescription(achievementSheet, id),
                Map = kv.Key,
            }))
            .ToList();

        for (var i = 0; i < Achievements.Count; i++)
        {
            var id = Achievements[i].AchievementID;
            if (!achIndicesByID.TryGetValue(id, out var list))
            {
                list = new List<int>();
                achIndicesByID[id] = list;
            }
            list.Add(i);
        }
        // buff 追踪成就：上限已知，初始进度视为未知（未确认完成）
        foreach (var entry in Achievements)
        {
            if (MapData.BuffProgressRules.TryGetValue(entry.AchievementID, out var rule))
            {
                entry.Max = rule.Max;
                entry.IsProgressUnknown = true;
            }
        }
        uninitializedCount = Achievements.Count(a => a.Max == 0);

        AchievementTracker = new AchievementTracker(GameInteropProvider);
        AchievementTracker.OnAchievementProgress += OnAchievementProgress;

        MainWindow = new MainWindow(this);
        ConfigWindow = new ConfigWindow(this);
        FloatingWindow = new FloatingWindow(this);

        WindowSystem.AddWindow(MainWindow);
        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(FloatingWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "打开特殊地图成就追踪主窗口"
        });

        PluginInterface.UiBuilder.Draw += OnUiBuilderDraw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;

        Framework.Update += OnFrameworkUpdate;

        Log.Information($"=== {PluginInterface.Manifest.Name} 已加载 ===");
    }

    /// <summary>
    /// 按成就 ID 从 Lumina 游戏数据获取成就名称；获取失败时回退为成就编号。
    /// </summary>
    private static string ResolveAchievementName(ExcelSheet<Achievement>? sheet, uint id)
    {
        var name = sheet?.GetRowOrDefault(id)?.Name.ExtractText();
        return string.IsNullOrWhiteSpace(name) ? $"成就 #{id}" : name;
    }

    /// <summary>
    /// 按成就 ID 从 Lumina 游戏数据获取成就描述；获取失败时返回空字符串。
    /// </summary>
    private static string ResolveAchievementDescription(ExcelSheet<Achievement>? sheet, uint id)
        => sheet?.GetRowOrDefault(id)?.Description.ExtractText() ?? string.Empty;

    public void Dispose()
    {
        Framework.Update -= OnFrameworkUpdate;

        AchievementTracker.OnAchievementProgress -= OnAchievementProgress;
        AchievementTracker.Dispose();

        PluginInterface.UiBuilder.Draw -= OnUiBuilderDraw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;

        WindowSystem.RemoveAllWindows();

        CommandManager.RemoveHandler(CommandName);
    }

    private void OnAchievementProgress(uint id, uint current, uint max)
    {
        if (!achIndicesByID.TryGetValue(id, out var indices))
            return;

        // buff 追踪成就：进度由 buff 层数或未知状态显示，回调仅用于确认完成
        if (MapData.BuffProgressRules.ContainsKey(id))
        {
            if (max > 0 && current >= max)
            {
                foreach (var idx in indices)
                {
                    var entry = Achievements[idx];
                    entry.Current = entry.Max;
                    entry.IsProgressUnknown = false;
                }
            }
            return;
        }

        foreach (var idx in indices)
        {
            var entry = Achievements[idx];
            if (entry.Max == 0 && max > 0)
                uninitializedCount--;
            entry.Current = current;
            entry.Max = max;
        }
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        UpdateBuffProgressRequest();
        UpdateBuffBasedProgress();

        if (pendingRefresh.Count > 0)
        {
            var idx = pendingRefresh.Dequeue();
            AchievementTracker.Request(Achievements[idx].AchievementID);
            return;
        }

        achRetryCounter++;

        var refreshInterval = Math.Max(60, Configuration.RefreshIntervalSeconds * 60);
        var allInit = uninitializedCount == 0;

        if (allInit)
        {
            if (achRetryCounter >= refreshInterval)
            {
                achRetryCounter = 0;
                for (var i = 0; i < Achievements.Count; i++)
                {
                    // buff 追踪成就由场景状态实时驱动，无需向成就系统请求
                    if (MapData.BuffProgressRules.ContainsKey(Achievements[i].AchievementID))
                        continue;
                    pendingRefresh.Enqueue(i);
                }
                var idx = pendingRefresh.Dequeue();
                AchievementTracker.Request(Achievements[idx].AchievementID);
            }
        }
        else
        {
            if (achRetryCounter >= AchInitRetryInterval)
            {
                achRetryCounter = 0;
                for (var i = 0; i < Achievements.Count; i++)
                {
                    var idx = (nextUninitializedIdx + i) % Achievements.Count;
                    if (Achievements[idx].Max == 0)
                    {
                        AchievementTracker.Request(Achievements[idx].AchievementID);
                        nextUninitializedIdx = (idx + 1) % Achievements.Count;
                        return;
                    }
                }
            }
        }
    }

    /// <summary>
    /// 获取当前所在地图（如果被追踪的话）。
    /// </summary>
    public TrackedMap? GetCurrentMap()
        => MapData.ResolveMap(ClientState.TerritoryType);

    /// <summary>
    /// 首次加载插件时主动向成就系统请求 buff 追踪成就的进度，用于确认完成状态。
    /// 玩家数据未就绪或请求在途时逐帧重试，直至全部请求完成。
    /// </summary>
    private void UpdateBuffProgressRequest()
    {
        if (buffProgressRequestIdx >= buffProgressRequestQueue.Length)
            return;

        var id = buffProgressRequestQueue[buffProgressRequestIdx];
        if (AchievementTracker.Request(id))
            buffProgressRequestIdx++;
    }

    /// <summary>
    /// 更新基于 buff 层数的成就进度。
    /// 玩家位于成就适用地图内时，以 buff 层数换算实时进度；
    /// 位于地图外时，已确认完成的成就保持完成态，未完成的成就显示为未知进度。
    /// </summary>
    private void UpdateBuffBasedProgress()
    {
        var player = ObjectTable.LocalPlayer;
        if (player == null)
            return;

        var currentMap = GetCurrentMap();

        foreach (var (achId, rule) in MapData.BuffProgressRules)
        {
            if (!achIndicesByID.TryGetValue(achId, out var indices))
                continue;

            var inApplicableMap = currentMap.HasValue &&
                                  Array.IndexOf(rule.ApplicableMaps, currentMap.Value) >= 0;

            uint? buffCurrent = null;
            if (inApplicableMap)
            {
                var buffSum = 0u;
                foreach (var status in player.StatusList)
                {
                    foreach (var buffId in rule.BuffIds)
                    {
                        if (status.StatusId == buffId)
                        {
                            buffSum += status.Param;
                            break;
                        }
                    }
                }
                buffCurrent = Math.Min(buffSum, rule.Max);
            }

            foreach (var idx in indices)
            {
                var entry = Achievements[idx];
                if (entry.Max == 0)
                    uninitializedCount--;
                entry.Max = rule.Max;

                if (buffCurrent.HasValue)
                {
                    // 适用地图内：以 buff 层数显示；已确认完成则保持完成态
                    if (!entry.IsComplete)
                        entry.Current = buffCurrent.Value;
                    entry.IsProgressUnknown = false;
                }
                else if (entry.IsComplete)
                {
                    // 适用地图外且已完成：保持完成态显示
                    entry.IsProgressUnknown = false;
                }
                else
                {
                    // 适用地图外且未完成：进度未知
                    entry.IsProgressUnknown = true;
                }
            }
        }
    }

    private void OnUiBuilderDraw()
    {
        FloatingWindow.IsOpen = FloatingWindow.ShouldBeOpen;
        WindowSystem.Draw();
    }

    private void OnCommand(string command, string args)
    {
        if (string.IsNullOrWhiteSpace(args))
        {
            ToggleMainUi();
            return;
        }
        var arg = args.Trim().ToLowerInvariant();
        if (arg == "config" || arg == "cfg")
            ToggleConfigUi();
        else
            ToggleMainUi();
    }

    public void ToggleConfigUi() => ConfigWindow.Toggle();
    public void ToggleMainUi() => MainWindow.Toggle();
}
