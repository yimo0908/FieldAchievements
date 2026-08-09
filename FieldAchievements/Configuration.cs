using Dalamud.Configuration;
using System;

namespace FieldAchievements;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    /// <summary>显示悬浮窗。</summary>
    public bool ShowFloatingWindow { get; set; } = true;

    /// <summary>锁定悬浮窗位置。</summary>
    public bool LockFloatingWindow { get; set; } = false;

    /// <summary>悬浮窗背景透明度 (0~1)。</summary>
    public float FloatingWindowAlpha { get; set; } = 0.8f;

    /// <summary>在主窗口中显示成就描述。</summary>
    public bool ShowAchievementDescriptions { get; set; } = true;

    /// <summary>在主窗口中显示已完成的成就。</summary>
    public bool ShowCompletedAchievements { get; set; } = true;

    /// <summary>在悬浮窗中显示已完成的成就。</summary>
    public bool ShowCompletedInFloatingWindow { get; set; } = false;

    /// <summary>悬浮窗只显示当前地图的成就（否则显示所有地图）。</summary>
    public bool FloatingWindowCurrentMapOnly { get; set; } = true;

    /// <summary>成就进度刷新间隔（秒）。</summary>
    public int RefreshIntervalSeconds { get; set; } = 5;

    /// <summary>进度条像素风格宽度。</summary>
    public int ProgressBarWidth { get; set; } = 16;

    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
