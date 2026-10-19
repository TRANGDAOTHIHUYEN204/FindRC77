using System;
using UnityEngine;

public class HintPresenter : MonoBehaviour
{
    [SerializeField] private DifferentPairManagerHolder pairManagerHolder;
    [SerializeField] private StateGame stateGame;
    [SerializeField] private HintSpotlight spotlight;

    public DifferentPair HintedPair { get; private set; }
    public int CountHint => SaveGame.HintCount;

    public event Action<int> OnHintChanged;

    private void Start() => OnHintChanged?.Invoke(CountHint);

    [ContextMenu("Reset Hint Count")]
    public void ResetHint()
    {
        SaveGame.ResetHint();
        OnHintChanged?.Invoke(CountHint);
    }
    public void ShowHint()
    {
        if (CountHint <= 0)
        {
            Debug.Log("Hết hint rồi");
            return;
        }
        if (!stateGame.IsPlaying) return;

        var manager = pairManagerHolder.Manager;
        if (manager == null || manager.IsComplete) return;
        if (HintedPair != null && !HintedPair.IsFound) return;

        var entry = manager.GetHintEntry();
        if (entry == null) return;

        HintedPair = entry.Pair;
        spotlight.Show(HintedPair);
        SetCount(CountHint - 1);
    }
    public void AddHint(int amount) => SetCount(CountHint + amount);

    private void SetCount(int value)
    {
        SaveGame.HintCount = value;
        OnHintChanged?.Invoke(CountHint);
    }
    public void ClearHint()
    {
        HintedPair = null;
        spotlight.Hide();
    }
}