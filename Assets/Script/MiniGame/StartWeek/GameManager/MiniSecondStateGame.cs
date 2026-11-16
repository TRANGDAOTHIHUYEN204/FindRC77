using System;

public enum MiniSecondPhase { Playing, Paused, Won, Lost }

public class MiniSecondStateGame
{
    public const int WinScore = 30;

    public MiniSecondPhase Phase { get; private set; } = MiniSecondPhase.Playing;
    public int TotalScore { get; private set; }

    public bool IsPlaying => Phase == MiniSecondPhase.Playing;
    public bool IsPaused  => Phase == MiniSecondPhase.Paused;
    public bool IsEnded   => Phase == MiniSecondPhase.Won || Phase == MiniSecondPhase.Lost;
    public bool IsWinning => TotalScore >= WinScore;

    public event Action<int, int> OnScoreChanged;         
    public event Action<MiniSecondPhase> OnPhaseChanged;

    public void Begin()
    {
        TotalScore = 0;
        SetPhase(MiniSecondPhase.Playing);
        OnScoreChanged?.Invoke(TotalScore, 0);
    }

    public void AddScore(int amount)
    {
        if (!IsPlaying) return;

        TotalScore += amount; 
        OnScoreChanged?.Invoke(TotalScore, amount);
    }

    public void Pause()
    {
        if (IsPlaying) SetPhase(MiniSecondPhase.Paused);
    }

    public void Resume()
    {
        if (IsPaused) SetPhase(MiniSecondPhase.Playing);
    }
    
    public void Finish()
    {
        if (IsEnded) return;
        SetPhase(IsWinning ? MiniSecondPhase.Won : MiniSecondPhase.Lost);
    }

    private void SetPhase(MiniSecondPhase phase)
    {
        Phase = phase;
        OnPhaseChanged?.Invoke(phase);
    }
}