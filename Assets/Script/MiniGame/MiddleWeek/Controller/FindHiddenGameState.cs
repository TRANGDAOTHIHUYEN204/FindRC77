using System;
using System.Collections.Generic;

public enum GamePhase { Playing, Paused, Won, Lost }

[Serializable]
public class FindHiddenSaveData
{
    public int level;
    public List<int> foundIds = new List<int>();
}

public class FindHiddenGameState
{
    private readonly HashSet<int> found = new HashSet<int>();

    public GamePhase Phase { get; private set; } = GamePhase.Playing;
    public int Level { get; private set; }
    public int Total { get; private set; }
    public int FoundCount => found.Count;
    public IReadOnlyCollection<int> FoundIds => found;
    public bool IsEnded => Phase == GamePhase.Won || Phase == GamePhase.Lost;
    public bool IsCompleted => FoundCount >= Total;

    public event Action<int, int> OnProgress;
    public event Action<GamePhase> OnPhaseChanged;

    public void Begin(int level, int total, IEnumerable<int> restoredIds = null)
    {
        Level = level;
        Total = total;
        found.Clear();
        if (restoredIds != null)
            foreach (var id in restoredIds) found.Add(id);

        SetPhase(GamePhase.Playing);
        OnProgress?.Invoke(FoundCount, Total);
    }

    public bool MarkFound(int id)
    {
        if (!found.Add(id)) return false; 
        OnProgress?.Invoke(FoundCount, Total);
        return true;
    }

    public bool Contains(int id) => found.Contains(id);

    public void SetPhase(GamePhase phase)
    {
        Phase = phase;
        OnPhaseChanged?.Invoke(phase);
    }

    public FindHiddenSaveData ToSaveData()
    {
        return new FindHiddenSaveData
        {
            level = Level,
            foundIds = new List<int>(found)
        };
    }
}