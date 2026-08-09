using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace FieldAchievements.Windows;

public sealed class ConfigWindow : Window
{
    private readonly Plugin plugin;

    public ConfigWindow(Plugin plugin)
        : base("特殊地图成就追踪 - 设置###FieldAchievementsConfig")
    {
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(340, 260);
        SizeCondition = ImGuiCond.Always;

        this.plugin = plugin;
    }

    public override void Draw()
    {
        var cfg = plugin.Configuration;

        ImGui.TextUnformatted("悬浮窗");
        ImGui.Separator();

        var showFloating = cfg.ShowFloatingWindow;
        if (ImGui.Checkbox("显示悬浮窗", ref showFloating))
        {
            cfg.ShowFloatingWindow = showFloating;
            cfg.Save();
        }

        ImGui.SameLine();
        ImGui.BeginDisabled(!cfg.ShowFloatingWindow);
        var lockFloating = cfg.LockFloatingWindow;
        if (ImGui.Checkbox("锁定位置", ref lockFloating))
        {
            cfg.LockFloatingWindow = lockFloating;
            cfg.Save();
        }
        ImGui.EndDisabled();

        var showCompletedFloat = cfg.ShowCompletedInFloatingWindow;
        if (ImGui.Checkbox("悬浮窗显示已完成成就", ref showCompletedFloat))
        {
            cfg.ShowCompletedInFloatingWindow = showCompletedFloat;
            cfg.Save();
        }

        var alpha = cfg.FloatingWindowAlpha;
        ImGui.SetNextItemWidth(120f);
        if (ImGui.SliderFloat("透明度", ref alpha, 0.3f, 1.0f, "%.2f"))
        {
            cfg.FloatingWindowAlpha = alpha;
            cfg.Save();
        }

        ImGui.Spacing();
        ImGui.TextUnformatted("主窗口");
        ImGui.Separator();

        var showDesc = cfg.ShowAchievementDescriptions;
        if (ImGui.Checkbox("显示成就描述", ref showDesc))
        {
            cfg.ShowAchievementDescriptions = showDesc;
            cfg.Save();
        }

        ImGui.SameLine();
        var showCompleted = cfg.ShowCompletedAchievements;
        if (ImGui.Checkbox("显示已完成成就", ref showCompleted))
        {
            cfg.ShowCompletedAchievements = showCompleted;
            cfg.Save();
        }

        var barWidth = cfg.ProgressBarWidth;
        ImGui.SetNextItemWidth(120f);
        if (ImGui.SliderInt("进度条宽度", ref barWidth, 8, 32))
        {
            cfg.ProgressBarWidth = barWidth;
            cfg.Save();
        }

    }
}
