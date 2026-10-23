using System;
using UnityEngine;

public class HintPresenter : MonoBehaviour
{
    [SerializeField] private DifferentPairManagerHolder pairManagerHolder;
    [SerializeField] private StateGame stateGame;
    [SerializeField] private HintSpotlight spotlight;
    [SerializeField] private FactoryManager factory;
    [SerializeField, Min(0)] private int hintPerLevel = 1;

    private HintHand _hand;

    public DifferentPair HintedPair { get; private set; }
    public int CountHint { get; private set; }

    public event Action<int> OnHintChanged;

    private void Start() => ResetForLevel();

    [ContextMenu("Reset Hint For Level")]
    public void ResetForLevel()
    {
        CountHint = hintPerLevel;
        ClearHint();
        OnHintChanged?.Invoke(CountHint);
    }

    public void ShowHint()
    {
        if (CountHint <= 0)
        {
            Debug.Log("Hết hint của màn này rồi");
            return;
        }
        ShowInternal(consume: true, tutorial: false);
    }

    public void ShowTutorialHint() => ShowInternal(consume: false, tutorial: true);

    private void ShowInternal(bool consume, bool tutorial)
    {
        if (!stateGame.IsPlaying) return;

        var manager = pairManagerHolder.Manager;
        if (manager == null || manager.IsComplete) return;
        if (HintedPair != null && !HintedPair.IsFound) return;

        var entry = manager.GetHintEntry();
        if (entry == null) return;

        HintedPair = entry.Pair;

        if (tutorial)
        {
            spotlight.ShowAnimated(HintedPair);
            ReleaseHand();
            _hand = factory.CreateHand();
            if (_hand != null)
            {
                HintedPair.GetWorldRect(HintedPair.RootA, out Vector3 center, out _);
                _hand.PointAt(HintedPair.RootA, center);
            }
        }
        else
        {
            spotlight.Show(HintedPair);
        }

        if (consume) SetCount(CountHint - 1);
    }

    public void AddHint(int amount) => SetCount(CountHint + amount);

    private void SetCount(int value)
    {
        CountHint = Mathf.Max(0, value);
        OnHintChanged?.Invoke(CountHint);
    }

    public void ClearHint()
    {
        HintedPair = null;
        spotlight.Hide();
        ReleaseHand();
    }

    private void ReleaseHand()
    {
        if (_hand == null) return;
        _hand.Detach();
        factory.Release(_hand.gameObject);
        _hand = null;
    }
}