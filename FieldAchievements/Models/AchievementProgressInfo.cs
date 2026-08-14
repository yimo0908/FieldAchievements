using System;
using FieldAchievements.Data;

namespace FieldAchievements.Models;

/// <summary>
/// 成就进度信息。
/// </summary>
public sealed class AchievementProgressInfo
{
    public uint AchievementID { get; init; }
    public string AchievementName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TrackedMap Map { get; init; }

    public uint Current { get; set; }
    public uint Max { get; set; }

    /// <summary>
    /// 进度是否未知。
    /// buff 追踪成就在适用地图外且未确认完成时，显示为 ??/上限。
    /// </summary>
    public bool IsProgressUnknown { get; set; }

    public float Ratio => Max > 0 ? Math.Clamp((float)Current / Max, 0f, 1f) : 0f;
    public bool IsComplete => Max > 0 && Current >= Max;
    public bool IsLoaded => Max > 0;
}
