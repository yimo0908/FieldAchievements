﻿using System;
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

    private const int AchInitRetryInterval = 30;
    private const int AchRefreshDefaultInterval = 300;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        Achievements = MapData.MapAchievements
            .SelectMany(kv => kv.Value.Select(entry => new AchievementProgressInfo
            {
                AchievementID = entry.AchievementId,
                AchievementName = entry.Name,
                Description = entry.Description,
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
        uninitializedCount = Achievements.Count;

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
        if (achIndicesByID.TryGetValue(id, out var indices))
        {
            foreach (var idx in indices)
            {
                var entry = Achievements[idx];
                if (entry.Max == 0 && max > 0)
                    uninitializedCount--;
                entry.Current = current;
                entry.Max = max;
            }
        }
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
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
                    pendingRefresh.Enqueue(i);
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
    /// 基于 buff 层数更新特定成就的进度。
    /// 当玩家在指定地图内时，用 buff 层数代替成就系统获取实时进度。
    /// </summary>
    private void UpdateBuffBasedProgress()
    {
        var player = ObjectTable.LocalPlayer;
        if (player == null)
            return;

        var currentMap = GetCurrentMap();
        if (!currentMap.HasValue)
            return;

        var map = currentMap.Value;
        var statusList = player.StatusList;

        foreach (var (achId, rule) in MapData.BuffProgressRules)
        {
            if (Array.IndexOf(rule.ApplicableMaps, map) < 0)
                continue;

            if (!achIndicesByID.TryGetValue(achId, out var indices))
                continue;

            var buffSum = 0u;
            foreach (var status in statusList)
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

            var current = Math.Min(buffSum, rule.Max);
            foreach (var idx in indices)
            {
                var entry = Achievements[idx];
                if (entry.Max == 0)
                    uninitializedCount--;
                entry.Current = current;
                entry.Max = rule.Max;
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
