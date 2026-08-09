using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using FieldAchievements.Data;
using FieldAchievements.Models;

namespace FieldAchievements.Windows;

public sealed class FloatingWindow : Window
{
    private readonly Plugin plugin;
    private readonly StringBuilder sb = new();
    private bool collapsed;

    private static readonly Vector4 Yellow = new(1f, 0.85f, 0.3f, 1f);

    public FloatingWindow(Plugin plugin)
        : base(
            "##FieldAchievementsFloating",
            ImGuiWindowFlags.NoTitleBar
            | ImGuiWindowFlags.NoScrollbar
            | ImGuiWindowFlags.NoScrollWithMouse
            | ImGuiWindowFlags.AlwaysAutoResize
            | ImGuiWindowFlags.NoFocusOnAppearing
            | ImGuiWindowFlags.NoNav)
    {
        this.plugin = plugin;
        BgAlpha = plugin.Configuration.FloatingWindowAlpha;
        SizeCondition = ImGuiCond.FirstUseEver;
        Position = new Vector2(420f, 220f);
        PositionCondition = ImGuiCond.FirstUseEver;
    }

    public bool ShouldBeOpen
        => plugin.Configuration.ShowFloatingWindow && plugin.GetCurrentMap().HasValue;

    public override void Draw()
    {
        BgAlpha = plugin.Configuration.FloatingWindowAlpha;
        Flags = BuildFlags();

        if (ImGui.IsWindowHovered(ImGuiHoveredFlags.RootAndChildWindows) && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            plugin.ToggleMainUi();

        if (DrawHeader())
            collapsed = !collapsed;

        if (collapsed)
            return;

        ImGui.Separator();
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6f, 4f));

        DrawCurrentMapProgress();

        ImGui.PopStyleVar();
    }

    private bool DrawHeader()
    {
        var currentMap = plugin.GetCurrentMap();
        var title = currentMap.HasValue
            ? MapData.MapShortNames[currentMap.Value]
            : "特殊地图成就";

        ImGui.PushStyleColor(ImGuiCol.Text, Yellow);
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(title);
        ImGui.PopStyleColor();

        var clicked = ImGui.IsItemClicked(ImGuiMouseButton.Left);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(collapsed ? "左键展开悬浮窗\n右键打开主界面" : "左键折叠悬浮窗\n右键打开主界面");
        return clicked;
    }

    private void DrawCurrentMapProgress()
    {
        var currentMap = plugin.GetCurrentMap();
        if (!currentMap.HasValue)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, Style.ColorGray);
            ImGui.TextUnformatted("当前不在追踪地图");
            ImGui.PopStyleColor();
            return;
        }

        var cfg = plugin.Configuration;
        var progressList = plugin.Achievements
            .Where(a => a.Map == currentMap.Value)
            .ToList();

        var completedCount = progressList.Count(a => a.IsComplete);
        ImGui.TextColored(Style.ColorGray, $"已完成 {completedCount}/{progressList.Count}");

        var drewAny = false;

        foreach (var ach in progressList)
        {
            if (!cfg.ShowCompletedInFloatingWindow && ach.IsComplete)
                continue;
            drewAny = true;
            DrawCompactProgress(ach, cfg.ProgressBarWidth);
        }

        if (!drewAny)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, Style.ColorComplete);
            ImGui.TextUnformatted("所有成就已完成！");
            ImGui.PopStyleColor();
        }
    }

    private void DrawCompactProgress(AchievementProgressInfo ach, int barWidth)
    {
        var nameColor = ach.IsComplete ? Style.ColorComplete : Style.ColorDefault;

        ImGui.TextColored(nameColor, ach.AchievementName);

        if (ach.IsLoaded)
        {
            var ratio = ach.Ratio;
            var filled = Math.Clamp((int)Math.Round(ratio * barWidth), 0, barWidth);
            var barColor = ImGui.ColorConvertU32ToFloat4(GetProgressColor(ratio));

            sb.Clear();
            sb.Append('[');
            sb.Append('█', filled);
            sb.Append('░', barWidth - filled);
            sb.Append("] ");
            sb.Append(ach.Current);
            sb.Append("/");
            sb.Append(ach.Max);

            ImGui.TextColored(ach.IsComplete ? Style.ColorComplete : barColor, sb.ToString());
        }
        else
        {
            ImGui.TextColored(Style.ColorGray, "加载中...");
        }
    }

    private ImGuiWindowFlags BuildFlags()
    {
        var flags = ImGuiWindowFlags.NoTitleBar
                    | ImGuiWindowFlags.NoScrollbar
                    | ImGuiWindowFlags.NoScrollWithMouse
                    | ImGuiWindowFlags.AlwaysAutoResize
                    | ImGuiWindowFlags.NoFocusOnAppearing;

        if (plugin.Configuration.LockFloatingWindow)
            flags |= ImGuiWindowFlags.NoMove;

        return flags;
    }

    private static uint GetProgressColor(float ratio) => ratio switch
    {
        >= 1f    => Style.ProgressColorComplete,
        >= 0.5f  => Style.ProgressColorHigh,
        >= 0.25f => Style.ProgressColorMid,
        _        => Style.ProgressColorLow,
    };
}
