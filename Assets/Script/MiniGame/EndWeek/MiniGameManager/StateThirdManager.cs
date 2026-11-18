using System;

public enum MiniThirdPhase { Playing, Paused, Won, Lost }

public class MiniThirdStateGame
{
    public MiniThirdPhase Phase { get; private set; } = MiniThirdPhase.Playing;

    public bool IsPlaying => Phase == MiniThirdPhase.Playing;
    public bool IsPaused  => Phase == MiniThirdPhase.Paused;
    public bool IsEnded   => Phase == MiniThirdPhase.Won || Phase == MiniThirdPhase.Lost;

    public event Action<MiniThirdPhase> OnPhaseChanged;

    public void Begin() => SetPhase(MiniThirdPhase.Playing);

    public void Pause()
    {
        if (IsPlaying) SetPhase(MiniThirdPhase.Paused);
    }

    public void Resume()
    {
        if (IsPaused) SetPhase(MiniThirdPhase.Playing);
    }
    public void Finish(bool allCompleted)
    {
        if (IsEnded) return;
        SetPhase(allCompleted ? MiniThirdPhase.Won : MiniThirdPhase.Lost);
    }

    private void SetPhase(MiniThirdPhase phase)
    {
        Phase = phase;
        OnPhaseChanged?.Invoke(phase);
    }
}