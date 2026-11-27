using System;
using System.Collections.Generic;
using UnityEngine;

public class StreakManager : MonoBehaviour
{
    [Serializable]
    public struct Milestone
    {
        public int streak;
        public int hint;
        public int hintAsCoin;
    }

    [Serializable]
    private class State
    {
        public int streak;
        public bool completed;
        public string completedDate;
        public int lastCountedLevel = -1;
    }

    [SerializeField] private RewardWin rewardData;

    [SerializeField] private List<Milestone> milestones = new List<Milestone>
    {
        new Milestone { streak = 2, hint = 1, hintAsCoin = 0 },
        new Milestone { streak = 4, hint = 0, hintAsCoin = 1 },
        new Milestone { streak = 7, hint = 2, hintAsCoin = 0 },
        new Milestone { streak = 10, hint = 2, hintAsCoin = 2 },
    };

    private State _state;

    private State S
    {
        get
        {
            if (_state == null)
            {
                _state = Load();
                PreviousStreak = _state.streak;
                RefreshDaily();
            }

            return _state;
        }
    }

    public int CurrentStreak => S.streak;
    public bool IsCompleted => S.completed;
    public int PreviousStreak { get; private set; }

    public int MaxStreak => milestones.Count > 0
        ? milestones[milestones.Count - 1].streak
        : 0;

    public IReadOnlyList<Milestone> Milestones => milestones;

    public int HintPrice => rewardData != null
        ? rewardData.HintPrice
        : 0;

    public bool IsMilestone(int streak)
        => FindMilestone(streak).HasValue;

    public event Action<int> OnStreakChanged;
    public event Action<int, int, int> OnRewardGranted;
    public event Action OnStreakCompleted;

    private void Awake()
    {
        _ = S;
    }

    private void OnApplicationFocus(bool focus)
    {
        if (!focus || _state == null) return;

        if (RefreshDaily())
            OnStreakChanged?.Invoke(_state.streak);
    }

    public bool TryGetNextMilestone(int current, out Milestone next)
    {
        foreach (var m in milestones)
        {
            if (m.streak > current)
            {
                next = m;
                return true;
            }
        }

        next = default;
        return false;
    }

    public void MarkStreakShown()
    {
        PreviousStreak = S.streak;
    }

    public void OnWin(int levelIndex)
    {
        _ = S;

        RefreshDaily();

        PreviousStreak = _state.streak;

        if (_state.completed) return;
        if (_state.lastCountedLevel == levelIndex) return;

        _state.lastCountedLevel = levelIndex;
        _state.streak++;

        var m = FindMilestone(_state.streak);

        if (m.HasValue)
            Grant(m.Value);

        bool done = _state.streak >= MaxStreak;

        if (done)
        {
            _state.completed = true;
            _state.completedDate = Today();
        }

        Save();

        OnStreakChanged?.Invoke(_state.streak);

        if (done)
            OnStreakCompleted?.Invoke();
    }

    public void OnLose()
    {
        _ = S;

        RefreshDaily();

        PreviousStreak = _state.streak;

        if (_state.completed) return;

        _state.lastCountedLevel = -1;

        if (_state.streak == 0)
        {
            Save();
            return;
        }

        _state.streak = 0;

        Save();

        OnStreakChanged?.Invoke(0);
    }

    private void Grant(Milestone m)
    {
        int coin = rewardData != null
            ? m.hintAsCoin * rewardData.HintPrice
            : 0;

        int hint = m.hint;

        // Lưu pending để coin bay khi về HomeScene
        if (coin > 0)
            PendingReward.Add(coin);

        if (hint > 0)
            SaveGame.HintBalance += hint;

        OnRewardGranted?.Invoke(m.streak, coin, hint);
    }

    private Milestone? FindMilestone(int streak)
    {
        foreach (var m in milestones)
        {
            if (m.streak == streak)
                return m;
        }

        return null;
    }

    private bool RefreshDaily()
    {
        if (_state == null || !_state.completed)
            return false;

        if (_state.completedDate == Today())
            return false;

        _state = new State();

        PreviousStreak = 0;

        Save();

        return true;
    }

    private static string Today()
        => DateTime.Now.ToString("yyyyMMdd");

    private static State Load()
    {
        string json = SaveGame.StreakState;

        if (string.IsNullOrEmpty(json))
            return new State();

        return JsonUtility.FromJson<State>(json) ?? new State();
    }

    private void Save()
    {
        SaveGame.StreakState = JsonUtility.ToJson(_state);
    }
}