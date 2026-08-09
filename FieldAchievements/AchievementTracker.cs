using System;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace FieldAchievements;

public unsafe sealed class AchievementTracker : IDisposable
{
    public event Action<uint, uint, uint>? OnAchievementProgress;

    private readonly Hook<Achievement.Delegates.ReceiveAchievementProgress> hook;

    public AchievementTracker(IGameInteropProvider gameInterop)
    {
        hook = gameInterop.HookFromAddress<Achievement.Delegates.ReceiveAchievementProgress>(
            Achievement.Addresses.ReceiveAchievementProgress.Value,
            ReceiveAchievementDetour);
        hook.Enable();
    }

    public void Dispose()
    {
        hook.Dispose();
    }

    public void Request(uint id)
    {
        var ui = UIState.Instance();
        if (ui->PlayerState.IsLoaded &&
            ui->Achievement.ProgressRequestState != Achievement.AchievementState.Requested)
            ui->Achievement.RequestAchievementProgress(id);
    }

    private void ReceiveAchievementDetour(Achievement* self, uint id, uint current, uint max)
    {
        OnAchievementProgress?.Invoke(id, current, max);
        hook.Original(self, id, current, max);
    }
}
