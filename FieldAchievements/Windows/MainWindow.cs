using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using FieldAchievements.Data;
using FieldAchievements.Models;

namespace FieldAchievements.Windows;

public sealed class MainWindow : Window
{
    private readonly Plugin plugin;
    private readonly StringBuilder sb = new();

    private static readonly TrackedMap[] MapOrder =
    [
        TrackedMap.CrescentSouth,
        TrackedMap.CrescentNorth,
        TrackedMap.BozjanSouthernFront,
        TrackedMap.Zadnor,
    ];

    public MainWindow(Plugin plugin)
        : base("特殊地图成就追踪##FieldAchievementsMain", ImGuiWindowFlags.NoScrollbar)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(420, 280),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue),
        };

        this.plugin = plugin;

        TitleBarButtons.Add(new()
        {
            Icon = FontAwesomeIcon.Cog,
            IconOffset = new(1),
            Click = _ => plugin.ToggleConfigUi(),
        });
    }

    public override void PreDraw()
    {
        ImGui.PushStyleColor(ImGuiCol.WindowBg,      Style.PixelWindowBg);
        ImGui.PushStyleColor(ImGuiCol.ChildBg,       Style.PixelChildBg);
        ImGui.PushStyleColor(ImGuiCol.Border,        Style.PixelBorder);
        ImGui.PushStyleColor(ImGuiCol.Separator,     Style.PixelDim);
        ImGui.PushStyleColor(ImGuiCol.FrameBg,       Style.PixelChildBg);
        ImGui.PushStyleColor(ImGuiCol.Button,        Style.PixelButtonBg);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, Style.PixelTabHover);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive,  Style.PixelTabActive);
        ImGui.PushStyleColor(ImGuiCol.Text,          Style.ColorDefault);
        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, Style.PixelAccent);

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding,    0f);
        ImGui.PushStyleVar(ImGuiStyleVar.ChildRounding,     0f);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding,     0f);
        ImGui.PushStyleVar(ImGuiStyleVar.GrabRounding,      0f);
        ImGui.PushStyleVar(ImGuiStyleVar.TabRounding,       0f);
        ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarRounding, 0f);
        ImGui.PushStyleVar(ImGuiStyleVar.FrameBorderSize,   1f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize,  1f);
    }

    public override void PostDraw()
    {
        ImGui.PopStyleVar(Style.PushedVarCount);
        ImGui.PopStyleColor(Style.PushedColorCount);
    }

    public override void Draw()
    {
        using var fontScope = Plugin.PluginInterface.UiBuilder.MonoFontHandle.Push();

        DrawTopBar();
        ImGui.Separator();

        if (!ImGui.BeginTabBar("##field_ach_tabs"))
            return;

        foreach (var map in MapOrder)
        {
            if (ImGui.BeginTabItem(MapData.MapNames[map]))
            {
                DrawMapAchievements(map);
                ImGui.EndTabItem();
            }
        }

        ImGui.EndTabBar();
    }

    private void DrawTopBar()
    {
        var territory = Plugin.ClientState.TerritoryType;
        var currentMap = plugin.GetCurrentMap();
        var mapText = currentMap.HasValue
            ? MapData.MapNames[currentMap.Value]
            : "未识别";

        ImGui.TextColored(Style.PixelDim, "TerritoryType:");
        ImGui.SameLine();
        ImGui.TextUnformatted($"{territory}");
        ImGui.SameLine();
        ImGui.TextColored(Style.PixelDim, "识别地图:");
        ImGui.SameLine();
        ImGui.TextColored(currentMap.HasValue ? Style.ColorComplete : Style.ColorGray, mapText);

        if (ImGui.SmallButton("导出进度"))
            ExportProgress();
    }

    private void DrawMapAchievements(TrackedMap map)
    {
        var entries = MapData.MapAchievements[map];
        var progressList = plugin.Achievements
            .Where(a => a.Map == map)
            .ToList();

        if (progressList.Count == 0)
        {
            ImGui.TextColored(Style.ColorGray, "无追踪成就数据");
            return;
        }

        var completedCount = progressList.Count(a => a.IsComplete);
        var loadedCount = progressList.Count(a => a.IsLoaded);

        ImGui.TextColored(Style.PixelAccent, $"▌ {MapData.MapNames[map]}");
        ImGui.SameLine();
        ImGui.TextColored(Style.ColorGray, $"  已完成 {completedCount}/{progressList.Count}  已加载 {loadedCount}/{progressList.Count}");
        ImGuiHelpers.ScaledDummy(2);
        DrawPixelSeparator();
        ImGuiHelpers.ScaledDummy(2);

        using var child = ImRaii.Child($"##ach_list_{map}", Vector2.Zero, true);
        if (!child.Success)
            return;

        var cfg = plugin.Configuration;
        var first = true;

        foreach (var ach in progressList)
        {
            if (!cfg.ShowCompletedAchievements && ach.IsComplete)
                continue;

            if (!first)
            {
                ImGuiHelpers.ScaledDummy(2);
                DrawPixelSeparator();
                ImGuiHelpers.ScaledDummy(2);
            }
            first = false;

            DrawAchievementEntry(ach, cfg);
        }
    }

    private void DrawAchievementEntry(AchievementProgressInfo ach, Configuration cfg)
    {
        var nameColor = ach.IsComplete ? Style.ColorComplete : Style.ColorDefault;
        ImGui.TextColored(nameColor, $"  {ach.AchievementName}");

        if (cfg.ShowAchievementDescriptions)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, Style.ColorGray);
            ImGui.TextWrapped($"  {ach.Description}");
            ImGui.PopStyleColor();
        }

        if (ach.IsLoaded)
        {
            ImGuiHelpers.ScaledDummy(1);
            ImGui.Text("  ");
            ImGui.SameLine();
            DrawPixelProgress(ach.Ratio, ach.Current, ach.Max, cfg.ProgressBarWidth, ach.IsComplete);
        }
        else
        {
            ImGui.TextColored(Style.ColorGray, "  正在获取数据...");
        }
    }

    private void DrawPixelProgress(float ratio, uint current, uint max, int barWidth, bool isComplete)
    {
        var filled = Math.Clamp((int)Math.Round(ratio * barWidth), 0, barWidth);
        var barColor = ImGui.ColorConvertU32ToFloat4(GetProgressColor(ratio));

        sb.Clear();
        sb.Append('[');
        sb.Append('█', filled);
        sb.Append('░', barWidth - filled);
        sb.Append("] ");
        sb.Append(current);
        sb.Append(" / ");
        sb.Append(max);
        sb.Append(" (");
        sb.Append((int)(ratio * 100));
        sb.Append("%)");

        if (isComplete)
            ImGui.TextColored(Style.ColorComplete, sb.ToString());
        else
            ImGui.TextColored(barColor, sb.ToString());
    }

    private void DrawPixelSeparator()
    {
        var avail = ImGui.GetContentRegionAvail().X;
        var charWidth = ImGui.CalcTextSize("─").X;
        if (charWidth <= 0)
            return;
        var count = Math.Max(1, (int)(avail / charWidth));

        sb.Clear();
        sb.Append('─', count);
        ImGui.TextColored(Style.PixelDim, sb.ToString());
    }

    private static uint GetProgressColor(float ratio) => ratio switch
    {
        >= 1f    => Style.ProgressColorComplete,
        >= 0.5f  => Style.ProgressColorHigh,
        >= 0.25f => Style.ProgressColorMid,
        _        => Style.ProgressColorLow,
    };

    private void ExportProgress()
    {
        var lines = plugin.Achievements.Select(a =>
            $"[{MapData.MapShortNames[a.Map]}] {a.AchievementName}: {(a.IsLoaded ? $"{a.Current}/{a.Max}" : "未加载")}");
        var text = string.Join("\n", lines);
        ImGui.SetClipboardText(text);
        Plugin.Chat.Print("成就进度已导出到剪贴板");
    }
}
